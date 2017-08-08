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
            int svc_send_cnt = 0, svc_rec_cnt = 0, client_send_cnt = 0, client_rec_cnt = 0;
            //服务端
            NetServerProvider serverSocket = NetServerProvider.CreateNetServerProvider();
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
                   // Console.WriteLine("complate:from client[" + offset + "cnt:" + count);
                    serverSocket.Send(sToken, Encoding.UTF8.GetBytes("hi I'm server:" + DateTime.Now));
                    svc_rec_cnt += 1;
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            });
            serverSocket.SentHanlder = new OnSentHandler((stoken,count) => {
                svc_send_cnt += 1;
            });
            serverSocket.DisconnectedHanlder = new OnDisconnectedHandler((stoken) => {
                Console.WriteLine("disconnect"+stoken.TokenId);
            });

            bool isOk=serverSocket.Start(port);
            if (isOk)
            {
                Console.WriteLine("已启动服务。。。");

                //客户端
                NetClientProvider clientSocket = NetClientProvider.CreateNetClientProvider();
                clientSocket.SentHanlder = new OnSentHandler((stoken, cont) =>
                {
                    client_send_cnt += 1;
                });
                //异步连接
                clientSocket.ReceiveOffsetHanlder = new OnReceiveOffsetHandler((sToken, buffer, offset, count) =>
                {
                    client_rec_cnt += 1;
                    //Console.WriteLine("client:from server[" + Encoding.UTF8.GetString(buffer, offset, count));
                });
                clientSocket.DisconnectedHanlder = new OnDisconnectedHandler((stoken) => {
                    Console.WriteLine("clinetdiscount");
                });
                bool rt = clientSocket.ConnectTo(port, "127.0.0.1");
                if (rt)
                {

                    //Parallel.For(0, 1024, (i, state) =>
                    //{
                    //    NetClientProvider client = NetClientProvider.CreateNetClientProvider();
                    //    client.Connect(port, "127.0.0.1");
                    //});
                    int f = 0;
                    for (int i = 0; i < 100000; i++)
                    {
                        ++f;
                        if (f > 0 &&f%100==0)
                        {
                            Console.WriteLine(clientSocket.SendBufferNumber+":"+f);
                            Console.WriteLine(string.Format("svc[send:{0},rec:{1}],client[send{2},rec:{3}]", svc_send_cnt, svc_rec_cnt, client_send_cnt, client_rec_cnt));
                        }
                        clientSocket.Send(sendbuffer);
                    }
                    Console.WriteLine("complete" + f);
                    clientSocket.Dispose();
                }
            }
            Console.ReadKey();
            serverSocket.Dispose();
        }
    }
}
