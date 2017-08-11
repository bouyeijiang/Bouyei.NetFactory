using System.Net;
using System;
using System.Net.Sockets;

namespace Bouyei.NetProviderFactory.Udp
{
    internal class SocketReceive : IDisposable
    {
        #region variable
        private Socket receiveSocket = null;
        //private SocketTokenManager<SocketAsyncEventArgs> receivePool = null;
        //private SocketBufferManager receiveBufferPool = null;
        private SocketAsyncEventArgs socketArgs = null;
        private byte[] recBuffer = null;
        private bool isClose = false;
        private bool _isDisposed = false;

        /// <summary>
        /// 接收事件
        /// </summary>
        public event EventHandler<SocketAsyncEventArgs> OnReceived;

        #endregion

        #region structure
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="port">本机接收数据端口</param>
        /// <param name="bufferSize">接收缓冲区大小</param>
        public SocketReceive(int port)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);
            receiveSocket = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            receiveSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);

            receiveSocket.Bind(localEndPoint);
        }


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
                isClose = true;
                receiveSocket.Dispose();
                socketArgs.Dispose();
                DisposeSocketPool();
                _isDisposed = true;
            }
        }
        #endregion

        #region public
        public void Initialize(int maxNumberOfConnections, int bufferSize = 4096)
        {
            //receiveBufferPool = new SocketBufferManager(maxNumberOfConnections, bufferSize);
            //receivePool = new SocketTokenManager<SocketAsyncEventArgs>(maxNumberOfConnections);
            socketArgs = new SocketAsyncEventArgs();

            socketArgs.UserToken = receiveSocket;
            socketArgs.RemoteEndPoint = receiveSocket.LocalEndPoint;
            socketArgs.Completed += SocketArgs_Completed;
            recBuffer = new byte[bufferSize];
            socketArgs.SetBuffer(recBuffer, 0, bufferSize);

            //for (int i = 0; i < maxNumberOfConnections; ++i)
            //{
            //    SocketAsyncEventArgs socketArgs = new SocketAsyncEventArgs();
            //    socketArgs.UserToken = receiveSocket;
            //    socketArgs.RemoteEndPoint = receiveSocket.LocalEndPoint;
            //    socketArgs.Completed += SocketArgs_Completed;
            //    receiveBufferPool.SetBuffer(socketArgs);
            //    receivePool.Set(socketArgs);
            //}
        }

        /// <summary>
        /// 开始接收数据
        /// </summary>
        public void StartReceive()
        {
            bool rt = receiveSocket.ReceiveFromAsync(socketArgs);
            if (rt == false)
            {
                ProcessReceive(socketArgs);
            }

            //SocketAsyncEventArgs arg = receivePool.Get();
            //Socket s = arg.UserToken as Socket;
            //isClose = false;

            //if (!s.ReceiveFromAsync(arg))
            //{
            //    ProcessReceive(arg);
            //}
        }

        /// <summary>
        /// 停止接收数据
        /// </summary>
        public void StopReceive()
        {
            isClose = true;
            if (receiveSocket != null)
            {
                receiveSocket.Shutdown(SocketShutdown.Both);
                receiveSocket.Close();
            }
        }
        #endregion

        #region private
        private void DisposeSocketPool()
        {
            //if (receivePool != null)
            //{
            //    while (receivePool.Count > 0)
            //    {
            //        var item = receivePool.Get();
            //        if (item != null) item.Dispose();
            //    }
            //}
            //if (receiveBufferPool != null)
            //{
            //    receiveBufferPool.Clear();
            //}
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
        /// <param name="arg"></param>
        private void ProcessReceive(SocketAsyncEventArgs arg)
        {
            // receivePool.Set(args);

            if (arg.BytesTransferred > 0
                && arg.SocketError == SocketError.Success)
            {
                if (OnReceived != null)
                {
                    OnReceived(arg.UserToken as Socket, arg);
                }
            }

            if (isClose) return;

            StartReceive();
        }

        #endregion
    }
}