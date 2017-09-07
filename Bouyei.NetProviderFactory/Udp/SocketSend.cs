using System.Net;
using System;
using System.Net.Sockets;
using System.Threading;

namespace Bouyei.NetProviderFactory.Udp
{
    internal class SocketSend : IDisposable
    {
        #region variable
        private int maxCount = 0;
        private int blockSize = 0;
        private SocketTokenManager<SocketAsyncEventArgs> sendTokenManager = null;
        private SocketBufferManager sendBufferManager = null;
        private Socket sendSocket = null;
        private bool _isDisposed = false;

        #endregion

        #region structure
        /// <summary>
        /// 发送事件回调
        /// </summary>
        public event EventHandler<SocketAsyncEventArgs> SentEventHandler;

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
                sendSocket.Dispose();
                sendBufferManager.Clear();
                _isDisposed = true;
            }
        }
        #endregion

        #region public method 
        /// <summary>
        /// 初始化发送对象
        /// </summary>
        /// <param name="maxCountClient">客户端最大数</param>
        public SocketSend()
        {
            sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        /// <summary>
        /// 初始化客户端发送对象池
        /// </summary>
        /// <param name="maxCountClient"></param>
        /// <param name="blockSize"></param>
        public void Initialize(int maxCountClient, int blockSize = 4096)
        {
            this.maxCount = maxCountClient;
            this.blockSize = blockSize;
            sendTokenManager = new SocketTokenManager<SocketAsyncEventArgs>(maxCountClient);
            sendBufferManager = new SocketBufferManager(maxCountClient, blockSize);

            for (int i = 0; i < maxCount; ++i)
            {
                SocketAsyncEventArgs socketArgs = new SocketAsyncEventArgs
                {
                    UserToken = sendSocket
                };
                socketArgs.Completed += ClientSocket_Completed;
                sendBufferManager.SetBuffer(socketArgs);
                sendTokenManager.Set(socketArgs);
            }
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="isWaiting"></param>
        /// <param name="remoteEP"></param>
        public void Send(byte[] buffer, int offset, int size, bool isWaiting, IPEndPoint remoteEP)
        {
            try
            {
                ArraySegment<byte>[] segItems = sendBufferManager.BufferToSegments(buffer, offset, size);
                foreach (var seg in segItems)
                {
                    var tArgs = sendTokenManager.GetEmptyWait(isWaiting);
                    if (tArgs == null)
                        throw new Exception("发送缓冲池已用完,等待回收超时...");

                    tArgs.RemoteEndPoint = remoteEP;
                    Socket s = SocketVersion(remoteEP);
                    tArgs.UserToken = s;

                    if (!sendBufferManager.WriteBuffer(tArgs, seg.Array, seg.Offset, seg.Count))
                    {
                        sendTokenManager.Set(tArgs);

                        throw new Exception(string.Format("发送缓冲区溢出...buffer block max size:{0}", sendBufferManager.BlockSize));
                    }

                    if (tArgs.RemoteEndPoint != null)
                    {
                        if (!s.SendToAsync(tArgs))
                        {
                            ProcessSent(tArgs);
                        }
                    }
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
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="remoteEP"></param>
        /// <returns></returns>
        public int SendSync(byte[] data, int offset, int size, IPEndPoint remoteEP)
        {
            return SocketVersion(remoteEP).SendTo(data, offset, size, SocketFlags.None, remoteEP);
        }

        #endregion

        #region private method
        /// <summary>
        /// 释放缓冲池
        /// </summary>
        private void DisposeSocketPool()
        {
            sendTokenManager.ClearToCloseArgs();
        }

        /// <summary>
        /// 获取socket版本
        /// </summary>
        /// <param name="ips"></param>
        /// <returns></returns>
        private Socket SocketVersion(IPEndPoint ips)
        {
            if (ips.AddressFamily == sendSocket.AddressFamily)
            {
                return sendSocket;
            }
            else
            {
                sendSocket = new Socket(ips.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            }
            return sendSocket;
        }

        /// <summary>
        /// 处理发送的数据
        /// </summary>
        /// <param name="e"></param>
        private void ProcessSent(SocketAsyncEventArgs e)
        {
            sendTokenManager.Set(e);

            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                if (SentEventHandler != null)
                {
                    SentEventHandler(e.UserToken as Socket, e);
                }
            }
        }

        /// <summary>
        /// 完成发送事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ClientSocket_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.SendTo:
                    ProcessSent(e);
                    break;
                default:
                    break;
            }
        }
        #endregion
    }
}