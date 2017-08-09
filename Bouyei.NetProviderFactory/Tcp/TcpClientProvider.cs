using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Bouyei.NetProviderFactory.Tcp
{
    public class TcpClientProvider : IDisposable
    {
        #region variable
        private bool isConnected = false;
        private bool _isDisposed = false;
        private ChannelProviderType channelProviderState = ChannelProviderType.Async;
        private int blockSize = 4096;
        private int concurrentSend = 8;
        private byte[] receiveBuffer = null;
        private object readwritelock = new object();
        private ManualResetEvent mReset = new ManualResetEvent(false);
        private Socket clientSocket = null;
        private SocketTokenManager<SocketAsyncEventArgs> tokenPool = null;
        private SocketBufferManager sendBufferPool = null;
        #endregion

        #region property
        /// <summary>
        /// 发送回调处理
        /// </summary>
        public OnSentHandler SentCallback { get; set; }

        /// <summary>
        /// 接收数据回调处理
        /// </summary>
        public OnReceiveHandler RecievedCallback { get; set; }

        /// <summary>
        /// 接受数据回调，返回缓冲区和偏移量
        /// </summary>
        public OnReceiveOffsetHandler ReceiveOffsetCallback { get; set; }

        /// <summary>
        /// 断开连接回调处理
        /// </summary>
        public OnDisconnectedHandler DisconnectedCallback { get; set; }

        /// <summary>
        /// 连接回调处理
        /// </summary>
        public OnConnectedHandler ConnectedCallback { get; set; }

        /// <summary>
        /// 是否连接状态
        /// </summary>
        public bool IsConnected
        {
            get { return isConnected; }
        }

        public int SendBufferPoolNumber { get { return tokenPool.Count; } }

        public ChannelProviderType ChannelProviderState
        {
            get { return channelProviderState; }
        }

        #endregion

        #region constructor
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;

            if (isDisposing)
            {
                DisposeSocketPool();
                clientSocket.Dispose();
                //recBufferPool.Clear();
                _isDisposed = true;
            }
        }

        private void DisposeSocketPool()
        {
            while (tokenPool.Count > 0)
            {
                var item = tokenPool.Get();
                if (item != null) item.Dispose();
            }
            sendBufferPool.Clear();
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="chunkBufferSize">发送块缓冲区大小</param>
        /// <param name="concurrentSend">并发发送数</param>
        public TcpClientProvider(int chunkBufferSize = 4096, int concurrentSend = 8)
        {
            this.concurrentSend = concurrentSend;
            this.blockSize = chunkBufferSize;
            this.receiveBuffer = new byte[chunkBufferSize];
        }

        #endregion

        #region public method
        /// <summary>
        /// 异步建立连接
        /// </summary>
        /// <param name="port"></param>
        /// <param name="ip"></param>
        public void Connect(int port, string ip)
        {
            try
            {
                if (isConnected ||
                    clientSocket != null)
                {
                    Close();
                }

                isConnected = false;
                channelProviderState = ChannelProviderType.Async;
                InitializePool(concurrentSend);

                IPEndPoint ips = new IPEndPoint(IPAddress.Parse(ip), port);

                clientSocket = new Socket(ips.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = ips;
                args.UserToken = new SocketToken(-1) { TokenSocket = clientSocket };

                args.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);

                if (!clientSocket.ConnectAsync(args))
                {
                    ProcessConnectHandler(args);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 异步等待连接返回结果
        /// </summary>
        /// <param name="port"></param>
        /// <param name="ip"></param>
        /// <returns></returns>
        public bool ConnectTo(int port,string ip)
        {
            try
            {
                if (isConnected ||
                    clientSocket != null)
                {
                    Close();
                }

                isConnected = false;
                channelProviderState = ChannelProviderType.AsyncWait;

                IPEndPoint ips = new IPEndPoint(IPAddress.Parse(ip), port);

                clientSocket = new Socket(ips.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                //连接事件绑定
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = ips;
                args.UserToken = new SocketToken(-1) { TokenSocket = clientSocket };

                args.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);

                if (!clientSocket.ConnectAsync(args))
                {
                    ProcessConnectHandler(args);
                }
                mReset.WaitOne();
                isConnected = clientSocket.Connected;
                
                if (isConnected)
                    InitializePool(concurrentSend);

                return isConnected;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 同步连接
        /// </summary>
        /// <param name="port"></param>
        /// <param name="ip"></param>
        /// <returns></returns>
        public bool ConnectSync(int port, string ip)
        {

            if (isConnected ||
                clientSocket != null)
            {
                Close();
            }

            isConnected = false;
            channelProviderState = ChannelProviderType.Sync;
            int retry = 3;

            IPEndPoint ips = new IPEndPoint(IPAddress.Parse(ip), port);

            clientSocket = new Socket(ips.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            again:
            try
            {
                clientSocket.Connect(ips);
                isConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                if (retry >= 0) throw ex;
               Thread.Sleep(500);
                retry -= 1;
                goto again;
            }
        }

        /// <summary>
        /// 异步发送数据
        /// </summary>
        /// <param name="buffer"></param>
        public void Send(byte[] buffer,bool waitingSignal=true)
        {
            Send(buffer, 0, buffer.Length, waitingSignal);
        }

        /// <summary>
        /// 根据偏移发送缓冲区数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public void Send(byte[] buffer, int offset, int size, bool waitingSignal = true)
        {
            SocketAsyncEventArgs tArgs = null;
            try
            {
                if (isConnected == false ||
                    clientSocket == null ||
                    clientSocket.Connected == false) return;

                tArgs = tokenPool.Get();
                if (tArgs == null)
                {
                    while (waitingSignal)
                    {
                        Thread.Sleep(500);
                        tArgs = tokenPool.Get();
                        if (tArgs != null) break;
                    }
                }
                if (tArgs == null)
                    throw new Exception("发送缓冲池已用完,等待回收...");

                if (!sendBufferPool.WriteBuffer(tArgs, buffer, offset, size))
                {
                    tArgs.SetBuffer(buffer, offset, size);
                }

                if (tArgs.UserToken == null)
                    ((SocketToken)tArgs.UserToken).TokenSocket = clientSocket;

                if (!clientSocket.SendAsync(tArgs))
                {
                    ProcessSentHandler(tArgs);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 发送文件
        /// </summary>
        /// <param name="filename"></param>
        public void SendFile(string filename)
        {
            clientSocket.SendFile(filename);
        }

        /// <summary>
        /// 同步发送数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="recBufferSize"></param>
        /// <param name="recAct"></param>
        /// <returns></returns>
        public int SendSync(byte[] buffer, Action<int, byte[]> recAct = null, int recBufferSize = 4096)
        {
            if (channelProviderState != ChannelProviderType.Sync)
            {
                throw new Exception("需要使用同步连接...ConnectSync");
            }

            int sent = clientSocket.Send(buffer);
            if (recAct != null && sent > 0)
            {
                byte[] recBuffer = new byte[recBufferSize];

                int cnt = clientSocket.Receive(recBuffer, recBuffer.Length, 0);

                recAct(cnt, recBuffer);
            }
            return sent;
        }

        /// <summary>
        /// 指定缓冲区接收数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="recBufferSize"></param>
        /// <param name="recAct"></param>
        /// <returns></returns>
        public int SendSync(byte[] buffer,int offset,int size,int recBufferSize=4096
            ,Action<int,byte[]>recAct=null)
        {
            if (channelProviderState != ChannelProviderType.Sync)
            {
                throw new Exception("需要使用同步连接...ConnectSync");
            }
            int sent = clientSocket.Send(buffer, offset, size, SocketFlags.None);
            if (recAct != null && sent > 0)
            {
                byte[] recBuffer = new byte[recBufferSize];

                int cnt = clientSocket.Receive(recBuffer, recBuffer.Length, 0);

                recAct(cnt, recBuffer);
            }
            return sent;
        }

        /// <summary>
        /// 指定缓冲区引用接受数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="recOffset"></param>
        /// <param name="recSize"></param>
        /// <param name="recBuffer"></param>
        /// <param name="recAct"></param>
        /// <returns></returns>
        public int SendSync(byte[] buffer, int offset, int size
            ,ref int recOffset,ref int recSize,ref byte[] recBuffer
            ,Action<int> recAct = null)
        {
            if (channelProviderState != ChannelProviderType.Sync)
            {
                throw new Exception("需要使用同步连接...ConnectSync");
            }
            int sent = clientSocket.Send(buffer, offset, size, SocketFlags.None);
            if (recAct != null && sent > 0)
            {
                int cnt = clientSocket.Receive(recBuffer, recOffset, recSize, 0);

                recAct(cnt);
            }
            return sent;
        }

        /// <summary>
        /// 同步接收数据
        /// </summary>
        /// <param name="recBufferSize"></param>
        /// <param name="recAct"></param>
        public void ReceiveSync(Action<int, byte[]> recAct,int recBufferSize = 4096)
        {
            if (channelProviderState != ChannelProviderType.Sync)
            {
                throw new Exception("需要使用同步连接...ConnectSync");
            }
            int cnt = 0;
            byte[] buffer = new byte[recBufferSize];
            do
            {
                if (clientSocket.Connected == false) break;

                cnt = clientSocket.Receive(buffer, buffer.Length, 0);
                if (cnt > 0)
                {
                    recAct(cnt, buffer);
                }
            } while (cnt > 0);
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            try
            {
                if (clientSocket != null)
                {
                    isConnected = false;
                    if (clientSocket.Connected)
                    {
                        clientSocket.Disconnect(true);
                        clientSocket.Shutdown(SocketShutdown.Both);
                    }

                    clientSocket.Close();
                    clientSocket.Dispose();
                    clientSocket = null;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region private method

        /// <summary>
        /// 关闭连接对象
        /// </summary>
        /// <param name="e"></param>
        private void Close(SocketAsyncEventArgs e)
        {
            if (e.ConnectSocket != null)
            {
                if (e.ConnectSocket.Connected)
                    e.ConnectSocket.Shutdown(SocketShutdown.Both);
                e.ConnectSocket.Close();
                e.ConnectSocket.Dispose();
            }
        }

        private void Close()
        {
            if (clientSocket != null)
            {
                if (clientSocket.Connected)
                {
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Disconnect(true);
                }
                clientSocket.Close();
                clientSocket.Dispose();
            }
            DisposeSocketPool();
        }

        /// <summary>
        /// 初始化发送对象池
        /// </summary>
        /// <param name="maxNumber"></param>
        private void InitializePool(int maxNumberOfConnections)
        {
            if(tokenPool!=null) tokenPool.Clear();
            if (sendBufferPool != null) sendBufferPool.Clear();

            tokenPool = new SocketTokenManager<SocketAsyncEventArgs>(maxNumberOfConnections);
            sendBufferPool = new SocketBufferManager(maxNumberOfConnections, blockSize);
          
            SocketAsyncEventArgs tArgs = null;
            for (int i = 0; i < maxNumberOfConnections; ++i)
            {
                tArgs = new SocketAsyncEventArgs();
                //tArgs.DisconnectReuseSocket = true;
                tArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                tArgs.UserToken = new SocketToken(i);
                sendBufferPool.SetBuffer(tArgs);
                tokenPool.Set(tArgs);
            }
        }

        /// <summary>
        /// 处理发送之后的事件
        /// </summary>
        /// <param name="e"></param>
        private void ProcessSentHandler(SocketAsyncEventArgs e)
        {
            try
            {
                tokenPool.Set(e);
                SocketToken sToken = e.UserToken as SocketToken;

                //if (e.DisconnectReuseSocket == false)
                //    e.DisconnectReuseSocket = true;
                if (e.SocketError == SocketError.Success)
                {
                    if (SentCallback != null)
                        SentCallback(sToken, e.BytesTransferred);
                }
                else
                {
                    Close(e);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 处理接收数据之后的事件
        /// </summary>
        /// <param name="e"></param>
        private void ProcessReceiveHandler(SocketAsyncEventArgs e)
        {
            SocketToken sToken = e.UserToken as SocketToken;

            try
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    if (ReceiveOffsetCallback != null)
                        ReceiveOffsetCallback(sToken, e.Buffer, e.Offset, e.BytesTransferred);

                    if (RecievedCallback != null)
                    {
                        if (e.Offset == 0 && e.BytesTransferred == e.Buffer.Length)
                        {
                            RecievedCallback(sToken, e.Buffer);
                        }
                        else
                        {
                            byte[] realBytes = new byte[e.BytesTransferred];

                            Buffer.BlockCopy(e.Buffer, e.Offset, realBytes, 0, e.BytesTransferred);
                            RecievedCallback(sToken, realBytes);
                        }
                    }

                    if (!isConnected) return;

                    if (!clientSocket.ReceiveAsync(e))
                    {
                        ProcessReceiveHandler(e);
                    }
                }
                else
                {
                    if (DisconnectedCallback != null)
                        DisconnectedCallback(sToken);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 处理连接之后的事件
        /// </summary>
        /// <param name="e"></param>
        private void ProcessConnectHandler(SocketAsyncEventArgs e)
        {
            try
            {
                isConnected = (e.SocketError == SocketError.Success);
                if (channelProviderState == ChannelProviderType.AsyncWait) mReset.Set();//异步等待连接

                if (isConnected)
                {
                    e.SetBuffer(receiveBuffer, 0, blockSize);
                    if (!clientSocket.ReceiveAsync(e))
                    {
                        ProcessReceiveHandler(e);
                    }
                }
                if (ConnectedCallback != null)
                    ConnectedCallback(e.UserToken as SocketToken, isConnected);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 处理断开连接事件
        /// </summary>
        /// <param name="e"></param>
        private void ProcessDisconnectHandler(SocketAsyncEventArgs e)
        {
            try
            {
                Close(e);
                isConnected = (e.SocketError == SocketError.Success);

                if (DisconnectedCallback != null)
                    DisconnectedCallback(e.UserToken as SocketToken);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 响应下一个接收、发送、连接事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Send:
                    ProcessSentHandler(e);
                    break;
                case SocketAsyncOperation.Receive:
                    ProcessReceiveHandler(e);
                    break;
                case SocketAsyncOperation.Connect:
                    ProcessConnectHandler(e);
                    break;
                case SocketAsyncOperation.Disconnect:
                    ProcessDisconnectHandler(e);
                    break;
            }
        }
        #endregion
    }
}