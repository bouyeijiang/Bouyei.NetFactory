using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetProviderFactoryDemo
{
    using NetProviderFactory;
    class Program
    {
        static void Main(string[] args)
        {
            int port = 13145;
            //服务端
            NetServerProvider serverSocket = NetServerProvider.CreateNetServierProvider();

            //已经截取接收到的真实数据
            serverSocket.ReceiveHanlder = new OnReceiveHandler((sToken, buffer) =>
            {
                Console.WriteLine("complate:from client[" + Encoding.UTF8.GetString(buffer));
                serverSocket.Send(sToken, Encoding.UTF8.GetBytes("hi" + DateTime.Now));
            });

            //直接返回缓冲区和接收到的偏移
            serverSocket.ReceiveOffsetHanlder = new OnReceiveOffsetHandler((sToken, buffer, offset, cnt) =>
            {
                byte[] dst = new byte[cnt];
                Array.Copy(buffer, offset, dst, 0, cnt);
                Console.WriteLine("offset:from client[" + Encoding.UTF8.GetString(dst));
            });

            bool isOk=serverSocket.Start(port);
            if (isOk)
            {
                Console.WriteLine("已启动服务。。。");

                //客户端
                NetClientProvider clientSocket = NetClientProvider.CreateNetClientProvider();
                clientSocket.ReceiveHanlder = new OnReceiveHandler((sToken, buffer) =>
                {
                    Console.WriteLine("client:from server[" + Encoding.UTF8.GetString(buffer));
                });
                clientSocket.Connect(port, "127.0.0.1");
                System.Threading.Thread.Sleep(1000);
                clientSocket.Send(Encoding.UTF8.GetBytes("hello" + DateTime.Now));
            }
            Console.ReadKey();
        }
    }
}
