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
                    if (socket.Connected)
                        socket.Shutdown(SocketShutdown.Send);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch
                { }
                try
                {
                    socket.Dispose();
                }
                catch
                { }
            }
        }
    }
}
