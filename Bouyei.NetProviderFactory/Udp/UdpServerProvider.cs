using System;
using System.Net;
using System.Net.Sockets;

namespace Bouyei.NetProviderFactory.Udp
{
    public class UdpServerProvider : IDisposable
    {
        #region 变量定义
        private SocketReceive socketRecieve = null;
        private SocketSend socketSend = null;
        private bool _isDisposed = false;

        #endregion

        #region 属性

        public OnReceiveOffsetHandler ReceiveOffsetHanlder { get; set; }

        /// <summary>
        /// 接收事件响应回调
        /// </summary>
        public OnReceiveHandler ReceiveCallbackHandler { get; set; }

        /// <summary>
        /// 发送事件响应回调
        /// </summary>
        public OnSentHandler SentCallbackHandler { get; set; }

        /// <summary>
        /// 断开连接事件回调
        /// </summary>
        public OnDisconnectedHandler DisconnectedCallbackHandler { get; set; }

        #endregion

        #region structure
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
                socketRecieve.Dispose();
                socketSend.Dispose();
                _isDisposed = true;
            }
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        public UdpServerProvider()
        {

        }

        /// <summary>
        /// 启动服务
        /// </summary>
        /// <param name="port">接收数据端口</param>
        /// <param name="recBufferSize">接收缓冲区</param>
        /// <param name="maxConnectionCount">最大客户端连接数</param>
        public void Start(int port,
            int recBufferSize,
            int maxConnectionCount)
        {
            socketSend = new SocketSend();
            socketSend.SentEventHandler += new EventHandler<SocketAsyncEventArgs>(sendSocket_SentEventHandler);
            socketSend.Initialize(maxConnectionCount, recBufferSize);

            socketRecieve = new SocketReceive(port);
            socketRecieve.Initialize(maxConnectionCount, recBufferSize);
            socketRecieve.OnReceived += new EventHandler<SocketAsyncEventArgs>(receiveSocket_OnReceived);
            socketRecieve.StartReceive();
        }
        #endregion

        #region public method
        /// <summary>
        /// 停止服务
        /// </summary>
        public void Stop()
        {
            if (socketSend != null)
            {
                socketSend = null;
            }
            if (socketRecieve != null)
            {
                socketRecieve.StopReceive();
            }
        }

        /// <summary>
        /// 服务端发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="remoteEP"></param>
        public void Send(IPEndPoint remoteEP, byte[] data, int offset, int size, bool waitingSignal = true)
        {
            socketSend.Send(data, offset, size, waitingSignal, remoteEP);
        }

        /// <summary>
        ///  服务端发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public void Send(string ip, int port, byte[] data, int offset, int size, bool waitingSignal = true)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port);

            socketSend.Send(data, offset, size, waitingSignal, ep);
        }

        public int SendSync(byte[] data, int offset, int size, IPEndPoint remoteEP)
        {
            return socketSend.SendSync(data, offset, size, remoteEP);
        }
        #endregion

        #region private method
        private void sendSocket_SentEventHandler(object sender, SocketAsyncEventArgs e)
        {
            if (SentCallbackHandler != null && isServerResponse(e)==false)
            {
                SentCallbackHandler(new SocketToken()
                {
                    TokenIpEndPoint = (IPEndPoint)e.RemoteEndPoint
                }, e.Buffer,e.Offset,e.BytesTransferred);
            }
        }

        private void receiveSocket_OnReceived(object sender, SocketAsyncEventArgs e)
        {
            if (isClientRequest(e)) return;

            SocketToken sToken = new SocketToken()
            {
                TokenIpEndPoint = (IPEndPoint)e.RemoteEndPoint
            };

            if (ReceiveOffsetHanlder != null)
                ReceiveOffsetHanlder(sToken, e.Buffer, e.Offset, e.BytesTransferred);

            if (ReceiveCallbackHandler != null)
            {
                if (e.BytesTransferred > 0)
                {
                    if (e.Offset == 0 && e.BytesTransferred == e.Buffer.Length)
                    {
                        ReceiveCallbackHandler(sToken, e.Buffer);
                    }
                    else
                    {
                        byte[] realBytes = new byte[e.BytesTransferred];
                        Buffer.BlockCopy(e.Buffer, e.Offset, realBytes, 0, e.BytesTransferred);
                        ReceiveCallbackHandler(sToken, realBytes);
                    }
                }
            }
        }

        private bool isClientRequest(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred == 1 && e.Buffer[0] == 0)
            {
                socketSend.Send(new byte[] { 1 }, 0, 1,
                    true,
                    (IPEndPoint)e.RemoteEndPoint);
                return true;
            }
            else return false;
        }

        private bool isServerResponse(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred == 1 && e.Buffer[0] == 1)
            {
                return true;
            }
            else return false;
        }

        #endregion
    }
}