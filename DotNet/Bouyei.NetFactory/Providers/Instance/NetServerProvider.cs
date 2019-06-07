/*-------------------------------------------------------------
 *   auth: bouyei
 *   date: 2017/7/29 13:43:40
 *contact: 453840293@qq.com
 *profile: www.openthinking.cn
 *   guid: 7b7a759e-571f-4486-969a-5306e9dc0f51
---------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetFactory
{
    using Tcp;
    using Udp;
    using WebSocket;

    public class NetServerProvider : INetServerProvider
    {
        #region variable
        private TcpServerProvider tcpServerProvider = null;
        private UdpServerProvider udpServerProvider = null;
        private WSServerProvider wsServerProvider = null;
        private Encoding encoding = Encoding.UTF8;

        private int chunkBufferSize = 4096;
        private int maxNumberOfConnections = 512;
        private bool _isDisposed = false;
        #endregion

        #region property
        private OnReceivedHandler _receiveHanlder = null;
        public OnReceivedHandler ReceivedHandler
        {
            get { return _receiveHanlder; }
            set
            {
                _receiveHanlder = value;
                if (NetProviderType.Tcp == NetProviderType)
                {
                    tcpServerProvider.ReceivedCallback = _receiveHanlder;
                }
                else if (NetProviderType.Udp == NetProviderType)
                {
                    udpServerProvider.ReceivedCallbackHandler = _receiveHanlder;
                }else if(NetProviderType.WebSocket==NetProviderType)
                {
                    wsServerProvider.OnReceived = _receiveHanlder;
                }
            }
        }

        private OnSentHandler _sentHanlder = null;
        public OnSentHandler SentHandler
        {
            get { return _sentHanlder; }
            set
            {
                _sentHanlder = value;
                if (NetProviderType.Tcp == NetProviderType)
                {
                    tcpServerProvider.SentCallback = _sentHanlder;
                }
                else if (NetProviderType.Udp == NetProviderType)
                {
                    udpServerProvider.SentCallbackHandler = _sentHanlder;
                }
                else if (NetProviderType.WebSocket == NetProviderType)
                {
                    wsServerProvider.OnSent = _sentHanlder;
                }
            }
        }

        private OnAcceptedHandler _acceptHanlder = null;
        public OnAcceptedHandler AcceptedHandler
        {
            get { return _acceptHanlder; }
            set
            {
                _acceptHanlder = value;
                if (NetProviderType.Tcp == NetProviderType)
                {
                    tcpServerProvider.AcceptedCallback = _acceptHanlder;
                }
                else if (NetProviderType.WebSocket == NetProviderType)
                {
                    wsServerProvider.OnAccepted = _acceptHanlder;
                }
            }
        }

        private OnReceivedSegmentHandler _receiveOffsetHandler = null;
        public OnReceivedSegmentHandler ReceivedOffsetHandler
        {
            get { return _receiveOffsetHandler; }
            set
            {
                _receiveOffsetHandler = value;
                if (NetProviderType.Tcp == NetProviderType)
                {
                    tcpServerProvider.ReceivedOffsetCallback = _receiveOffsetHandler;
                }
                else if (NetProviderType.Udp == NetProviderType)
                {
                    udpServerProvider.ReceivedOffsetHanlder = _receiveOffsetHandler;
                }
                else if (NetProviderType.WebSocket == NetProviderType)
                {
                    wsServerProvider.OnReceivedBytes = _receiveOffsetHandler;
                }
            }
        }

        private OnDisconnectedHandler _disconnectedHanlder = null;
        public OnDisconnectedHandler DisconnectedHandler
        {
            get { return _disconnectedHanlder; }
            set
            {
                _disconnectedHanlder = value;
                if (NetProviderType.Tcp == NetProviderType)
                {
                    tcpServerProvider.DisconnectedCallback = _disconnectedHanlder;
                }
                else if (NetProviderType.Udp == NetProviderType)
                {
                    udpServerProvider.DisconnectedCallbackHandler = _disconnectedHanlder;
                }
                else if (NetProviderType.WebSocket == NetProviderType)
                {
                    wsServerProvider.OnDisconnected = _disconnectedHanlder;
                }
            }
        }

        public NetProviderType NetProviderType { get; private set; }

        #endregion

        #region constructor
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
                if (tcpServerProvider != null)
                    tcpServerProvider.Dispose();

                if (udpServerProvider != null)
                    udpServerProvider.Dispose();

                if (wsServerProvider != null)
                    wsServerProvider.Dispose();

                _isDisposed = true;
            }
        }

        public NetServerProvider(
            int chunkBufferSize = 4096,
            int maxNumberOfConnections = 32,
            NetProviderType netProviderType = NetProviderType.Tcp)
        {
            this.NetProviderType = netProviderType;
            this.chunkBufferSize = chunkBufferSize;
            this.maxNumberOfConnections = maxNumberOfConnections;

            if (netProviderType == NetProviderType.Tcp)
            {
                tcpServerProvider = new TcpServerProvider(maxNumberOfConnections, chunkBufferSize);
            }
            else if (netProviderType == NetProviderType.Udp)
            {
                udpServerProvider = new UdpServerProvider(maxNumberOfConnections, chunkBufferSize);
            }
            else if(netProviderType==NetProviderType.WebSocket)
            {
                wsServerProvider = new WSServerProvider(maxNumberOfConnections, chunkBufferSize);
            }
        }

        public NetServerProvider(NetProviderType netProviderType)
        {
            this.NetProviderType = netProviderType;

            if (netProviderType == NetProviderType.Tcp)
            {
                tcpServerProvider = new TcpServerProvider(maxNumberOfConnections, chunkBufferSize);
            }
            else if (netProviderType == NetProviderType.Udp)
            {
                udpServerProvider = new UdpServerProvider(maxNumberOfConnections, chunkBufferSize);
            }
            else if (netProviderType == NetProviderType.WebSocket)
            {
                wsServerProvider = new WSServerProvider(maxNumberOfConnections, chunkBufferSize);
            }
        }

        public static NetServerProvider CreateProvider(
            int chunkBufferSize = 4096,
            int maxNumberOfConnections = 32,
            NetProviderType netProviderType = NetProviderType.Tcp)
        {
            return new NetServerProvider(chunkBufferSize, maxNumberOfConnections, netProviderType);
        }

        #endregion

        #region public method
        public bool Start(int port, string ip = "0.0.0.0")
        {
            if (NetProviderType == NetProviderType.Tcp)
            {
                return tcpServerProvider.Start(port, ip);
            }
            else if (NetProviderType == NetProviderType.Udp)
            {
                udpServerProvider.Start(port);
                return true;
            }
            else if (NetProviderType.WebSocket == NetProviderType)
            {
              return  wsServerProvider.Start(port,ip);
            }
            return false;
        }

        public void Stop()
        {
            if (NetProviderType == NetProviderType.Tcp)
            {
                tcpServerProvider.Stop();
            }
            else if (NetProviderType == NetProviderType.Udp)
            {
                udpServerProvider.Stop();
            }
            else if (NetProviderType.WebSocket == NetProviderType)
            {
                wsServerProvider.Stop();
            }
        }

        public bool Send(SegmentToken segToken, bool waiting = true)
        {
            if (NetProviderType == NetProviderType.Tcp)
            {
                return tcpServerProvider.Send(segToken, waiting);
            }
            else if (NetProviderType == NetProviderType.Udp)
            {
                return udpServerProvider.Send(segToken.Data,segToken.sToken.TokenIpEndPoint, waiting);
            }
            else if (NetProviderType.WebSocket == NetProviderType)
            {
                wsServerProvider.Send(segToken.sToken, encoding.GetString(segToken.Data.buffer,
                    segToken.Data.offset, segToken.Data.size));
            }
            return false;
        }

        public bool Send(SocketToken sToken,string content, bool waiting = true)
        {
            if (NetProviderType == NetProviderType.Tcp)
            {
                return tcpServerProvider.Send(new SegmentToken(sToken,encoding.GetBytes(content)), waiting);
            }
            else if (NetProviderType == NetProviderType.Udp)
            {
                return udpServerProvider.Send(new SegmentOffset(encoding.GetBytes(content)),sToken.TokenIpEndPoint,waiting);
            }
            else if (NetProviderType.WebSocket == NetProviderType)
            {
               return wsServerProvider.Send(sToken,content,waiting);
            }
            return false;
        }

        public int SendSync(SegmentToken segToken)
        {
            if (NetProviderType == NetProviderType.Tcp)
            {
                tcpServerProvider.SendSync(segToken);
            }
            else if (NetProviderType == NetProviderType.Udp)
            {
                return udpServerProvider.SendSync(
                      segToken.sToken.TokenIpEndPoint, segToken.Data);
            }
            else if (NetProviderType.WebSocket == NetProviderType)
            {
                return wsServerProvider.Send(segToken) ? 1 : 0;
            }
            return 0;
        }

        public void CloseToken(SocketToken sToken)
        {
            if (NetProviderType == NetProviderType.Tcp)
            {
                tcpServerProvider.Close(sToken);
            }
            else if (NetProviderType == NetProviderType.Udp)
            {
               
            }
            else if (NetProviderType.WebSocket == NetProviderType)
            {
                 wsServerProvider.Close(sToken);
            }
        }
        #endregion
    }
}
