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

        public bool IsConnected
        {
            get
            {
                if (ProviderType.Tcp == NetProviderType)
                    return tcpClientProvider.IsConnected;
                else return false;
            }
        }

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
        public OnDisconnectedHandler DisconnectedHanlder
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

        public static NetClientProvider CreateNetClientProvider(ProviderType netProviderType = ProviderType.Tcp,
             int bufferSizeByConnection = 4096, int maxNumberOfConnections = 8)
        {
            return new NetClientProvider(netProviderType, bufferSizeByConnection, maxNumberOfConnections);
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

        public bool ConnectSync(int port, string ip)
        {
            if (NetProviderType == ProviderType.Tcp)
            {
                return tcpClientProvider.ConnectSync(port, ip);
            }
            else if (NetProviderType == ProviderType.Udp)
            {
                udpClientProvider.Initialize(bufferSizeByConnection, port);
                return true;
            }
            return false;
        }

        public void SendSync(byte[] buffer, Action<int,byte[]> recAct = null,int recBufferSize=4096)
        {
            if (NetProviderType == ProviderType.Tcp)
            {
                tcpClientProvider.SendSync(buffer, recAct,recBufferSize);
            }
            else if (NetProviderType == ProviderType.Udp)
            {
                udpClientProvider.SendSync(buffer, recAct, recBufferSize);
            }
        }

        public void ReceiveSync(Action<int, byte[]> recAct, int recBufferSize = 4096)
        {
            if (NetProviderType == ProviderType.Tcp)
            {
                tcpClientProvider.ReceiveSync( recAct, recBufferSize);
            }
            else if (NetProviderType == ProviderType.Udp)
            {
                udpClientProvider.ReceiveSync(recAct, recBufferSize);
            }
        }
        #endregion
    }
}
