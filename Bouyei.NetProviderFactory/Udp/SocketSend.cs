using System.Net;
using System;
using System.Net.Sockets;

namespace Bouyei.NetProviderFactory.Udp
{
    internal class SocketSend:IDisposable
    {
        private int maxCount = 0;
        private int blocksize = 0;
        private SocketTokenManager<SocketAsyncEventArgs> tokenPool = null;
        private SocketBufferManager sentBufferPool = null;
        private Socket clientSocket = null;
        private bool _isDisposed = false;
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
                clientSocket.Dispose();
                sentBufferPool.Clear();
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
        /// 初始化发送对象
        /// </summary>
        /// <param name="maxCountClient">客户端最大数</param>
        public SocketSend()
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        /// <summary>
        /// 初始化客户端发送对象池
        /// </summary>
        public void Initialize(int maxCountClient, int blockSize = 4096)
        {
            this.maxCount = maxCountClient;
            this.blocksize = blockSize;
            tokenPool = new SocketTokenManager<SocketAsyncEventArgs>(maxCountClient);
            sentBufferPool = new SocketBufferManager(maxCountClient, blockSize);

            for (int i = 0; i < maxCount; ++i)
            {
                SocketAsyncEventArgs socketArgs = new SocketAsyncEventArgs();
                socketArgs.Completed += new EventHandler<SocketAsyncEventArgs>(ClientSocket_Completed);
                sentBufferPool.SetBuffer(socketArgs);
                tokenPool.Push(socketArgs);
            }
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="remoteEP"></param>
        public void Send(byte[] data,int offset,int size ,IPEndPoint remoteEP)
        {
            SocketAsyncEventArgs socketArgs = null;
            try
            {
                socketArgs = tokenPool.Pop();
                //如果发送对象池已经为空
                if (socketArgs == null)
                {
                    Initialize(maxCount,blocksize);
                    socketArgs = tokenPool.Pop();
                }
                socketArgs.RemoteEndPoint = remoteEP;
                
                if(!sentBufferPool.WriteBuffer(socketArgs,data,offset,size))
                {
                    throw new Exception("设置发送缓冲区失败");
                }

                if (socketArgs.RemoteEndPoint != null)
                {
                    if (!clientSocket.SendToAsync(socketArgs))
                    {
                        ProcessSent(socketArgs);
                    }
                }
            }
            catch (Exception ex)
            {
                tokenPool.Push(socketArgs);
                
                throw ex;
            }
        }

        /// <summary>
        /// 处理发送的数据
        /// </summary>
        /// <param name="e"></param>
        private void ProcessSent(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                if (SentEventHandler != null)
                {
                    SentEventHandler(clientSocket, e);
                }
            }
            tokenPool.Push(e);
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
    }
}