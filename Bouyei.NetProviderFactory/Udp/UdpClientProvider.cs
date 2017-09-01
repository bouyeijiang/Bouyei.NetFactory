using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections.Generic;

namespace Bouyei.NetProviderFactory.Udp
{
    public class UdpClientProvider : IDisposable
    {
        #region 定义变量
        private bool _isDisposed = false;
        private EndPoint serverIpEndPoint = null;
        private int bufferSizeByConnection = 4096;
        private int maxNumberOfConnections = 64;
        private byte[] cliRecBuffer = null;
        private bool isConnected = false;
        private Socket cliSocket = null;

        private ManualResetEvent mReset = new ManualResetEvent(false);
        private SocketTokenManager<SocketAsyncEventArgs> sendTokenManager = null;
        private SocketBufferManager sendBufferManager = null;

        #endregion

        #region 属性
        public int SendBufferPoolNumber { get { return sendTokenManager.Count; } }

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

        #region public method
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

                if (cliSocket != null)
                {
                    cliSocket.Shutdown(SocketShutdown.Both);
                    cliSocket.Close();
                    cliSocket.Dispose();
                }
                _isDisposed = true;
            }
        }

        private void DisposeSocketPool()
        {
            if (sendTokenManager != null)
            {
                while (sendTokenManager.Count > 0)
                {
                    var item = sendTokenManager.Get();
                    if (item != null) item.Dispose();
                }
            }
            if (sendBufferManager != null)
            {
                sendBufferManager.Clear();
            }
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        public UdpClientProvider(int bufferSizeByConnection, int maxNumberOfConnections)
        {
            this.maxNumberOfConnections = maxNumberOfConnections;
            this.bufferSizeByConnection = bufferSizeByConnection;
            cliRecBuffer = new byte[bufferSizeByConnection];
            Initialize();
        }

        public void Disconnect()
        {
            Close();
            isConnected = false;
        }

        /// <summary>
        /// 尝试连接
        /// </summary>
        /// <param name="port"></param>
        /// <param name="ip"></param>
        /// <returns></returns>
        public bool Connect(int port, string ip)
        {
            Close();

            serverIpEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            cliSocket = new Socket(serverIpEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            cliSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);

            int retry = 3;
            again:
            try
            {
                //探测是否有效连接
                SocketAsyncEventArgs sArgs = new SocketAsyncEventArgs();
                sArgs.Completed += IO_Completed;
                sArgs.UserToken = cliSocket;
                sArgs.RemoteEndPoint = serverIpEndPoint;
                sArgs.SetBuffer(new byte[] { 0 }, 0, 1);

                bool rt = cliSocket.SendToAsync(sArgs);
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
        /// <param name="isWaiting"></param>
        public void Send(byte[] buffer, int offset, int size, bool isWaiting = true)
        {
            ArraySegment<byte>[] segItems = sendBufferManager.BufferToSegments(buffer, offset, size);
            foreach (var seg in segItems)
            {
                SocketAsyncEventArgs tArgs = sendTokenManager.Get();
                if (tArgs == null)
                {
                    while (isWaiting)
                    {
                        Thread.Sleep(1000);
                        tArgs = sendTokenManager.Get();
                        if (tArgs != null) break;
                    }
                }
                if (tArgs == null)
                    throw new Exception("发送缓冲池已用完,等待回收...");

                tArgs.RemoteEndPoint = serverIpEndPoint;

                if (!sendBufferManager.WriteBuffer(tArgs, seg.Array, seg.Offset, seg.Count))
                {
                    sendTokenManager.Set(tArgs);

                    throw new Exception(string.Format("发送缓冲区溢出...buffer block max size:{0}", sendBufferManager.BlockSize));
                }

                if (!cliSocket.SendToAsync(tArgs))
                {
                    ProcessSent(tArgs);
                }
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
            int sent = cliSocket.SendTo(buffer, serverIpEndPoint);
            if (recAct != null && sent > 0)
            {
                byte[] recBuffer = new byte[recBufferSize];

                int cnt = cliSocket.ReceiveFrom(recBuffer,
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
                cnt = cliSocket.ReceiveFrom(buffer,
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
            sArgs.UserToken = cliSocket;
            sArgs.RemoteEndPoint = serverIpEndPoint;
            sArgs.SetBuffer(cliRecBuffer, 0, bufferSizeByConnection);
            if (!cliSocket.ReceiveFromAsync(sArgs))
            {
                ProcessReceive(sArgs);
            }
        }

        #endregion

        #region private method
        /// <summary>
        /// 初始化对象
        /// </summary>
        /// <param name="recBufferSize"></param>
        /// <param name="port"></param>
        private void Initialize()
        {
            sendTokenManager = new SocketTokenManager<SocketAsyncEventArgs>(maxNumberOfConnections);
            sendBufferManager = new SocketBufferManager(maxNumberOfConnections, bufferSizeByConnection);

            //初始化发送接收对象池
            for (int i = 0; i < maxNumberOfConnections; ++i)
            {
                SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
                sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                sendArgs.UserToken = cliSocket;
                sendBufferManager.SetBuffer(sendArgs);
                sendTokenManager.Set(sendArgs);
            }
        }

        private void Close()
        {
            if (cliSocket != null)
            {
                cliSocket.Shutdown(SocketShutdown.Both);
                cliSocket.Close();
                cliSocket.Dispose();
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
                    if (isServerResponse(e) == false)
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
                isConnected = e.SocketError == SocketError.Success;

                if (SentCallbackHandler != null && isClientRequest(e)==false)
                {
                    SocketToken sToken = new SocketToken()
                    {
                        TokenSocket = e.UserToken as Socket,
                        TokenIpEndPoint = (IPEndPoint)e.RemoteEndPoint
                    };
                    SentCallbackHandler(sToken, e.Buffer,e.Offset,e.BytesTransferred);
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

        private bool isServerResponse(SocketAsyncEventArgs e)
        {
            isConnected = e.SocketError == SocketError.Success;

            if (e.BytesTransferred == 1 && e.Buffer[0] == 1)
            {
                mReset.Set();
                return true;
            }
            else return false;
        }

        private bool isClientRequest(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred == 1 && e.Buffer[0] == 0)
            {
                return true;
            }
            else return false;
        }
        #endregion
    }
}