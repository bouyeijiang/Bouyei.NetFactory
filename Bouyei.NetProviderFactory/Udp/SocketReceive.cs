using System.Net;
using System;
using System.Net.Sockets;

namespace Bouyei.NetProviderFactory.Udp
{
    internal class SocketReceive:IDisposable
    {
        private Socket svcReceiveSocket = null;
        private IPEndPoint localEndPoint = null;
        private SocketTokenManager<SocketAsyncEventArgs> receivePool = null;
        private SocketBufferManager receiveBufferPool = null;
        private bool isClose = false;
        private bool _isDisposed = false;

        /// <summary>
        /// 接收事件
        /// </summary>
        public event EventHandler<SocketAsyncEventArgs> OnReceived;

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="port">本机接收数据端口</param>
        /// <param name="bufferSize">接收缓冲区大小</param>
        public SocketReceive(int port)
        {
            svcReceiveSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            localEndPoint = new IPEndPoint(IPAddress.Any, port);
            svcReceiveSocket.Bind(localEndPoint);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void DisposeSocketPool()
        {
            while (receivePool.Count > 0)
            {
                var item = receivePool.Get();
                if (item != null) item.Dispose();
            }
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;

            if (isDisposing)
            {
                DisposeSocketPool();
                receiveBufferPool.Clear();
                svcReceiveSocket.Dispose();
                _isDisposed = true;
            }
        }

        public void Initialize(int maxNumberOfConnections, int bufferSize = 2048)
        {
            receiveBufferPool = new SocketBufferManager(maxNumberOfConnections, bufferSize);
            receivePool = new SocketTokenManager<SocketAsyncEventArgs>(maxNumberOfConnections);

            for (int i = 0; i < maxNumberOfConnections; ++i)
            {
                SocketAsyncEventArgs socketArgs = new SocketAsyncEventArgs();
                socketArgs.Completed += SocketArgs_Completed;
                receiveBufferPool.SetBuffer(socketArgs);
                receivePool.Set(socketArgs);
            }
        }

        /// <summary>
        /// 开始接收数据
        /// </summary>
        public void StartReceive()
        {
            SocketAsyncEventArgs arg = receivePool.Get();

            if (!svcReceiveSocket.ReceiveFromAsync(arg))
            {
                ProcessReceive(arg);
            }
            isClose = false;
        }

        /// <summary>
        /// 停止接收数据
        /// </summary>
        public void StopReceive()
        {
            isClose = true;
        }

        /// <summary>
        /// 接收完成事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SocketArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.ReceiveFrom:
                    this.ProcessReceive(e);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 处理接收信息
        /// </summary>
        /// <param name="args"></param>
        private void ProcessReceive(SocketAsyncEventArgs args)
        {
            if (isClose) return;
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                if (OnReceived != null)
                {
                    OnReceived(svcReceiveSocket, args);
                }
            }

            receivePool.Set(args);

            StartReceive();
        }
    }
}