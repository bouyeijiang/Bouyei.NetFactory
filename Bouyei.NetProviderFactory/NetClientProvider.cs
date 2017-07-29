using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace Bouyei.NetProviderFactory
{
    using Tcp;
    using Udp;

    public class NetClientProvider : INetClientProvider, IDisposable
    {
        #region variable
        private bool _isDisposed = false;
        private int bufferSizeByConnection = 2048;
        private int maxNumberOfConnections = 1024;
        private TcpClientProvider tcpClientProvider = null;
        private UdpClientProvider udpClientProvider = null;
        #endregion

        #region property
        private OnReceiveHandler _receiveHanlder = null;
        public OnReceiveHandler ReceiveHanlder
        {
            get { return _receiveHanlder; }
            set
            {
                _receiveHanlder = value;
                if (ProviderType.Tcp == NetProviderType)
                {
                    tcpClientProvider.RecievedCallback = _receiveHanlder;
                }
                else if (ProviderType.Udp == NetProviderType)
                {
                    udpClientProvider.ReceiveCallbackHandler = _receiveHanlder;
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
                if (ProviderType.Tcp == NetProviderType)
                {
                    tcpClientProvider.SentCallback = _sentHanlder;
                }
                else if (ProviderType.Udp == NetProviderType)
                {
                    udpClientProvider.SentCallbackHandler = _sentHanlder;
                }
            }
        }

        private OnConnectedHandler _connectedHanlder = null;
        public OnConnectedHandler ConnectedHandler
        {
            get { return _connectedHanlder; }
            set
            {
                _connectedHanlder = value;
                if (ProviderType.Tcp == NetProviderType)
                {
                    tcpClientProvider.ConnectedCallback = _connectedHanlder;
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
                if (ProviderType.Tcp == NetProviderType)
                {
                    tcpClientProvider.ReceiveOffsetCallback = _receiveOffsetHandler;
                }
                else if (ProviderType.Udp == NetProviderType)
                {
                    udpClientProvider.ReceiveOffsetHandler = _receiveOffsetHandler;
                }
            }
        }

        private OnDisconnectedHandler _disconnectedHandler = null;
        public OnDisconnectedHandler DisconnectedHandler
        {
            get { return _disconnectedHandler; }
            set
            {
                _disconnectedHandler = value;
                if (ProviderType.Tcp == NetProviderType)
                {
                    tcpClientProvider.DisconnectedCallback = _disconnectedHandler;
                }
            }
        }

        public ProviderType NetProviderType { get; private set; }

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
                if (tcpClientProvider != null)
                    tcpClientProvider.Dispose();

                if (udpClientProvider != null)
                    udpClientProvider.Dispose();

                _isDisposed = true;
            }
        }

        public NetClientProvider(ProviderType netProviderType = ProviderType.Tcp,
             int bufferSizeByConnection = 4096, int maxNumberOfConnections = 8)
        {
            NetProviderType = netProviderType;
            this.bufferSizeByConnection = bufferSizeByConnection;
            this.maxNumberOfConnections = maxNumberOfConnections;

            if (netProviderType == ProviderType.Tcp)
            {
                tcpClientProvider = new TcpClientProvider(bufferSizeByConnection, maxNumberOfConnections);
            }
            else if (netProviderType == ProviderType.Udp)
            {
                udpClientProvider = new UdpClientProvider();
            }
        }

        #endregion

        #region public method
        public void Disconnect()
        {
            if (NetProviderType == ProviderType.Tcp)
            {
                tcpClientProvider.Disconnect();
            }
        }

        public void Connect(int port, string ip)
        {
            if (NetProviderType == ProviderType.Tcp)
            {
                tcpClientProvider.Connect(port, ip);
            }
            else if (NetProviderType == ProviderType.Udp)
            {
                udpClientProvider.Initialize(bufferSizeByConnection, port);
            }
        }

        public void Send(byte[] buffer, IPEndPoint udpEp)
        {
            if (NetProviderType == ProviderType.Tcp)
            {
                tcpClientProvider.Send(buffer);
            }
            else if (NetProviderType == ProviderType.Udp)
            {
                udpClientProvider.Send(buffer, udpEp);
            }
        }

        public void Send(byte[] buffer)
        {
            if (NetProviderType == ProviderType.Tcp)
            {
                tcpClientProvider.Send(buffer);
            }
            else if (NetProviderType == ProviderType.Udp)
            {
                udpClientProvider.Send(buffer);
            }
        }

        public void Send(byte[] buffer, Action<byte[]> recAct = null)
        {
            if (NetProviderType == ProviderType.Tcp)
            {
                tcpClientProvider.Send(buffer, recAct);
            }
            else if (NetProviderType == ProviderType.Udp)
            {
                udpClientProvider.Send(buffer, recAct);
            }
        }
        #endregion
    }
}
