using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetProviderFactoryClientDemo
{
    using NetProviderFactory;

    class Program
    {
        static void Main(string[] args)
        {
            int port = 13145;

            NetClientProvider clientSocket = NetClientProvider.CreateNetClientProvider();

            //异步连接
            clientSocket.Connect(port, "127.0.0.1");
            clientSocket.ConnectedHandler = new OnConnectedHandler((sToken,isConnected) => {
                clientSocket.Send(Encoding.UTF8.GetBytes("client:hello" + DateTime.Now));
            });
            clientSocket.ReceiveOffsetHanlder = new OnReceiveOffsetHandler((sToken, buffer, offset, cnt) =>
            {
                Console.WriteLine("client:from server[" + Encoding.UTF8.GetString(buffer, offset, cnt));
            });

            //同步连接
            //if (clientSocket.ConnectSync(port, "127.0.0.1"))
            //{
            //    clientSocket.SendSync(Encoding.UTF8.GetBytes("I'm client" + DateTime.Now), (recCnt,buffer) =>
            //    {
            //        Console.WriteLine("client:from server[" + Encoding.UTF8.GetString(buffer, 0, recCnt));
            //    });
            //}
            Console.ReadKey();
        }
    }
}
