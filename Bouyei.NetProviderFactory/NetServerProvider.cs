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

namespace Bouyei.NetProviderFactory
{
    using Tcp;
    using Udp;
    public class NetServerProvider : INetServerProvider
    {
        #region variable
        private TcpServerProvider tcpServerProvider = null;
        private UdpServerProvider udpServerProvider = null;
        private int bufferSizeByConnection = 2048;
        private int maxNumberOfConnections = 1024;
        private bool _isDisposed = false;
        #endregion

        #region property
        private OnReceiveHandler _receiveHanlder = null;
        public OnReceiveHandler ReceiveHanlder
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
                    udpServerProvider.ReceiveCallbackHandler = _receiveHanlder;
                }
            }
        }

        private OnSentHandler _sentHanlder = null;
        public OnSentHandler SentHanlder
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
            }
        }

        private OnAcceptHandler _acceptHanlder = null;
        public OnAcceptHandler AcceptHandler
        {
            get { return _acceptHanlder; }
            set
            {
                _acceptHanlder = value;
                if (NetProviderType.Tcp == NetProviderType)
                {
                    tcpServerProvider.AcceptedCallback = _acceptHanlder;
                }
            }
        }

        private OnReceiveOffsetHandler _receiveOffsetHandler = null;
        public OnReceiveOffsetHandler ReceiveOffsetHanlder
        {
            get { return _receiveOffsetHandler; }
            set
            {
                _receiveOffsetHandler = value;
                if (NetProviderType.Tcp == NetProviderType)
                {
                    tcpServerProvider.ReceiveOffsetCallback = _receiveOffsetHandler;
                }
                else if (NetProviderType.Udp == NetProviderType)
                {
                    udpServerProvider.ReceiveOffsetHanlder = _receiveOffsetHandler;
                }
            }
        }

        private OnDisconnectedHandler _disconnectedHanlder = null;
        public OnDisconnectedHandler DisconnectedHanlder
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
                _isDisposed = true;
            }
        }

        public NetServerProvider(
            int bufferSizeByConnection = 4096,
            int maxNumberOfConnections = 64,
            NetProviderType netProviderType = NetProviderType.Tcp)
        {
            this.NetProviderType = netProviderType;
            this.bufferSizeByConnection = bufferSizeByConnection;
            this.maxNumberOfConnections = maxNumberOfConnections;

            if (netProviderType == NetProviderType.Tcp)
            {
                tcpServerProvider = new TcpServerProvider(maxNumberOfConnections, bufferSizeByConnection);
            }
            else if (netProviderType == NetProviderType.Udp)
            {
                udpServerProvider = new UdpServerProvider();
            }
        }

        public NetServerProvider(NetProviderType netProviderType)
        {
            this.NetProviderType = netProviderType;
            this.bufferSizeByConnection = 4096;
            this.maxNumberOfConnections = 64;

            if (netProviderType == NetProviderType.Tcp)
            {
                tcpServerProvider = new TcpServerProvider(maxNumberOfConnections, bufferSizeByConnection);
            }
            else if (netProviderType == NetProviderType.Udp)
            {
                udpServerProvider = new UdpServerProvider();
            }
        }

        public static NetServerProvider CreateNetServerProvider(
            int bufferSizeByConnection = 4096,
            int maxNumberOfConnections = 64,
            NetProviderType netProviderType = NetProviderType.Tcp)
        {
            return new NetServerProvider(bufferSizeByConnection, maxNumberOfConnections, netProviderType);
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
                udpServerProvider.Start(port, bufferSizeByConnection, maxNumberOfConnections);
                return true;
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
        }

        public void Send(SocketToken sToken, byte[] buffer,bool waitingSignal=true)
        {
            if (NetProviderType == NetProviderType.Tcp)
            {
                tcpServerProvider.Send(sToken, buffer, 0, buffer.Length,waitingSignal);
            }
            else if (NetProviderType == NetProviderType.Udp)
            {
                udpServerProvider.Send(
                    (System.Net.IPEndPoint)sToken.TokenSocket.RemoteEndPoint,
                    buffer, 0, buffer.Length,waitingSignal);
            }
        }

        public void Send(SocketToken sToken, byte[] buffer, int offset, int size, bool waitingSignal = true)
        {
            if (NetProviderType == NetProviderType.Tcp)
            {
                tcpServerProvider.Send(sToken, buffer, offset, size,waitingSignal);
            }
            else if (NetProviderType == NetProviderType.Udp)
            {
                udpServerProvider.Send(
                    (System.Net.IPEndPoint)sToken.TokenSocket.RemoteEndPoint,
                    buffer, offset, size, waitingSignal);
            }
        }

        public int SendSync(SocketToken sToken, byte[] buffer)
        {
            if (NetProviderType == NetProviderType.Tcp)
            {
                tcpServerProvider.SendSync(sToken, buffer, 0, buffer.Length);
            }
            else if (NetProviderType == NetProviderType.Udp)
            {
             return udpServerProvider.SendSync(
                    buffer, 0, buffer.Length,
                    (System.Net.IPEndPoint)sToken.TokenSocket.RemoteEndPoint);
            }
            return 0;
        }
        public int SendSync(SocketToken sToken, byte[] buffer, int offset, int size)
        {
            if (NetProviderType == NetProviderType.Tcp)
            {
                tcpServerProvider.SendSync(sToken, buffer, offset, size);
            }
            else if (NetProviderType == NetProviderType.Udp)
            {
                return udpServerProvider.SendSync(
                       buffer, offset, size,
                       (System.Net.IPEndPoint)sToken.TokenSocket.RemoteEndPoint);
            }
            return 0;
        }

        #endregion
    }
}
