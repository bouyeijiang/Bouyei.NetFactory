using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Bouyei.NetFactoryCore.Udp
{
    using Providers.Udp;

    public class UdpServerProvider :UdpSocket, IDisposable
    {
        #region variable
        private SocketReceive socketReceive = null;
        private SocketSend socketSend = null;
        private bool _isDisposed = false;
        private Encoding encoding = Encoding.UTF8;
        private int bufferSizeByConnection = 4096;
        private int maxNumberOfConnections = 8;

        #endregion

        #region property

        public OnReceivedSegmentHandler ReceiveOffsetHanlder { get; set; }

        /// <summary>
        /// 接收事件响应回调
        /// </summary>
        public OnReceivedHandler ReceiveCallbackHandler { get; set; }

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
                socketReceive.Dispose();
                socketSend.Dispose();
                _isDisposed = true;
            }
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        public UdpServerProvider(int maxNumberOfConnections, int bufferSizeByConnection)
            : base(bufferSizeByConnection)
        {
            this.maxNumberOfConnections = maxNumberOfConnections;
            this.bufferSizeByConnection = bufferSizeByConnection;
        }

        /// <summary>
        /// 启动服务
        /// </summary>
        /// <param name="port">接收数据端口</param>
        public void Start(int port)
        { 
            socketReceive = new SocketReceive(port, maxNumberOfConnections, bufferSizeByConnection);
            socketReceive.OnReceived += receiveSocket_OnReceived;
            socketReceive.StartReceive();

            socketSend = new SocketSend(socketReceive.socket,maxNumberOfConnections, bufferSizeByConnection);
            socketSend.SentEventHandler += sendSocket_SentEventHandler;
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
                socketSend.Dispose();
            }
            if (socketReceive != null)
            {
                socketReceive.StopReceive();
            }
        }
 
        public bool Send(SegmentOffset dataSegment,IPEndPoint remoteEP ,bool waiting = true)
        {
            return socketSend.Send(dataSegment, remoteEP, waiting);
        }
 
        public int SendSync(IPEndPoint remoteEP, SegmentOffset dataSegment)
        {
            return socketSend.SendSync(dataSegment , remoteEP);
        }
        #endregion

        #region private method
        private void sendSocket_SentEventHandler(object sender, SocketAsyncEventArgs e)
        {
            if (SentCallbackHandler != null)
            {
                SentCallbackHandler(new SegmentToken(new SocketToken()
                {
                    TokenIpEndPoint = (IPEndPoint)e.RemoteEndPoint
                }, e.Buffer, e.Offset, e.BytesTransferred));
            }
        }

        private void receiveSocket_OnReceived(object sender, SocketAsyncEventArgs e)
        {
            SocketToken sToken = new SocketToken()
            {
                TokenIpEndPoint = (IPEndPoint)e.RemoteEndPoint
            };

            if (ReceiveOffsetHanlder != null)
                ReceiveOffsetHanlder(new SegmentToken(sToken, e.Buffer, e.Offset, e.BytesTransferred));

            if (ReceiveCallbackHandler != null)
            {
                if (e.BytesTransferred > 0)
                {
                    ReceiveCallbackHandler(sToken, encoding.GetString(e.Buffer, e.Offset, e.BytesTransferred));
                }
            }
        }

        #endregion
    }
}