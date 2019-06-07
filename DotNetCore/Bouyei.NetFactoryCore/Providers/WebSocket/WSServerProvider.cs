﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Bouyei.NetFactoryCore.WebSocket
{
    using Protocols.WebSocketProto;
    using Bouyei.NetFactoryCore.Tcp;


    public class WSServerProvider : IDisposable
    {
        private Encoding encoding = Encoding.UTF8;
        TcpServerProvider serverProvider = null;
        List<ConnectionInfo> ConnectionPool = null;
        Timer threadingTimer = null;
        int timeout = 1000 * 60 * 6;
        object lockobject = new object();

        public OnReceivedHandler OnReceived { get; set; }
        public OnReceivedSegmentHandler OnReceivedBytes { get; set; }
        public OnAcceptedHandler OnAccepted { get; set; }
        public OnDisconnectedHandler OnDisconnected { get; set; }
        public OnSentHandler OnSent { get; set; }

        public WSServerProvider(int maxConnections = 32, int bufferSize = 4096)
        {
            ConnectionPool = new List<ConnectionInfo>(maxConnections);

            serverProvider = new TcpServerProvider(maxConnections, bufferSize);
            //serverProvider.AcceptedCallback = new OnAcceptHandler(AcceptedHandler);
            serverProvider.DisconnectedCallback = new OnDisconnectedHandler(DisconnectedHandler);
            serverProvider.ReceivedOffsetCallback = new OnReceivedSegmentHandler(ReceivedHandler);
            serverProvider.SentCallback = new OnSentHandler(OnSendHandler);

            threadingTimer = new Timer(new TimerCallback(TimingEvent), null, -1, -1);
        }

        public static WSServerProvider CreateProvider()
        {
            return new WSServerProvider();
        }

        public void Dispose()
        {
            threadingTimer.Dispose();
        }

        private void TimingEvent(object obj)
        {
            lock (lockobject)
            {
                var items = ConnectionPool.FindAll(x => DateTime.Now.Subtract(x.ConnectedTime).TotalMilliseconds >= timeout);

                foreach (var node in items)
                {
                    CloseAndRemove(node);
                }
            }
        }

        public bool Start(int port, string ip = "0.0.0.0")
        {
            bool isOk = serverProvider.Start(port, ip);
            if (isOk)
            {
                threadingTimer.Change(timeout / 2, timeout);
            }
            return isOk;
        }

        public void Stop()
        {
            threadingTimer.Change(-1, -1);
            lock (lockobject)
            {
                foreach (var node in ConnectionPool)
                {
                    CloseAndRemove(node);
                }
            }
        }

        public bool Send(SocketToken sToken, string content)
        {
            byte[] buffer = new ServerPackage().GetBytes(content);

            return serverProvider.Send(new SegmentToken(sToken, buffer));
        }

        public bool Close(SocketToken sToken)
        {
            lock (lockobject)
            {
                bool isOk = Remove(sToken);
                if (isOk)
                {
                    return ConnectionPool.RemoveAll(x => x.sToken.TokenId == sToken.TokenId) > 0;
                }
                return false;
            }
        }

        //private void AcceptedHandler(SocketToken sToken)
        //{

        //}

        private void DisconnectedHandler(SocketToken sToken)
        {
            //ConnectionPool.Remove(new ConnectionInfo() { sToken = sToken });
            Remove(sToken);

            if (OnDisconnected != null) OnDisconnected(sToken);
        }

        private void OnSendHandler(SegmentToken session)
        {
            if (OnSent != null)
            {
                OnSent(session);
            }
        }

        private void ReceivedHandler(SegmentToken session)
        {
            var connection = ConnectionPool.Find(x => x.sToken.TokenId == session.sToken.TokenId);
            if (connection == null)
            {
                connection = new ConnectionInfo() { sToken = session.sToken };

                ConnectionPool.Add(connection);
            }

            if (connection.IsHandShaked == false)
            {
                var serverPackage = new ServerPackage();

                var access = serverPackage.GetHandshakePackage(session.Data);
                connection.IsHandShaked = access.IsHandShaked();

                if (connection.IsHandShaked == false)
                {
                    CloseAndRemove(connection);
                    return;
                }
                connection.ConnectedTime = DateTime.Now;

                string rsp = serverPackage.RspAcceptPackage(access);

                serverProvider.Send(new SegmentToken(session.sToken, encoding.GetBytes(rsp)));

                connection.accessInfo = access;

                if (OnAccepted != null) OnAccepted(session.sToken);
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
                    CloseAndRemove(connection);
                    return;
                }
                else
                {
                    if (OnReceivedBytes != null)
                        OnReceivedBytes(new SegmentToken(session.sToken, packet.Payload));
                }

                RefreshTimeout(session.sToken);
            }
        }

        private void RefreshTimeout(SocketToken sToken)
        {
            foreach (var item in ConnectionPool)
            {
                if (item.sToken.TokenId == sToken.TokenId)
                {
                    item.ConnectedTime = DateTime.Now;
                    break;
                }
            }
        }

        private void CloseAndRemove(ConnectionInfo connection)
        {
            bool isOk = Remove(connection);
            if (isOk)
            {
                serverProvider.Close(connection.sToken);
            }
        }

        private bool Remove(ConnectionInfo info)
        {
            return ConnectionPool.Remove(info);
        }

        private bool Remove(SocketToken sToken)
        {

            return ConnectionPool.RemoveAll(x => x.sToken.TokenId == sToken.TokenId) > 0;
        }
    }


    internal class ConnectionInfo : IComparable<SocketToken>
    {
        public SocketToken sToken { get; set; }

        public bool IsHandShaked { get; set; }

        public AccessInfo accessInfo { get; set; }
        public DateTime ConnectedTime { get; set; } = DateTime.MinValue;
        public int CompareTo(SocketToken info)
        {
            return sToken.TokenId - info.TokenId;
        }
    }
}
