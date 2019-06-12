using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Bouyei.NetFactory.WebSocket
{
    using Protocols.WebSocketProto;
    using Bouyei.NetFactory.Tcp;

    public class WSClientProvider:IDisposable
    {
        TcpClientProvider clientProvider = null;
        private Encoding encoding = Encoding.UTF8;
        ManualResetEvent resetEvent = new ManualResetEvent(false);
        int waitingTimeout = 1000 * 60 * 30;
        public bool IsConnected { get; private set; }
        AcceptInfo acceptInfo = null;

        public OnDisconnectedHandler OnDisconnected{ get; set; }

        public OnConnectedHandler OnConnected { get; set; }

        public OnReceivedHandler OnReceived { get; set; }
        public OnReceivedSegmentHandler OnReceivedBytes { get; set; }
        public OnSentHandler OnSent { get; set; }
        public WSClientProvider(int bufferSize=4096,int blocks=8)
        {
            clientProvider = new TcpClientProvider(bufferSize, blocks);
            clientProvider.DisconnectedCallback = new OnDisconnectedHandler(DisconnectedHandler);
            clientProvider.ReceivedOffsetCallback = new OnReceivedSegmentHandler(ReceivedHanlder);
            clientProvider.SentCallback = new OnSentHandler(SentHandler);
        }

        public void Dispose()
        {
            resetEvent.Dispose();
        }

        public static WSClientProvider CreateProvider(int bufferSize = 4096, int blocks = 8)
        {
            return new WSClientProvider(bufferSize,blocks);
        }

        /// <summary>
        /// wsUrl:ws://ip:port
        /// </summary>
        /// <param name="wsUrl"></param>
        /// <returns></returns>
        public bool Connect(string wsUrl)
        {
            string[] urlParams = wsUrl.Split(':');
            if (wsUrl.Length < 3)
                throw new Exception("wsUrl is error format.for example as ws://localhost:80");

            urlParams[1] = urlParams[1].Replace("//", "");

            Random rand = new Random(DateTime.Now.Millisecond);
            string host = urlParams[1] + ":" + urlParams[2];
 
            bool isOk = clientProvider.ConnectTo(int.Parse(urlParams[2]), urlParams[1]);
            if (isOk == false) throw new Exception("连接失败...");

            string req = new AccessInfo()
            {
                Host = host,
                Origin = "http://" + host,
                SecWebSocketKey = Convert.ToBase64String(encoding.GetBytes(wsUrl + rand.Next(100, 100000).ToString()))
            }.ToString();

            isOk = clientProvider.Send(new SegmentOffset(encoding.GetBytes(req)));

            resetEvent.WaitOne(waitingTimeout);

            return IsConnected;
        }

        public bool Connect(UrlInfo url)
        {
            return Connect(string.Format("{0}://{1}:{2}", url.Proto, url.Domain, url.Port));
        }

        public bool Send(string msg,bool waiting=true)
        {
            if (IsConnected == false) return false;

            var buf = new WebsocketFrame().ToSegmentFrame(msg);
            clientProvider.Send(buf,waiting);
            return true;
        }

        public bool Send(SegmentOffset data,bool waiting=true)
        {
            if (IsConnected == false) return false;

            clientProvider.Send(data,waiting);
            return true;
        }

        public void SendPong(SegmentOffset buf)
        {
            var seg = new WebsocketFrame().ToSegmentFrame(buf,OpCodeType.Bong);
            clientProvider.Send(seg, true);
        }

        public void SendPing()
        {
            var buf = new WebsocketFrame().ToSegmentFrame(new byte[] { },OpCodeType.Bing);
            clientProvider.Send(buf, true);
        }

        private void DisconnectedHandler(SocketToken sToken)
        {
            IsConnected = false;
            if (OnDisconnected != null) OnDisconnected(sToken);
        }

        private void ReceivedHanlder(SegmentToken session)
        {
            if (IsConnected == false)
            {
                string msg = encoding.GetString(session.Data.buffer, session.Data.offset, session.Data.size);
                acceptInfo = new WebsocketFrame().ParseAcceptedFrame(msg);

                if ((IsConnected = acceptInfo.IsHandShaked()))
                {
                    resetEvent.Set();
                    if (OnConnected != null) OnConnected(session.sToken, IsConnected);
                }
                else
                {
                    clientProvider.Disconnect();
                }
            }
            else
            {
                WebsocketFrame packet = new WebsocketFrame();
                bool isOk= packet.DecodingFromBytes(session.Data, true);
                if (isOk == false) return;

                if (packet.OpCode == 0x01)
                {
                    if (OnReceived != null)
                        OnReceived(session.sToken, encoding.GetString(packet.Payload.buffer,
                        packet.Payload.offset, packet.Payload.size));

                    return;
                }
                else if (packet.OpCode == 0x08)//close
                {
                    IsConnected = false;
                    clientProvider.Disconnect();
                }
                else if (packet.OpCode == 0x09)//ping
                {
                    SendPong(session.Data);
                }
                else if (packet.OpCode == 0x0A)//pong
                {
                    SendPing();
                }

                if (OnReceivedBytes != null && packet.Payload.size > 0)
                    OnReceivedBytes(new SegmentToken(session.sToken, packet.Payload));
            }
        }
        private void SentHandler(SegmentToken session)
        {
            if (OnSent!=null){
                OnSent(session);
            }
        }
        //private void ConnectedHandler(SocketToken sToken,bool isConnected)
        //{

        //}
    }
}
