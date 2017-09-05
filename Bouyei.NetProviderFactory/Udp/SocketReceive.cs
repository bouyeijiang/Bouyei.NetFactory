using System.Net;
using System;
using System.Net.Sockets;

namespace Bouyei.NetProviderFactory.Udp
{
    internal class SocketReceive : IDisposable
    {
        #region variable
        private Socket recSocket = null;
        private SocketAsyncEventArgs recArgs = null;
        private byte[] recBuffer = null;
        private bool isStoped = false;
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
            recSocket = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            recSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);

            recSocket.Bind(localEndPoint);
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
                isStoped = true;
                _isDisposed = true;
                Utils.SafeCloseSocket(recSocket);
                recArgs.Dispose();
            }
        }
        #endregion

        #region public
        public void Initialize(int maxNumberOfConnections, int bufferSize = 4096)
        {
            recArgs = new SocketAsyncEventArgs();

            recArgs.UserToken = recSocket;
            recArgs.RemoteEndPoint = recSocket.LocalEndPoint;
            recArgs.Completed += SocketArgs_Completed;
            recBuffer = new byte[bufferSize];
            recArgs.SetBuffer(recBuffer, 0, bufferSize);
        }

        /// <summary>
        /// 开始接收数据
        /// </summary>
        public void StartReceive()
        {
            bool rt = recSocket.ReceiveFromAsync(recArgs);
            if (rt == false)
            {
                ProcessReceive(recArgs);
            }
        }

        /// <summary>
        /// 停止接收数据
        /// </summary>
        public void StopReceive()
        {
            isStoped = true;
            Utils.SafeCloseSocket(recSocket);
            if (recArgs != null)
            {
                recArgs.Dispose();
            }
        }
        #endregion

        #region private

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

            if (isStoped) return;

            StartReceive();
        }

        #endregion
    }
}