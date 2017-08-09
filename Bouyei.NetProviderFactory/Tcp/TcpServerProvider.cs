using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Bouyei.NetProviderFactory.Tcp
{
    public class TcpServerProvider : IDisposable
    {
        #region variable
        private bool stoped = true;
        private bool _isDisposed = false;
        private int numberOfConnections = 0;
        private int maxNumber = 32;

        private Semaphore acceptSemphoreClients = null;
        private Socket svcSkt = null;
        private SocketTokenManager<SocketAsyncEventArgs> sendPool = null;
        private SocketTokenManager<SocketAsyncEventArgs> acceptPool = null;
        private SocketBufferManager recvBuffer = null;
        private SocketBufferManager sendBuffer = null;

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
                acceptSemphoreClients.Dispose();
                DisposeSocketPool();
                svcSkt.Dispose();
                recvBuffer.Clear();
                sendBuffer.Clear();
                _isDisposed = true;
            }
        }

        private void DisposeSocketPool()
        {
            while (sendPool.Count > 0)
            {
                var item = sendPool.Get();
                if (item != null) item.Dispose();
            }
            while (acceptPool.Count > 0)
            {
                var item = acceptPool.Get();
                if (item != null) item.Dispose();
            }
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="maxConnections">最大连接数</param>
        /// <param name="chunkBufferSize">接收块缓冲区</param>
        public TcpServerProvider(int maxConnections = 32, int chunkBufferSize = 4096)
        {
            this.maxNumber = maxConnections;

            acceptSemphoreClients = new Semaphore(maxConnections, maxConnections);

            recvBuffer = new SocketBufferManager(maxConnections, chunkBufferSize);
            acceptPool = new SocketTokenManager<SocketAsyncEventArgs>(maxConnections);

            //maxConnections = maxConnections >= 65536 ? (maxConnections >> 1) : maxConnections;

            sendPool = new SocketTokenManager<SocketAsyncEventArgs>(maxConnections);
            sendBuffer = new SocketBufferManager(maxConnections, chunkBufferSize);
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
                if (svcSkt != null)
                {
                    svcSkt.Close();
                    svcSkt.Dispose();
                }

                IPEndPoint ips = new IPEndPoint(IPAddress.Parse(ip), port);

                svcSkt = new Socket(ips.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                svcSkt.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                svcSkt.Bind(ips);

                svcSkt.Listen(10);

                stoped = false;

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
                stoped = true;

                if (numberOfConnections > 0)
                {
                    if (acceptSemphoreClients != null)
                        acceptSemphoreClients.Release(numberOfConnections);

                    numberOfConnections = 0;
                }

                if (svcSkt != null)
                {
                    svcSkt.Close();

                    svcSkt.Dispose();
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
        public void Send(SocketToken sToken, byte[] buffer, int offset, int size, bool waitingSignal = true)
        {
            try
            {
                //从连接池中取出一个发送的对象
                
                SocketAsyncEventArgs tArgs = sendPool.Get();
                //确保发送连接池不为空,否则尝试3次是否有可用发送缓冲对象
                if (tArgs == null)
                {
                    while (waitingSignal)
                    {
                        Thread.Sleep(500);
                        tArgs = sendPool.Get();
                        if (tArgs != null) break;
                    }
                }
                if (tArgs == null)
                    throw new Exception("发送缓冲池已用完,等待回收...");

                tArgs.UserToken = sToken;
                if (!sendBuffer.WriteBuffer(tArgs, buffer,offset,size))
                {
                    tArgs.SetBuffer(buffer, offset, size);
                }

                if (!sToken.TokenSocket.SendAsync(tArgs))
                {
                    ProcessSent(tArgs);
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
            acceptPool.Clear();
            SocketAsyncEventArgs args = null;
            for (int i = 0; i < maxNumber; ++i)
            {
                args = new SocketAsyncEventArgs();
                //args.DisconnectReuseSocket = true;
                args.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                args.UserToken = new SocketToken(i);
                recvBuffer.SetBuffer(args);
                acceptPool.Set(args);
            }
        }

        /// <summary>
        /// 初始化发送对象池
        /// </summary>
        private void InitializeSendPool()
        {
            sendPool.Clear();
            SocketAsyncEventArgs args = null;
            for (int i = 0; i < maxNumber; ++i)
            {
                args = new SocketAsyncEventArgs();
                //args.DisconnectReuseSocket = true;
                args.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                args.UserToken = new SocketToken(i);
                sendBuffer.SetBuffer(args);
                sendPool.Set(args);
            }
        }

        /// <summary>
        /// 启动接受客户端连接
        /// </summary>
        /// <param name="e"></param>
        private void StartAccept(SocketAsyncEventArgs e)
        {
            if (stoped) return;
            if (svcSkt == null) { stoped = true; return; }
            try
            {
                if (e == null)
                {
                    e = new SocketAsyncEventArgs();
                    e.DisconnectReuseSocket = true;
                    e.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                }
                else
                {
                    e.AcceptSocket = null;
                }
                acceptSemphoreClients.WaitOne();

                if (!svcSkt.AcceptAsync(e))
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
                if (stoped)
                {
                    if (e.ConnectSocket != null)
                    {
                        e.ConnectSocket.Close();
                        e.ConnectSocket.Dispose();
                        e.Dispose();
                    }
                    return;
                }

                //从对象池中取出一个对象
                SocketAsyncEventArgs tArgs = acceptPool.Get();
                if (maxNumber == numberOfConnections || tArgs == null)
                {
                    throw new Exception("已经达到最大连接数");
                }

                Interlocked.Increment(ref numberOfConnections);
                ((SocketToken)tArgs.UserToken).TokenSocket = e.AcceptSocket;
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

            if (stoped) return;

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
                if (stoped)
                {
                    if (e.ConnectSocket != null) e.ConnectSocket.Close();
                    if (e.AcceptSocket != null) e.AcceptSocket.Close();
                    acceptPool.Set(e);
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
                    //关闭异常对象
                    CloseClientSocket(sToken);
                    if (DisconnectedCallback != null)
                        DisconnectedCallback(sToken);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (e.SocketError == SocketError.Success)
                {
                    //继续投递下一个接受请求
                    if (!sToken.TokenSocket.ReceiveAsync(e))
                    {
                        this.ProcessReceive(e);
                    }
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
                //用完的对象放回对象池
                sendPool.Set(e);
                SocketToken sToken = e.UserToken as SocketToken;

                if (e.SocketError == SocketError.Success)
                {
                    //事件回调传递
                    if (SentCallback != null)
                        SentCallback(sToken, e.BytesTransferred);
                }
                else
                {
                    CloseClientSocket(sToken);
                }
            }
            catch (Exception ex)
            {
                throw ex;
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
                acceptPool.Set(e);

                //递减信号量
                acceptSemphoreClients.Release();

                if (DisconnectedCallback != null)
                    DisconnectedCallback(e.UserToken as SocketToken);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 内部使用的释放连接对象方法
        /// </summary>
        /// <param name="sToken"></param>
        private void CloseClientSocket(SocketToken sToken)
        {
            try
            {
                if (sToken != null)
                {
                    sToken.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
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