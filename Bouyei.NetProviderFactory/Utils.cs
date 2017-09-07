using System;
using System.Net.Sockets;

namespace Bouyei.NetProviderFactory
{
    internal class Utils
    {
        internal static void SafeCloseSocket(Socket socket)
        {
            if (socket != null)
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Send);
                    socket.Close();
                }
                catch
                { }
            }
        }
    }
}
