using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Bouyei.NetProviderFactory.Tcp
{
    public class TcpClientProvider : IDisposable
    {
        #region variable
        private bool isConnected = false;
        private bool _isDisposed = false;
        private int blockSize = 4096;
        private int maxBufferNumber = 8;
        private byte[] receiveBuffer = null;
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
                sendBufferPool.Clear();
                //recBufferPool.Clear();
                _isDisposed = true;
            }
        }

        private void DisposeSocketPool()
        {
            while (tokenPool.Count > 0)
            {
                var item = tokenPool.Pop();
                if (item != null) item.Dispose();
            }
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="blockSize"></param>
        /// <param name="maxNumberOfConnections"></param>
        public TcpClientProvider(int blockSize = 4096, int maxNumberOfConnections = 8)
        {
            this.maxBufferNumber = maxNumberOfConnections;
            this.blockSize = blockSize;
            this.receiveBuffer = new byte[blockSize];
            tokenPool = new SocketTokenManager<SocketAsyncEventArgs>(maxNumberOfConnections);
            sendBufferPool = new SocketBufferManager(maxNumberOfConnections, blockSize);
            InitializePool(maxNumberOfConnections);
        }

        #endregion

        #region public method
        /// <summary>
        /// 异步建立连接
        /// </summary>
        /// <param name="port"></param>
        /// <param name="ip"></param>
        public void Connect(int port, string ip = "0.0.0.0")
        {
            try
            {
                if (isConnected ||
                    clientSocket != null)
                {
                    clientSocket.Disconnect(true);
                    clientSocket.Close();
                    clientSocket.Dispose();
                }

                isConnected = false;

                IPEndPoint ips = new IPEndPoint(IPAddress.Parse(ip), port);

                clientSocket = new Socket(ips.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = ips;
                args.UserToken = clientSocket;
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
        /// 异步发送数据
        /// </summary>
        /// <param name="buffer"></param>
        public void Send(byte[] buffer)
        {
            try
            {
                if (isConnected == false ||
                    clientSocket == null ||
                    clientSocket.Connected == false) return;

                SocketAsyncEventArgs tArgs = tokenPool.Pop();
                if (tArgs == null)
                    throw new Exception("发送连接池为空");

                if (!sendBufferPool.WriteBuffer(buffer, tArgs))
                {
                    tArgs.SetBuffer(buffer, 0, buffer.Length);
                }

                tArgs.UserToken = tArgs;

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

        public void SendFile(string filename)
        {
            clientSocket.SendFile(filename);
        }

        public int Send(byte[] buffer, Action<byte[]> recAct = null)
        {
            int sent = clientSocket.Send(buffer);
            if (recAct != null && sent > 0)
            {
                byte[] recBuffer = new byte[1024];
                List<byte> array = new List<byte>(recBuffer.Length);
                int cnt = 0;

                do
                {
                    cnt = clientSocket.Receive(recBuffer, recBuffer.Length, 0);
                    if (cnt > 0)
                    {
                        for (int i = 0; i < cnt; ++i)
                        {
                            array.Add(recBuffer[i]);
                        }
                    }

                } while (cnt > 0);
                recAct(array.ToArray());
            }
            return sent;
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

        /// <summary>
        /// 初始化发送对象池
        /// </summary>
        /// <param name="maxNumber"></param>
        private void InitializePool(int maxNumber)
        {
            tokenPool.Clear();

            for (int i = 0; i < maxNumber; ++i)
            {
                SocketAsyncEventArgs tArgs = new SocketAsyncEventArgs();
                tArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                tArgs.UserToken = new SocketToken(i);
                sendBufferPool.SetBuffer(tArgs);
                tokenPool.Push(tArgs);
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
                tokenPool.Push(e);

                isConnected = (e.SocketError == SocketError.Success);

                SentCallback?.Invoke(e.UserToken as SocketToken, e.BytesTransferred);
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
            try
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    ReceiveOffsetCallback?.Invoke(e.UserToken as SocketToken, e.Buffer, e.Offset, e.BytesTransferred);

                    if (RecievedCallback != null)
                    {
                        if (e.Offset == 0 && e.BytesTransferred == e.Buffer.Length)
                        {
                            RecievedCallback(e.UserToken as SocketToken, e.Buffer);
                        }
                        else
                        {
                            byte[] realBytes = new byte[e.BytesTransferred];

                            Buffer.BlockCopy(e.Buffer, e.Offset, realBytes, 0, e.BytesTransferred);
                            RecievedCallback(e.UserToken as SocketToken, realBytes);
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
                    isConnected = false;

                    DisconnectedCallback?.Invoke(e.UserToken as SocketToken);
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

                if (isConnected)
                {
                    e.SetBuffer(receiveBuffer, 0, blockSize);
                    if (!clientSocket.ReceiveAsync(e))
                    {
                        ProcessReceiveHandler(e);
                    }
                }

                ConnectedCallback?.Invoke(e.UserToken as SocketToken, isConnected);
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
                    Close(e);
                    break;
            }
        }
        #endregion
    }
}