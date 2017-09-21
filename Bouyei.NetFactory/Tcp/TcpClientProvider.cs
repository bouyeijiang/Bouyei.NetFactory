using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Bouyei.NetFactory.Tcp
{
    public class TcpClientProvider : IDisposable
    {
        #region variable
        private bool isConnected = false;
        private bool _isDisposed = false;
        private int cliRecBufferSize = 4096;
        private int cliConSend = 8;
        private int cliConnectTimeout = 30000;
        private byte[] cliRecBuffer = null;
        private Socket cliSocket = null;
        private SocketTokenManager<SocketAsyncEventArgs> sendTokenManager = null;
        private SocketBufferManager sBufferManager = null;
        private ChannelProviderType channelProviderState = ChannelProviderType.Async;
        private ManualResetEvent mReset = new ManualResetEvent(false);
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

        public int SendBufferPoolNumber { get { return sendTokenManager.Count; } }

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
                Utils.SafeCloseSocket(cliSocket);
                _isDisposed = true;
            }
        }

        private void DisposeSocketPool()
        {
            sendTokenManager.Clear();
            if (sBufferManager != null)
            {
                sBufferManager.Clear();
            }
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="chunkBufferSize">发送块缓冲区大小</param>
        /// <param name="concurrentSend">并发发送数</param>
        public TcpClientProvider(int chunkBufferSize = 4096, int concurrentSend = 8)
        {
            this.cliConSend = concurrentSend;
            this.cliRecBufferSize = chunkBufferSize;
            this.cliRecBuffer = new byte[chunkBufferSize];
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
                if (isConnected || cliSocket != null)
                {
                    Close();
                }
                
                isConnected = false;
                channelProviderState = ChannelProviderType.Async;
                InitializePool(cliConSend);

                IPEndPoint ips = new IPEndPoint(IPAddress.Parse(ip), port);

                cliSocket = CreateConnectAsync(ips, cliRecBuffer);
            }
            catch (Exception ex)
            {
                Close();
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
                if (isConnected || cliSocket != null)
                {
                    Close();
                }

                isConnected = false;
                channelProviderState = ChannelProviderType.AsyncWait;

                IPEndPoint ips = new IPEndPoint(IPAddress.Parse(ip), port);

                cliSocket = CreateConnectAsync(ips, cliRecBuffer);

                mReset.WaitOne(cliConnectTimeout);
                isConnected = cliSocket.Connected;
                
                if (isConnected)
                    InitializePool(cliConSend);

                return isConnected;
            }
            catch (Exception ex)
            {
                cliSocket.Dispose();
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

            if (isConnected || cliSocket != null)
            {
                Close();
            }

            isConnected = false;
            channelProviderState = ChannelProviderType.Sync;
            int retry = 3;

            IPEndPoint ips = new IPEndPoint(IPAddress.Parse(ip), port);

            cliSocket = new Socket(ips.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                LingerState = new LingerOption(true, 0),
                NoDelay = true
            };

            while (retry > 0)
            {
                try
                {
                    --retry;
                    cliSocket.Connect(ips);
                    isConnected = true;
                    return true;
                }
                catch (Exception ex)
                {
                    Close();
                    if (retry <= 0) throw ex;
                    Thread.Sleep(1000);
                }
            }
            return false;
        }

        /// <summary>
        /// 异步发送数据
        /// </summary>
        /// <param name="buffer"></param>
        public void Send(byte[] buffer,bool isWaiting=true)
        {
            Send(buffer, 0, buffer.Length, isWaiting);
        }

        /// <summary>
        /// 根据偏移发送缓冲区数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public void Send(byte[] buffer, int offset, int size, bool isWaiting = true)
        {
            try
            {
                if (isConnected == false ||
                    cliSocket == null ||
                    cliSocket.Connected == false) return;

                ArraySegment<byte>[] segItems = sBufferManager.BufferToSegments(buffer, offset, size);
                foreach (var seg in segItems)
                {
                    var tArgs = sendTokenManager.GetEmptyWait(isWaiting);
                    if (tArgs == null)
                        throw new Exception("发送缓冲池已用完,等待回收超时...");

                    if (!sBufferManager.WriteBuffer(tArgs, seg.Array, seg.Offset, seg.Count))
                    {
                        sendTokenManager.Set(tArgs);

                        throw new Exception(string.Format("发送缓冲区溢出...buffer block max size:{0}", sBufferManager.BlockSize));
                    }
                    if (tArgs.UserToken == null)
                        ((SocketToken)tArgs.UserToken).TokenSocket = cliSocket;

                    if (!cliSocket.SendAsync(tArgs))
                    {
                        ProcessSentHandler(tArgs);
                    }

                    if (sendTokenManager.Count < (sendTokenManager.Capacity >> 2))
                        Thread.Sleep(5);
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
            cliSocket.SendFile(filename);
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

            int sent = cliSocket.Send(buffer);
            if (recAct != null && sent > 0)
            {
                byte[] recBuffer = new byte[recBufferSize];

                int cnt = cliSocket.Receive(recBuffer, recBuffer.Length, 0);

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
            int sent = cliSocket.Send(buffer, offset, size, SocketFlags.None);
            if (recAct != null && sent > 0)
            {
                byte[] recBuffer = new byte[recBufferSize];

                int cnt = cliSocket.Receive(recBuffer, recBuffer.Length, 0);

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
            int sent = cliSocket.Send(buffer, offset, size, SocketFlags.None);
            if (recAct != null && sent > 0)
            {
                int cnt = cliSocket.Receive(recBuffer, recOffset, recSize, 0);

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
                if (cliSocket.Connected == false) break;

                cnt = cliSocket.Receive(buffer, buffer.Length, 0);
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
            Close();
        }

        #endregion

        #region private method

        private Socket CreateConnectAsync(IPEndPoint ips, byte[] recBuffer)
        {
            Socket socket = new Socket(ips.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                LingerState = new LingerOption(true, 0),
                NoDelay = true
            };

            //连接事件绑定
            var sArgs = new SocketAsyncEventArgs
            {
                RemoteEndPoint = ips,
                UserToken = new SocketToken(-1) { TokenSocket = socket }
            };
            sArgs.AcceptSocket = socket;
            sArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            if (!socket.ConnectAsync(sArgs))
            {
                ProcessConnectHandler(sArgs);
            }
            return socket;
        }

        private void Close()
        {
            DisposeSocketPool();
            Utils.SafeCloseSocket(cliSocket);
        }

        /// <summary>
        /// 初始化发送对象池
        /// </summary>
        /// <param name="maxNumber"></param>
        private void InitializePool(int maxNumberOfConnections)
        {
            if(sendTokenManager!=null) sendTokenManager.Clear();
            if (sBufferManager != null) sBufferManager.Clear();

            sendTokenManager = new SocketTokenManager<SocketAsyncEventArgs>(maxNumberOfConnections);
            sBufferManager = new SocketBufferManager(maxNumberOfConnections, cliRecBufferSize);
          
            for (int i = 0; i < maxNumberOfConnections; ++i)
            {
                SocketAsyncEventArgs tArgs = new SocketAsyncEventArgs() {
                    DisconnectReuseSocket=true
                };
                tArgs.Completed +=  IO_Completed;
                tArgs.UserToken = new SocketToken(i)
                {
                    TokenSocket = cliSocket,
                    TokenId = i
                };
                sBufferManager.SetBuffer(tArgs);
                sendTokenManager.Set(tArgs);
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
                if (e.SocketError == SocketError.Success)
                {
                    if (SentCallback != null)
                    {
                        SocketToken sToken = e.UserToken as SocketToken;
                        sToken.TokenIpEndPoint = (IPEndPoint)e.RemoteEndPoint;

                        SentCallback(sToken, e.Buffer, e.Offset, e.BytesTransferred);
                    }
                }
                else
                {
                    ProcessDisconnectEvent(e);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                sendTokenManager.Set(e);
            }
        }

        /// <summary>
        /// 处理接收数据之后的事件
        /// </summary>
        /// <param name="e"></param>
        private void ProcessReceiveHandler(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred == 0
                || e.SocketError != SocketError.Success
                || e.AcceptSocket.Connected == false)
            {
                ProcessDisconnectEvent(e);
                return;
            }
            SocketToken sToken = e.UserToken as SocketToken;
            sToken.TokenIpEndPoint = (IPEndPoint)e.RemoteEndPoint;

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
                    e.SetBuffer(cliRecBuffer, 0, cliRecBufferSize);
                    if (ConnectedCallback != null)
                    {
                        SocketToken sToken = e.UserToken as SocketToken;
                        sToken.TokenIpEndPoint = (IPEndPoint)e.RemoteEndPoint;
                        ConnectedCallback(sToken, isConnected);
                    }

                    if (!e.AcceptSocket.ReceiveAsync(e))
                    {
                        ProcessReceiveHandler(e);
                    }
                }
                else
                {
                    ProcessDisconnectEvent(e);
                }
            }
            catch
            { }
        }

        /// <summary>
        /// 处理断开连接事件
        /// </summary>
        /// <param name="e"></param>
        private void ProcessDisconnectHandler(SocketAsyncEventArgs e)
        {
            try
            {
                isConnected = (e.SocketError == SocketError.Success);
                if (isConnected||
                    e.AcceptSocket.Connected)
                {
                    isConnected = false;
                    Close();
                }

                if (DisconnectedCallback != null)
                {
                    SocketToken sToken = e.UserToken as SocketToken;
                    sToken.TokenIpEndPoint = (IPEndPoint)e.RemoteEndPoint;
                    DisconnectedCallback(sToken);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void ProcessDisconnectEvent(SocketAsyncEventArgs e)
        {
            try
            {
                if(e.AcceptSocket.Connected)
                e.AcceptSocket.Shutdown(SocketShutdown.Both);

                bool willRaiseEvent = e.AcceptSocket.DisconnectAsync(e);

                if (!willRaiseEvent)
                {
                    ProcessDisconnectHandler(e);
                }
            }
            catch(Exception ex)
            {

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
                default:
                    break;
            }
        }
        #endregion
    }
}