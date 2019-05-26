using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace Bouyei.NetFactory
{
    using Tcp;
    using Udp;
    using WebSocket;

    public class NetClientProvider : INetClientProvider
    {
        #region variable
        private bool _isDisposed = false;
        private int chunkBufferSize = 4096;
        private int sendConcurrentSize = 64;
        private TcpClientProvider tcpClientProvider = null;
        private UdpClientProvider udpClientProvider = null;
        private WSClientProvider wsClientProvider = null;
        #endregion

        #region property

        public bool IsConnected
        {
            get
            {
                if (NetProviderType.Tcp == NetProviderType)
                    return tcpClientProvider.IsConnected;
                else if (NetProviderType.WebSocket == NetProviderType)
                    return wsClientProvider.IsConnected;
                else return false;
            }
        }

        /// <summary>
        /// 发送缓冲区个数
        /// </summary>
        public int BufferPoolCount
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

        private OnReceivedHandler _receiveHanlder = null;
        public OnReceivedHandler ReceiveHandler
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
                else if (NetProviderType.WebSocket == NetProviderType)
                {
                    wsClientProvider.OnReceived = _receiveHanlder;
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
                    tcpClientProvider.SentCallback = _sentHanlder;
                }
                else if (NetProviderType.Udp == NetProviderType)
                {
                    udpClientProvider.SentCallbackHandler = _sentHanlder;
                }
                else if (NetProviderType.WebSocket == NetProviderType)
                {
                    wsClientProvider.OnSent = _sentHanlder;
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
                else if (NetProviderType.WebSocket == NetProviderType)
                {
                    wsClientProvider.OnConnected = _connectedHanlder;
                }
            }
        }

        private OnReceivedSegmentHandler _receiveOffsetHandler = null;
        public OnReceivedSegmentHandler ReceiveOffsetHandler
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
                else if (NetProviderType.WebSocket == NetProviderType)
                {
                    wsClientProvider.OnReceivedBytes = _receiveOffsetHandler;
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
                if (NetProviderType.Tcp == NetProviderType)
                {
                    tcpClientProvider.DisconnectedCallback = _disconnectedHandler;
                }else if (NetProviderType.WebSocket == NetProviderType)
                {
                    wsClientProvider.OnDisconnected = _disconnectedHandler;
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

                if (wsClientProvider != null)
                    wsClientProvider.Dispose(); 

                _isDisposed = true;
            }
        }

        public NetClientProvider(
            int chunkBufferSize = 4096, 
            int sendConcurrentSize = 8,
             NetProviderType netProviderType = NetProviderType.Tcp)
        {
            NetProviderType = netProviderType;
            this.chunkBufferSize = chunkBufferSize;
            this.sendConcurrentSize = sendConcurrentSize;

            if (netProviderType == NetProviderType.Tcp)
            {
                tcpClientProvider = new TcpClientProvider(chunkBufferSize, sendConcurrentSize);
            }
            else if (netProviderType == NetProviderType.Udp)
            {
                udpClientProvider = new UdpClientProvider(chunkBufferSize,sendConcurrentSize);
            }
            else if (netProviderType == NetProviderType.WebSocket)
            {
                wsClientProvider = new WSClientProvider(chunkBufferSize, sendConcurrentSize);
            }
        }

        public NetClientProvider(NetProviderType netProviderType)
        {
            NetProviderType = netProviderType;

            if (netProviderType == NetProviderType.Tcp)
            {
                tcpClientProvider = new TcpClientProvider(chunkBufferSize, sendConcurrentSize);
            }
            else if (netProviderType == NetProviderType.Udp)
            {
                udpClientProvider = new UdpClientProvider(chunkBufferSize,sendConcurrentSize);
            }
            else if (netProviderType == NetProviderType.WebSocket)
            {
                wsClientProvider = new WSClientProvider(chunkBufferSize, sendConcurrentSize);
            }
        }

        public static NetClientProvider CreateProvider(
             int chunkBufferSize = 4096, 
             int sendConcurrentSize = 8,
             NetProviderType netProviderType = NetProviderType.Tcp)
        {
            return new NetClientProvider(chunkBufferSize, sendConcurrentSize, netProviderType);
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
            else if (NetProviderType == NetProviderType.WebSocket)
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
            else if (NetProviderType == NetProviderType.WebSocket)
            {
                wsClientProvider.Connect("ws://" + ip + ":" + port);
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
            else if (NetProviderType == NetProviderType.WebSocket)
            {
                wsClientProvider.Connect("ws://"+ip+":"+port);
            }
            return false;
        }

        public bool Send(SegmentOffset dataSegment, bool waiting = true)
        {
            if (NetProviderType == NetProviderType.Tcp)
            {
              return  tcpClientProvider.Send(dataSegment, waiting);
            }
            else if (NetProviderType == NetProviderType.Udp)
            {
               return udpClientProvider.Send(dataSegment, waiting);
            }
            else if (NetProviderType == NetProviderType.WebSocket)
            {
                wsClientProvider.Send(dataSegment, waiting);
            }
            return false;
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
            else if (NetProviderType == NetProviderType.WebSocket)
            {
                wsClientProvider.Connect("ws://" + ip + ":" + port);
            }
            return false;
        }

        public void SendSync(SegmentOffset sendSegment, SegmentOffset receiveSegment)
        {
            if (NetProviderType == NetProviderType.Tcp)
            {
                tcpClientProvider.SendSync(sendSegment, receiveSegment);
            }
            else if (NetProviderType == NetProviderType.Udp)
            {
                udpClientProvider.SendSync(sendSegment, receiveSegment);
            }
            else if (NetProviderType == NetProviderType.WebSocket)
            {
                throw new Exception("not support");
            }
        }

        public void ReceiveSync(SegmentOffset receiveSegment, Action<SegmentOffset> receiveAction)
        {
            if (NetProviderType == NetProviderType.Tcp)
            {
                tcpClientProvider.ReceiveSync( receiveSegment, receiveAction);
            }
            else if (NetProviderType == NetProviderType.Udp)
            {
                udpClientProvider.ReceiveSync(receiveSegment, receiveAction);
            }
            else if (NetProviderType == NetProviderType.WebSocket)
            {
                throw new Exception("not support");
            }
        }
        #endregion
    }
}
