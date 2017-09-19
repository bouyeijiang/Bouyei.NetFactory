using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Bouyei.NetFactory.Tcp
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
            if (maxConnections < 2) maxConnections = 2;
            this.maxNumberOfConnections = maxConnections;

            maxNumberAcceptedClients = new Semaphore(maxConnections, maxConnections);

            recvBufferManager = new SocketBufferManager(maxConnections, chunkBufferSize);
            acceptTokenManager = new SocketTokenManager<SocketAsyncEventArgs>(maxConnections);

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
                CloseSocket(svcSocket);

                IPEndPoint ips = new IPEndPoint(IPAddress.Parse(ip), port);

                svcSocket = new Socket(ips.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                svcSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                svcSocket.LingerState = new LingerOption(true, 0);
                //svcSocket.UseOnlyOverlappedIO = true;
                //svcSocket.NoDelay = true;

                svcSocket.Bind(ips);

                svcSocket.Listen(maxNumberOfConnections >> 1);

                isStoped = false;

                StartAccept(null);
                return true;
            }
            catch (Exception ex)
            {
                CloseSocket(svcSocket);
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
                DisposePoolToken();

                if (numberOfConnections > 0)
                {
                    if (maxNumberAcceptedClients != null)
                        maxNumberAcceptedClients.Release(numberOfConnections);

                    numberOfConnections = 0;
                }
                Utils.SafeCloseSocket(svcSocket);
                isStoped = true;
            }
            catch (Exception ex)
            {
                
            }
        }

        public void Close(SocketToken sToken)
        {
            DisconnectAsyncEvent(sToken);
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
                    if (!sendBufferManager.WriteBuffer(tArgs, seg.Array, seg.Offset, seg.Count))
                    {
                        sendTokenManager.Set(tArgs);

                        throw new Exception(string.Format("发送缓冲区溢出...buffer block max size:{0}", sendBufferManager.BlockSize));
                    }
                    if (!sToken.SendAsync(tArgs))
                    {
                        ProcessSent(tArgs);
                    }

                    if (sendTokenManager.Count < (sendTokenManager.Capacity >> 1))
                        Thread.Sleep(5);
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

        private void DisposePoolToken()
        {
            sendTokenManager.ClearToCloseArgs();
            acceptTokenManager.ClearToCloseArgs();
        }

        /// <summary>
        /// 初始化接收对象池
        /// </summary>
        private void InitializeAcceptPool()
        {
            acceptTokenManager.Clear();
            for (int i = 0; i < maxNumberOfConnections; ++i)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs() {
                    DisconnectReuseSocket=true,
                    SocketError=SocketError.SocketError
                };
                args.Completed += Accept_Completed;
                args.UserToken = new SocketToken(i)
                {
                    TokenAgrs = args,
                };
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
            for (int i = 0; i < maxNumberOfConnections; ++i)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs() {
                    DisconnectReuseSocket=true,
                    SocketError=SocketError.SocketError
                };
                args.Completed += IO_Completed;
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
            if (isStoped || svcSocket == null)
            {
                isStoped = true;
                return;
            }
            if (e == null)
            {
                e = new SocketAsyncEventArgs()
                {
                    DisconnectReuseSocket = true,
                    UserToken = new SocketToken(-255)
                };
                e.Completed += Accept_Completed;
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

        /// <summary>
        /// 处理客户端连接
        /// </summary>
        /// <param name="e"></param>
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (isStoped 
                || maxNumberOfConnections <= numberOfConnections
                ||e.SocketError!=SocketError.Success)
            {
                DisposeSocketArgs(e);
                return;
            }

            //从对象池中取出一个对象
            SocketAsyncEventArgs tArgs = acceptTokenManager.GetEmptyWait(false);
            if (tArgs == null)
            {
                DisposeSocketArgs(e);
                return;
                //throw new Exception(string.Format("已经达到最大连接数max:{0};used:{1}",
                //    maxNumberOfConnections, numberOfConnections));
            }

            Interlocked.Increment(ref numberOfConnections);

            SocketToken sToken = ((SocketToken)tArgs.UserToken);
            sToken.TokenSocket = e.AcceptSocket;
            sToken.TokenAgrs = tArgs;
            sToken.TokenIpEndPoint = (IPEndPoint)e.AcceptSocket.RemoteEndPoint;
            tArgs.UserToken = sToken;

            //继续准备下一个接收
            if (!sToken.TokenSocket.ReceiveAsync(tArgs))
            {
                ProcessReceive(tArgs);
            }

            //将信息传递到自定义的方法
            if (AcceptedCallback != null)
                AcceptedCallback(sToken);

            if (isStoped) return;

            StartAccept(e);
        }

        /// <summary>
        /// 处理接收的数据
        /// </summary>
        /// <param name="e"></param>
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred == 0
                || e.SocketError != SocketError.Success)
            {
                ProcessDisconnect(e);
                return;
            }

            SocketToken sToken = e.UserToken as SocketToken;

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
            if (sToken.TokenSocket.Connected)
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

        /// <summary>
        /// 处理发送的数据
        /// </summary>
        /// <param name="e"></param>
        private void ProcessSent(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    if (SentCallback != null)
                    {
                        SocketToken sToken = e.UserToken as SocketToken;
                        SentCallback(sToken, e.Buffer, e.Offset, e.BytesTransferred);
                    }
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
            SocketToken sToken = e.UserToken as SocketToken;
            if (sToken == null) {
                e.Dispose();
                return;
            }

            try
            {
                sToken.Close();

                Interlocked.Decrement(ref numberOfConnections);
                //递减信号量
                maxNumberAcceptedClients.Release();

                //将断开的对象重新放回复用队列
                acceptTokenManager.Set(e);

                if (DisconnectedCallback != null)
                    DisconnectedCallback(sToken);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void DisposeSocketArgs(SocketAsyncEventArgs e)
        {
            SocketToken s = e.UserToken as SocketToken;
            if (s != null) s.Close();// if (e.UserToken is SocketToken s) --新语法
            e.Dispose();
        }

        private void CloseSocket(Socket s)
        {
            if (s == null) return;
            try
            {
                s.Shutdown(SocketShutdown.Both);
            }
            catch(ObjectDisposedException) { return; }
            catch { }
            try
            {
                s.Dispose();
            }
            catch { }
            s = null;
        }

        //slow close client socket
        private void DisconnectAsyncEvent(SocketToken sToken)
        {
            try
            {
                if (sToken == null
                    || sToken.TokenSocket == null
                    || sToken.TokenAgrs == null) return;

                if (sToken.TokenSocket.Connected)
                    sToken.TokenSocket.Shutdown(SocketShutdown.Send);
 
                SocketAsyncEventArgs args = new SocketAsyncEventArgs()
                {
                    DisconnectReuseSocket = true,
                    SocketError = SocketError.SocketError,
                    UserToken = null
                };
                args.Completed += Accept_Completed;
                if (sToken.TokenSocket.DisconnectAsync(args) == false)
                {
                    ProcessDisconnect(sToken.TokenAgrs);
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception ex)
            {

            }
        }

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

        void Accept_Completed(object send,SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    ProcessAccept(e);
                    break;
                case SocketAsyncOperation.Disconnect:
                    ProcessDisconnect(e);
                    break;
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSent(e);
                    break;
                default:
                    break;
            }
        }
        #endregion
    }
}