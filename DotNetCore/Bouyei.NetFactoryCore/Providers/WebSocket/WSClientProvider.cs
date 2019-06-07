using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Bouyei.NetFactoryCore.WebSocket
{
    using Protocols.WebSocketProto;
    using Bouyei.NetFactoryCore.Tcp;

    public class WSClientProvider : IDisposable
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
            clientProvider.ReceivedOffsetCallback = new OnReceivedSegmentHandler(OnReceivedEventHandler);
            clientProvider.SentCallback = new OnSentHandler(SentHandler);
        }

        public void Dispose()
        {
            if (clientProvider != null)
            {
                clientProvider.Dispose();
            }
        }

        public static WSClientProvider CreateProvider(int bufferSize=4096,int blocks=8)
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
                throw new Exception("wsUrl is error format,example ws://localhost:80");

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

        public void Disconnect()
        {
            clientProvider.Disconnect();
        }

        public bool Send(string msg,bool waiting=true)
        {
            if (IsConnected == false) return false;

            var buf = new ClientPackage().GetBytes(msg);
            clientProvider.Send(new SegmentOffset(buf));
            return true;
        }

        public bool Send(SegmentOffset data,bool waiting=true)
        {
            if (IsConnected == false) return false;

            var buf = new ClientPackage().EncodingToBytes();
            clientProvider.Send(new SegmentOffset(buf), waiting);
            return true;
        }

        private void DisconnectedHandler(SocketToken sToken)
        {
            IsConnected = false;
            if (OnDisconnected != null) OnDisconnected(sToken);
        }

        private void SentHandler(SegmentToken session)
        {
            if (OnSent != null){
                OnSent(session);
            }
        }

        //private void ConnectedHandler(SocketToken sToken,bool isConnected)
        //{

        //}

        private void OnReceivedEventHandler(SegmentToken session)
        {
            if (IsConnected == false)
            {
                string msg = encoding.GetString(session.Data.buffer, session.Data.offset, session.Data.size);
                acceptInfo = new ClientPackage().GetAcceptPackage(msg);

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
                ClientPackage packet = new ClientPackage();
                packet.DecodingFromBytes(session.Data, true);
                if (packet.OpCode == 0x01)
                {
                    if (OnReceived != null)
                        OnReceived(session.sToken, encoding.GetString(packet.Payload.buffer,
                        packet.Payload.offset, packet.Payload.size));
                }
                else if (packet.OpCode == 0x08)
                {
                    IsConnected = false;
                    clientProvider.Disconnect();
                }
                else
                {
                    if (OnReceivedBytes != null)
                        OnReceivedBytes(new SegmentToken(session.sToken, packet.Payload));
                }
            }
        }
    }
}
