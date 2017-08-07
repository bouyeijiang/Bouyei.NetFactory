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
            NetServerProvider serverSocket = NetServerProvider.CreateNetServerProvider(4096,128);
            byte[] sendbuffer = new byte[4095];
            for (int i = 0; i < sendbuffer.Length; ++i)
            {
                sendbuffer[i] = (byte)(i > 255 ? 255 : i);
            }

            //已经截取接收到的真实数据
            serverSocket.ReceiveOffsetHanlder = new OnReceiveOffsetHandler((sToken, buffer,offset,count) =>
            {
                try
                {
                    Console.WriteLine("complate:from client[" + offset + "cnt:" + count);
                    serverSocket.Send(sToken, Encoding.UTF8.GetBytes("hi I'm server:" + DateTime.Now));
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            });

            bool isOk=serverSocket.Start(port);
            if (isOk)
            {
                Console.WriteLine("已启动服务。。。");

                //客户端
                NetClientProvider clientSocket = NetClientProvider.CreateNetClientProvider(4096,128);
                //if (clientSocket.ConnectSync(port, "127.0.0.1"))
                //{
                //    clientSocket.SendSync(Encoding.UTF8.GetBytes("I'm client" + DateTime.Now), (buffer) =>
                //    {
                //        Console.WriteLine("client:from server[" + Encoding.UTF8.GetString(buffer));
                //    });
                //}

                //异步连接
                clientSocket.ReceiveOffsetHanlder = new OnReceiveOffsetHandler((sToken, buffer, offset, count) =>
                {
                    try
                    {
                        Console.WriteLine("client:from server[" + Encoding.UTF8.GetString(buffer, offset, count));
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                });
                clientSocket.Connect(port, "127.0.0.1");
                System.Threading.Thread.Sleep(1000);

                Parallel.For(0, 100000, (i) => {
                    if (clientSocket.SendBufferNumber > 0)
                        clientSocket.Send(sendbuffer);

                    if (clientSocket.SendBufferNumber <= 1)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                });
            }
            Console.WriteLine("complete");
            Console.ReadKey();
        }
    }
}
