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

    public class NetClientProvider : INetClientProvider
    {
        #region variable
        private bool _isDisposed = false;
        private int bufferSizeByConnection = 4096;
        private int maxNumberOfConnections = 64;
        private TcpClientProvider tcpClientProvider = null;
        private UdpClientProvider udpClientProvider = null;
        #endregion

        #region property

        public bool IsConnected
        {
            get
            {
                if (NetProviderType.Tcp == NetProviderType)
                    return tcpClientProvider.IsConnected;
                else return false;
            }
        }

        /// <summary>
        /// 发送缓冲区个数
        /// </summary>
        public int SendBufferNumber
        {
            get
            {
                if (NetProviderType.Tcp == NetProviderType)
                    return tcpClientProvider.SendBufferPoolNumber;
                else if (NetProviderType.Udp == NetProviderType)
                    return udpClientProvider.SendBufferPoolNumber;
                else return 0;
            }
        }

        private OnReceiveHandler _receiveHanlder = null;
        public OnReceiveHandler ReceiveHanlder
        {
            get { return _receiveHanlder; }
            set
            {
                _receiveHanlder = value;
                if (NetProviderType.Tcp == NetProviderType)
                {
                    tcpClientProvider.RecievedCallback = _receiveHanlder;
                }
                else if (NetProviderType.Udp == NetProviderType)
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
                if (NetProviderType.Tcp == NetProviderType)
                {
                    tcpClientProvider.SentCallback = _sentHanlder;
                }
                else if (NetProviderType.Udp == NetProviderType)
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
                if (NetProviderType.Tcp == NetProviderType)
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
                if (NetProviderType.Tcp == NetProviderType)
                {
                    tcpClientProvider.ReceiveOffsetCallback = _receiveOffsetHandler;
                }
                else if (NetProviderType.Udp == NetProviderType)
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
                if (NetProviderType.Tcp == NetProviderType)
                {
                    tcpClientProvider.DisconnectedCallback = _disconnectedHandler;
                }
            }
        }

        public NetProviderType NetProviderType { get; private set; }

        public ChannelProviderType ChannelProviderType { get {
            if (NetProviderType.Tcp == NetProviderType)
            {
                return tcpClientProvider.ChannelProviderState;
            }
            else  
            {
                return ChannelProviderType.Async;
            }
        } }

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

        public NetClientProvider(
            int bufferSizeByConnection = 4096, 
            int maxNumberOfConnections = 8,
             NetProviderType netProviderType = NetProviderType.Tcp)
        {
            NetProviderType = netProviderType;
            this.bufferSizeByConnection = bufferSizeByConnection;
            this.maxNumberOfConnections = maxNumberOfConnections;

            if (netProviderType == NetProviderType.Tcp)
            {
                tcpClientProvider = new TcpClientProvider(bufferSizeByConnection, maxNumberOfConnections);
            }
            else if (netProviderType == NetProviderType.Udp)
            {
                udpClientProvider = new UdpClientProvider(bufferSizeByConnection,maxNumberOfConnections);
            }
        }

        public NetClientProvider(NetProviderType netProviderType)
        {
            NetProviderType = netProviderType;
            this.bufferSizeByConnection = 4096;
            this.maxNumberOfConnections = 8;

            if (netProviderType == NetProviderType.Tcp)
            {
                tcpClientProvider = new TcpClientProvider(bufferSizeByConnection, maxNumberOfConnections);
            }
            else if (netProviderType == NetProviderType.Udp)
            {
                udpClientProvider = new UdpClientProvider(bufferSizeByConnection,maxNumberOfConnections);
            }
        }

        public static NetClientProvider CreateProvider(
             int bufferSizeByConnection = 4096, 
             int maxNumberOfConnections = 8,
             NetProviderType netProviderType = NetProviderType.Tcp)
        {
            return new NetClientProvider(bufferSizeByConnection, maxNumberOfConnections, netProviderType);
        }

        #endregion

        #region public method
        public void Disconnect()
        {
            if (NetProviderType == NetProviderType.Tcp)
            {
                tcpClientProvider.Disconnect();
            }
            else if (NetProviderType == NetProviderType.Udp)
            {
                udpClientProvider.Disconnect();
            }
        }

        public void Connect(int port, string ip)
        {
            if (NetProviderType == NetProviderType.Tcp)
            {
                tcpClientProvider.Connect(port, ip);
            }
            else if (NetProviderType == NetProviderType.Udp)
            {
                udpClientProvider.Connect(port, ip);
            }
        }

        public bool ConnectTo(int port, string ip)
        {
            if (NetProviderType == NetProviderType.Tcp)
            {
               return tcpClientProvider.ConnectTo(port, ip);
            }
            else if (NetProviderType == NetProviderType.Udp)
            {
               return udpClientProvider.Connect(port,ip);
            }
            return false;
        }

        public void Send(byte[] buffer,bool waitingSignal=true)
        {
            if (NetProviderType == NetProviderType.Tcp)
            {
                tcpClientProvider.Send(buffer, waitingSignal);
            }
            else if (NetProviderType == NetProviderType.Udp)
            {
                udpClientProvider.Send(buffer,0,buffer.Length,waitingSignal);
            }
        }

        public void Send(byte[] buffer, int offset, int size, bool waitingSignal = true)
        {
            if (NetProviderType == NetProviderType.Tcp)
            {
                tcpClientProvider.Send(buffer, offset, size, waitingSignal);
            }
            else if (NetProviderType == NetProviderType.Udp)
            {
                udpClientProvider.Send(buffer, offset, size, waitingSignal);
            }
        }

        public bool ConnectSync(int port, string ip)
        {
            if (NetProviderType == NetProviderType.Tcp)
            {
                return tcpClientProvider.ConnectSync(port, ip);
            }
            else if (NetProviderType == NetProviderType.Udp)
            {
                udpClientProvider.Connect(port,ip);
                return true;
            }
            return false;
        }

        public void SendSync(byte[] buffer, Action<int,byte[]> recAct = null,int recBufferSize=4096)
        {
            if (NetProviderType == NetProviderType.Tcp)
            {
                tcpClientProvider.SendSync(buffer, recAct,recBufferSize);
            }
            else if (NetProviderType == NetProviderType.Udp)
            {
                udpClientProvider.SendSync(buffer, recAct, recBufferSize);
            }
        }

        public void ReceiveSync(Action<int, byte[]> recAct, int recBufferSize = 4096)
        {
            if (NetProviderType == NetProviderType.Tcp)
            {
                tcpClientProvider.ReceiveSync( recAct, recBufferSize);
            }
            else if (NetProviderType == NetProviderType.Udp)
            {
                udpClientProvider.ReceiveSync(recAct, recBufferSize);
            }
        }
        #endregion
    }
}
