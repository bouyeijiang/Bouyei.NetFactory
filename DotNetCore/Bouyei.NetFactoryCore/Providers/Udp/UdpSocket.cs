/*-------------------------------------------------------------
 *project:Bouyei.NetFactory.Providers.Udp
 *   auth: bouyei
 *   date: 2018/1/27 17:15:02
 *contact: 453840293@qq.com
 *profile: www.openthinking.cn
---------------------------------------------------------------*/
using System;
using System.Net;
using System.Net.Sockets;

namespace Bouyei.NetFactoryCore.Providers.Udp
{
    public class UdpSocket
    {
        internal Socket socket = null;
        internal bool Broadcast = false;
        protected bool isConnected = false;
        protected EndPoint ipEndPoint = null;

        protected byte[] receiveBuffer = null;
        protected int receiveChunkSize = 4096;
        protected int receiveTimeout = 1000 * 60 * 30;
        protected int sendTimeout = 1000 * 60 * 30;
         
        public UdpSocket(int size, bool Broadcast = false)
        {
            this.receiveChunkSize = size;
            this.receiveBuffer = new byte[size];

            this.Broadcast = Broadcast;
        }
        protected void SafeClose()
        {
            if (socket == null) return;

            try
            {
                if (socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    //socket.Disconnect(false);
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch
            { }

            try
            {
                socket.Close();
            }
            catch
            { }
        }

        protected void CreateUdpSocket(int port, IPAddress ip)
        {
            if (Broadcast) ipEndPoint = new IPEndPoint(IPAddress.Broadcast, port);
            else ipEndPoint = new IPEndPoint(ip, port);

            socket = new Socket(ipEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
            {
                ReceiveTimeout = receiveTimeout,
                SendTimeout = sendTimeout
            };
            if (Broadcast)
            {
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            }
            else
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
        }
    }
}
