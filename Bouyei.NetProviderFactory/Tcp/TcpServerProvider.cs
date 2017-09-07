using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Bouyei.NetProviderFactory.Tcp
{
    public class TcpServerProvider : IDisposable
    {
        #region variable
        private bool isStoped = true;
        private bool _isDisposed = false;
        private int numberOfConnections = 0;
        private int maxNumberOfConnections = 32;

        private Semaphore maxNumberAcceptedClients = null;
        private Socket svcSocket = null;
        private SocketTokenManager<SocketAsyncEventArgs> sendTokenManager = null;
        private SocketTokenManager<SocketAsyncEventArgs> acceptTokenManager = null;
        private SocketBufferManager recvBufferManager = null;
        private SocketBufferManager sendBufferManager = null;

        #endregion

        #region property
        /// <summary>
        /// 接受连接回调处理
        /// </summary>
        public OnAcceptHandler AcceptedCallback { get; set; }

        /// <summary>
        /// 接收数据回调处理
        /// </summary>
        public OnReceiveHandler ReceivedCallback { get; set; }

        /// <summary>
        ///接收数据缓冲区，返回缓冲区的实际偏移和数量
        /// </summary>
        public OnReceiveOffsetHandler ReceiveOffsetCallback { get; set; }

        /// <summary>
        /// 发送回调处理
        /// </summary>
        public OnSentHandler SentCallback { get; set; }

        /// <summary>
        /// 断开连接回调处理
        /// </summary>
        public OnDisconnectedHandler DisconnectedCallback { get; set; }

        /// <summary>
        /// 连接数
        /// </summary>
        public int NumberOfConnections
        {
            get { return numberOfConnections; }
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
                svcSocket.Dispose();
                recvBufferManager.Clear();
                sendBufferManager.Clear();
                _isDisposed = true;
                maxNumberAcceptedClients.Dispose();
            }
        }

        private void DisposeSocketPool()
        {
            sendTokenManager.ClearToCloseArgs();
            acceptTokenManager.ClearToCloseArgs();
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="maxConnections">最大连接数</param>
        /// <param name="chunkBufferSize">接收块缓冲区</param>
        public TcpServerProvider(int maxConnections = 32, int chunkBufferSize = 4096)
        {
            this.maxNumberOfConnections = maxConnections;

            maxNumberAcceptedClients = new Semaphore(maxConnections, maxConnections);

            recvBufferManager = new SocketBufferManager(maxConnections, chunkBufferSize);
            acceptTokenManager = new SocketTokenManager<SocketAsyncEventArgs>(maxConnections);

            //maxConnections = maxConnections >= 65536 ? (maxConnections >> 1) : maxConnections;

            sendTokenManager = new SocketTokenManager<SocketAsyncEventArgs>(maxConnections);
            sendBufferManager = new SocketBufferManager(maxConnections, chunkBufferSize);
        }

        #endregion

        #region public method

        /// <summary>
        /// 启动socket监听服务
        /// </summary>
        /// <param name="port"></param>
        /// <param name="ip"></param>
        public bool Start(int port, string ip = "0.0.0.0")
        {
            int errorCount = 0;
            Stop();
            InitializeAcceptPool();
            InitializeSendPool();

            reStart:
            try
            {
                if (svcSocket != null)
                {
                    svcSocket.Close();
                    svcSocket.Dispose();
                }

                IPEndPoint ips = new IPEndPoint(IPAddress.Parse(ip), port);

                svcSocket = new Socket(ips.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                svcSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                svcSocket.Bind(ips);

                svcSocket.Listen(10);

                isStoped = false;

                StartAccept(null);
                return true;
            }
            catch (Exception ex)
            {
                ++errorCount;

                if (errorCount >= 3)
                {
                    throw ex;
                }
                else
                {
                    Thread.Sleep(1000);
                    goto reStart;
                }
            }
        }

        /// <summary>
        /// 停止socket监听服务
        /// </summary>
        public void Stop()
        {
            try
            {
                isStoped = true;

                if (numberOfConnections > 0)
                {
                    if (maxNumberAcceptedClients != null)
                        maxNumberAcceptedClients.Release(numberOfConnections);

                    numberOfConnections = 0;
                }

               Utils.SafeCloseSocket(svcSocket);
            }
            catch (Exception ex)
            {
                
            }
        }

        /// <summary>
        /// 异步发送数据
        /// </summary>
        public void Send(SocketToken sToken, byte[] buffer, 
            int offset, int size, bool isWaiting = true)
        {
            try
            {
                ArraySegment<byte>[] segItems = sendBufferManager.BufferToSegments(buffer, offset, size);
                foreach (var seg in segItems)
                {
                    var tArgs = sendTokenManager.GetEmptyWait(isWaiting);

                    if (tArgs == null)
                        throw new Exception("发送缓冲池已用完,等待回收超时...");

                    tArgs.UserToken = sToken;
                    if (!sendBufferManager.WriteBuffer(tArgs, seg.Array,seg.Offset, seg.Count))
                    {
                        sendTokenManager.Set(tArgs);

                        throw new Exception(string.Format("发送缓冲区溢出...buffer block max size:{0}", sendBufferManager.BlockSize));
                    }
                    if (!sToken.TokenSocket.SendAsync(tArgs))
                    {
                        ProcessSent(tArgs);
                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 同步发送数据
        /// </summary>
        /// <param name="sToken"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public int SendSync(SocketToken sToken, byte[] buffer)
        {
            return sToken.TokenSocket.Send(buffer);
        }

        /// <summary>
        /// 同步发送偏移数据
        /// </summary>
        /// <param name="sToken"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public int SendSync(SocketToken sToken, byte[] buffer, int offset, int size)
        {
            return sToken.TokenSocket.Send(buffer, offset, size, SocketFlags.None);
        }

        #endregion

        #region private method

        /// <summary>
        /// 初始化接收对象池
        /// </summary>
        private void InitializeAcceptPool()
        {
            acceptTokenManager.Clear();
            SocketAsyncEventArgs args = null;
            for (int i = 0; i < maxNumberOfConnections; ++i)
            {
                args = new SocketAsyncEventArgs();
                //args.DisconnectReuseSocket = true;
                args.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                args.UserToken = new SocketToken(i);
                recvBufferManager.SetBuffer(args);
                acceptTokenManager.Set(args);
            }
        }

        /// <summary>
        /// 初始化发送对象池
        /// </summary>
        private void InitializeSendPool()
        {
            sendTokenManager.Clear();
            SocketAsyncEventArgs args = null;
            for (int i = 0; i < maxNumberOfConnections; ++i)
            {
                args = new SocketAsyncEventArgs();
                //args.DisconnectReuseSocket = true;
                args.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                args.UserToken = new SocketToken(i);
                sendBufferManager.SetBuffer(args);
                sendTokenManager.Set(args);
            }
        }

        /// <summary>
        /// 启动接受客户端连接
        /// </summary>
        /// <param name="e"></param>
        private void StartAccept(SocketAsyncEventArgs e)
        {
            if (isStoped ||svcSocket == null)
            {
                isStoped = true;
                return;
            }
            try
            {
                if (e == null)
                {
                    e = new SocketAsyncEventArgs
                    {
                        //DisconnectReuseSocket = true
                    };
                    e.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                }
                else
                {
                    e.AcceptSocket = null;
                }
                maxNumberAcceptedClients.WaitOne();

                if (!svcSocket.AcceptAsync(e))
                {
                    ProcessAccept(e);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 处理客户端连接
        /// </summary>
        /// <param name="e"></param>
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            try
            {
                if (isStoped)
                {
                    CloseToken(e);
                    return;
                }

                //从对象池中取出一个对象
                SocketAsyncEventArgs tArgs = acceptTokenManager.Get();
                if (maxNumberOfConnections == numberOfConnections || tArgs == null)
                {
                    CloseToken(e);

                    throw new Exception("已经达到最大连接数");
                }

                Interlocked.Increment(ref numberOfConnections);

                SocketToken sToken= ((SocketToken)tArgs.UserToken);
                sToken.TokenSocket = e.AcceptSocket;
                sToken.TokenIpEndPoint = (IPEndPoint)e.AcceptSocket.RemoteEndPoint;
                tArgs.UserToken = sToken;

                //继续准备下一个接收
                if (!e.AcceptSocket.ReceiveAsync(tArgs))
                {
                    ProcessReceive(tArgs);
                }

                //将信息传递到自定义的方法
                if (AcceptedCallback != null)
                    AcceptedCallback(tArgs.UserToken as SocketToken);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            if (isStoped) return;

            //继续监听
            StartAccept(e);
        }

        /// <summary>
        /// 处理接收的数据
        /// </summary>
        /// <param name="e"></param>
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            SocketToken sToken = null;
            try
            {
                if (isStoped)
                {
                    CloseToken(e);
                    return;
                }

                sToken = e.UserToken as SocketToken;

                if (e.BytesTransferred > 0 && e.SocketError==SocketError.Success)
                {
                    if (ReceiveOffsetCallback != null)
                        ReceiveOffsetCallback(sToken, e.Buffer, e.Offset, e.BytesTransferred);

                    //处理接收到的数据
                    if (ReceivedCallback != null)
                    {
                        if (e.Offset == 0 && e.BytesTransferred == e.Buffer.Length)
                        {
                            ReceivedCallback(sToken, e.Buffer);
                        }
                        else
                        {
                            byte[] realBytes = new byte[e.BytesTransferred];
                            Buffer.BlockCopy(e.Buffer, e.Offset, realBytes, 0, e.BytesTransferred);
                            ReceivedCallback(sToken, realBytes);
                        }
                    }
                }
                else
                {
                   CloseToken(sToken);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (e.SocketError == SocketError.Success
                    &&e.BytesTransferred>0
                    &&sToken.TokenSocket.Connected)
                {
                    //继续投递下一个接受请求
                    if (!sToken.TokenSocket.ReceiveAsync(e))
                    {
                        this.ProcessReceive(e);
                    }
                }
                else
                {
                    ProcessDisconnect(e);
                }
            }
        }

        /// <summary>
        /// 处理发送的数据
        /// </summary>
        /// <param name="e"></param>
        private void ProcessSent(SocketAsyncEventArgs e)
        {
            try
            {
                SocketToken sToken = e.UserToken as SocketToken;

                if (e.SocketError == SocketError.Success)
                {
                    if (SentCallback != null)
                        SentCallback(sToken, e.Buffer, e.Offset, e.BytesTransferred);
                }
                else
                {
                    CloseToken(sToken);  
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
        /// 处理断开连接
        /// </summary>
        /// <param name="e"></param>
        private void ProcessDisconnect(SocketAsyncEventArgs e)
        {
            try
            {
                Interlocked.Decrement(ref numberOfConnections);
               
                //将断开的对象重新放回复用队列
                acceptTokenManager.Set(e);

                //递减信号量
                maxNumberAcceptedClients.Release();

                if (DisconnectedCallback != null)
                    DisconnectedCallback(e.UserToken as SocketToken);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 关闭连接对象方法
        /// </summary>
        /// <param name="sToken"></param>
        public void CloseToken(SocketToken sToken)
        {
            try
            {
                if (sToken != null) sToken.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void CloseToken(SocketAsyncEventArgs e)
        {
            try
            {
                SocketToken sToken = e.UserToken as SocketToken;
                if (sToken != null) sToken.Close();
            }
            catch (Exception)
            {

            }
        }

        //针对每一个连接的对象事件,当一个接受、发送、连接等操作完成时响应
        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSent(e);
                    break;
                case SocketAsyncOperation.Disconnect:
                    ProcessDisconnect(e);
                    break;
                case SocketAsyncOperation.Accept:
                    ProcessAccept(e);
                    break;
                default:
                    break;
            }
        }
        #endregion
    }
}