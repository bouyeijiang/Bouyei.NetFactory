using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections.Generic;

namespace Bouyei.NetProviderFactory.Udp
{
    public class UdpClientProvider:IDisposable
    {
        #region 定义变量
        private bool _isDisposed = false;
        private EndPoint serverIpEndPoint = null;
        private int bufferSizeByConnection = 4096;
        private int maxNumberOfConnections = 64;
        private byte[] receiveBuffer = null;
        private bool isConnected = false;
        private ManualResetEvent mReset = new ManualResetEvent(false);
        private SocketTokenManager<SocketAsyncEventArgs> sendPool = null;
        private SocketBufferManager sendBuffer = null;

        private Socket clientSocket = null;
        
        #endregion

        #region 属性
        public int SendBufferPoolNumber { get { return sendPool.Count; } }

        /// <summary>
        /// 接收回调处理
        /// </summary>
        public OnReceiveHandler ReceiveCallbackHandler { get; set; }

        /// <summary>
        /// 发送回调处理
        /// </summary>
        public OnSentHandler SentCallbackHandler { get; set; }
        /// <summary>
        /// 接收缓冲区回调
        /// </summary>
        public OnReceiveOffsetHandler ReceiveOffsetHandler { get; set; }
        #endregion

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

                if (clientSocket != null)
                {
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                    clientSocket.Dispose();
                }
                _isDisposed = true;
            }
        }

        private void DisposeSocketPool()
        {
            if (sendPool != null)
            {
                while (sendPool.Count > 0)
                {
                    var item = sendPool.Get();
                    if (item != null) item.Dispose();
                }
            }
            if (sendBuffer != null)
            {
                sendBuffer.Clear();
            }
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        public UdpClientProvider(int bufferSizeByConnection,int maxNumberOfConnections)
        {
            this.maxNumberOfConnections = maxNumberOfConnections;
            this.bufferSizeByConnection = bufferSizeByConnection;
            receiveBuffer = new byte[bufferSizeByConnection];
            Initialize();
        }

        public void Disconnect()
        {
            isConnected = false;
            if (clientSocket != null)
            {
                clientSocket.Close();
            }
        }

        /// <summary>
        /// 初始化对象
        /// </summary>
        /// <param name="recBufferSize"></param>
        /// <param name="port"></param>
        private void Initialize()
        {
            sendPool = new SocketTokenManager<SocketAsyncEventArgs>(maxNumberOfConnections);
            sendBuffer = new SocketBufferManager(maxNumberOfConnections, bufferSizeByConnection);
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            //初始化发送接收对象池
            for (int i = 0; i < maxNumberOfConnections; ++i)
            {
                SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
                sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                sendArgs.UserToken = clientSocket;
                sendBuffer.SetBuffer(sendArgs);
                sendPool.Set(sendArgs);
            }
        }

        /// <summary>
        /// 尝试连接
        /// </summary>
        /// <param name="port"></param>
        /// <param name="ip"></param>
        /// <returns></returns>
        public bool Connect(int port, string ip)
        {
            serverIpEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            int retry = 3;
            again:
            try
            {               
                //探测是否有效连接
                SocketAsyncEventArgs sArgs = new SocketAsyncEventArgs();
                sArgs.Completed += IO_Completed;
                sArgs.UserToken = clientSocket;
                sArgs.RemoteEndPoint = serverIpEndPoint;
                sArgs.SetBuffer(new byte[] { 0 }, 0, 1);
 
                bool rt = clientSocket.SendToAsync(sArgs);
                if (rt)
                {
                    StartReceive();
                    mReset.WaitOne();
                }
            }
            catch (Exception ex)
            {
                retry -= 1;
                if (retry > 0)
                {
                    Thread.Sleep(1000);
                    goto again;
                }
                throw ex;
            }
            return isConnected;
        }

        /// <summary>
        /// 异步发送
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="waitingSignal"></param>
        public void Send(byte[] buffer,int offset,int size,bool waitingSignal=true)
        {
            SocketAsyncEventArgs sendArgs = sendPool.Get();
            if (sendArgs == null)
            {
                while (waitingSignal)
                {
                    Thread.Sleep(1000);
                    sendArgs = sendPool.Get();
                    if (sendArgs != null) break;
                }
            }
            if (sendArgs == null)
                throw new Exception("发送缓冲池已用完,等待回收...");

            sendArgs.RemoteEndPoint = serverIpEndPoint;

            if (!sendBuffer.WriteBuffer(sendArgs,buffer,offset,size))
            {
                sendArgs.SetBuffer(buffer, offset, size);
            }
            Socket s = sendArgs.UserToken as Socket;

            if (!s.SendToAsync(sendArgs))
            {
                ProcessSent(sendArgs);
            }
        }

        /// <summary>
        /// 同步发送
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="recAct"></param>
        /// <param name="recBufferSize"></param>
        /// <returns></returns>
        public int SendSync(byte[] buffer, Action<int, byte[]> recAct = null, int recBufferSize = 4096)
        {
            int sent = clientSocket.SendTo(buffer, serverIpEndPoint);
            if (recAct != null && sent > 0)
            {
                byte[] recBuffer = new byte[recBufferSize];

                int cnt = clientSocket.ReceiveFrom(recBuffer,
                    recBuffer.Length,
                    SocketFlags.None,
                    ref serverIpEndPoint);

                recAct(cnt, recBuffer);
            }
            return sent;
        }

        /// <summary>
        /// 同步接收
        /// </summary>
        /// <param name="recAct"></param>
        /// <param name="recBufferSize"></param>
        public void ReceiveSync(Action<int, byte[]> recAct, int recBufferSize = 4096)
        {
            int cnt = 0;
            byte[] buffer = new byte[recBufferSize];
            do
            {
                cnt = clientSocket.ReceiveFrom(buffer, 
                    buffer.Length,
                    SocketFlags.None,
                    ref serverIpEndPoint);
                if (cnt > 0)
                {
                    recAct(cnt, buffer);
                }
            } while (cnt > 0);
        }

        /// <summary>
        /// 开始接收数据
        /// </summary>
        /// <param name="remoteEP"></param>
        public void StartReceive()
        {
            SocketAsyncEventArgs sArgs = new SocketAsyncEventArgs();
            sArgs.Completed += IO_Completed;
            sArgs.UserToken = clientSocket;
            sArgs.RemoteEndPoint = serverIpEndPoint;
            sArgs.SetBuffer(receiveBuffer, 0, bufferSizeByConnection);
            if (!clientSocket.ReceiveFromAsync(sArgs))
            {
                ProcessReceive(sArgs);
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            SocketToken sToken = new SocketToken()
            {
                TokenSocket = e.UserToken as Socket,
                TokenIpEndPoint = (IPEndPoint)e.RemoteEndPoint
            };

            try
            {
                if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
                {
                    //初次连接心跳
                    if (isRetryConnect(e) == false)
                    {
                        //缓冲区偏移量返回
                        if (ReceiveOffsetHandler != null)
                            ReceiveOffsetHandler(sToken, e.Buffer, e.Offset, e.BytesTransferred);

                        //截取后返回
                        if (ReceiveCallbackHandler != null)
                        {
                            if (e.Offset == 0 && e.BytesTransferred == e.Buffer.Length)
                            {
                                ReceiveCallbackHandler(sToken, e.Buffer);
                            }
                            else
                            {
                                byte[] realbytes = new byte[e.BytesTransferred];
                                Buffer.BlockCopy(e.Buffer, e.Offset, realbytes, 0, e.BytesTransferred);

                                ReceiveCallbackHandler(sToken, realbytes);
                            }
                        }
                    }
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
                    //继续下一个接收
                    if (!sToken.TokenSocket.ReceiveFromAsync(e))
                    {
                          ProcessReceive(e);
                    }
                }
            }
        }

        private void ProcessSent(SocketAsyncEventArgs e)
        {
            try
            {
                sendPool.Set(e);

                isConnected = e.SocketError == SocketError.Success;

                if (SentCallbackHandler != null)
                {
                    SocketToken sToken = new SocketToken()
                    {
                        TokenSocket = e.UserToken as Socket,
                        TokenIpEndPoint = (IPEndPoint)e.RemoteEndPoint
                    };
                    SentCallbackHandler(sToken, e.BytesTransferred);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.ReceiveFrom:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.SendTo:
                    ProcessSent(e);
                    break;
            }
        }

        private bool isRetryConnect(SocketAsyncEventArgs e)
        {
            isConnected = e.SocketError == SocketError.Success;

            if (e.BytesTransferred == 1 && e.Buffer[0] == 1)
            {
                mReset.Set();
                return true;
            }
            else return false;
        }
    }
}