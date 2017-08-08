using System;
using System.Net;
using System.Net.Sockets;

namespace Bouyei.NetProviderFactory.Udp
{
    public class UdpServerProvider:IDisposable
    {
        #region 变量定义
        private SocketReceive socketRecieve;
        private SocketSend socketSend;
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
            socketSend.Initialize(maxConnectionCount,recBufferSize);

            socketRecieve = new SocketReceive(port);
            socketRecieve.Initialize(maxConnectionCount, recBufferSize);
            socketRecieve.OnReceived += new EventHandler<SocketAsyncEventArgs>(receiveSocket_OnReceived);
            socketRecieve.StartReceive();
        }

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
        public void Send(IPEndPoint remoteEP, byte[] data,int offset,int size,bool waitingSignal=true)
        {
            socketSend.Send(data,offset,size, waitingSignal,remoteEP);
        }

        /// <summary>
        ///  服务端发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public void Send(byte[] data,int offset,int size, string ip, int port,bool waitingSignal=true)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port);

            socketSend.Send(data, offset, size, waitingSignal,ep);
        }

        void sendSocket_SentEventHandler(object sender, SocketAsyncEventArgs e)
        {
            if (SentCallbackHandler != null)
            {
                SentCallbackHandler(new SocketToken()
                {
                    TokenSocket = e.ConnectSocket
                }, e.BytesTransferred);
            }
        }

        void receiveSocket_OnReceived(object sender, SocketAsyncEventArgs e)
        {
            SocketToken sToken = new SocketToken()
            {
                TokenSocket = e.ConnectSocket
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
    }
}