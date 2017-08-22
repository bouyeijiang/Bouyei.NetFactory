using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Bouyei.NetProviderFactoryDemo
{
    using NetProviderFactory;
    using NetProviderFactory.Protocols;

    class Program
    {
        static void Main(string[] args)
        {
            ProtocolsDemo();

            UdpDemo();
            TcpDemo();
        }

        private static void TcpDemo()
        {
            int port = 13145;
            int svc_send_cnt = 0, svc_rec_cnt = 0, client_send_cnt = 0, client_rec_cnt = 0;
            //服务端
            NetServerProvider serverSocket = NetServerProvider.CreateNetServerProvider();
            byte[] sendbuffer = new byte[4096];
            for (int i = 0; i < sendbuffer.Length; ++i)
            {
                sendbuffer[i] = (byte)(i > 255 ? 255 : i);
            }

            //已经截取接收到的真实数据
            serverSocket.ReceiveOffsetHanlder = new OnReceiveOffsetHandler((sToken, buffer, offset, count) =>
            {
                try
                {
                    // Console.WriteLine("complate:from client[" + offset + "cnt:" + count);
                    serverSocket.Send(sToken, Encoding.UTF8.GetBytes("hi I'm server:" + DateTime.Now));
                    svc_rec_cnt += 1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            });
            serverSocket.SentHanlder = new OnSentHandler((stoken,buff, offset,count) =>
            {
                svc_send_cnt += 1;
            });
            serverSocket.DisconnectedHanlder = new OnDisconnectedHandler((stoken) =>
            {
                Console.WriteLine("disconnect" + stoken.TokenId);
            });

            bool isOk = serverSocket.Start(port);
            if (isOk)
            {
                Console.WriteLine("已启动服务。。。");

                //客户端
                NetClientProvider clientSocket = NetClientProvider.CreateNetClientProvider();
                clientSocket.SentHanlder = new OnSentHandler((stoken, buff,offset,cont) =>
                {
                    client_send_cnt += 1;
                });
                //异步连接
                clientSocket.ReceiveOffsetHanlder = new OnReceiveOffsetHandler((sToken, buffer, offset, count) =>
                {
                    client_rec_cnt += 1;
                    //Console.WriteLine("client:from server[" + Encoding.UTF8.GetString(buffer, offset, count));
                });
                clientSocket.DisconnectedHanlder = new OnDisconnectedHandler((stoken) =>
                {
                    Console.WriteLine("clinetdiscount");
                });
                bool rt = clientSocket.ConnectTo(port, "127.0.0.1");
                if (rt)
                {
                    for (int i = 0; i < 100000; i++)
                    {
                        if (i% 100 == 0)
                        {
                            Console.WriteLine(clientSocket.SendBufferNumber + ":" + i);
                            Console.WriteLine(string.Format("svc[send:{0},rec:{1}],client[send{2},rec:{3}]", svc_send_cnt, svc_rec_cnt, client_send_cnt, client_rec_cnt));
                        }
                        clientSocket.Send(sendbuffer);
                    }
                    Console.WriteLine("complete");
                    clientSocket.Dispose();
                }
            }
            Console.ReadKey();
            serverSocket.Dispose();
        }

        private static void UdpDemo()
        {
            int port = 12345;
            int svc_c = 0, cli_c = 0, cli_c2 = 0;
            INetClientProvider clientProvider = null;
            INetServerProvider serverProvider = NetServerProvider.CreateNetServerProvider(4096, 64, NetProviderType.Udp);
            serverProvider.ReceiveOffsetHanlder = new OnReceiveOffsetHandler((sToken, buffer, offset, count) =>
            {
                ++svc_c;
                Console.WriteLine("from client:" +svc_c+ Encoding.UTF8.GetString(buffer, offset, count));
                serverProvider.Send(sToken, Encoding.UTF8.GetBytes("i'm server" + DateTime.Now));
            });
            if (serverProvider.Start(port))
            {
                byte[] sendbuffer = new byte[4096];
                int sentcnt = 0;
                for (int i = 1; i < sendbuffer.Length; ++i)
                {
                    sendbuffer[i] = (byte)(i > 255 ? 255 : i);
                }
                
                clientProvider = NetClientProvider.CreateNetClientProvider(4096, 4, NetProviderType.Udp);
                clientProvider.SentHanlder = new OnSentHandler((sToken,buff,offset, count) =>
                {
                    ++sentcnt;
                  //  mER.Set();
                });
                clientProvider.ReceiveOffsetHanlder = new OnReceiveOffsetHandler((sToken, buffer, offset, count) =>
                {
                    Console.WriteLine("from server one:" +cli_c+ Encoding.UTF8.GetString(buffer, offset, count));
                    ++cli_c;
                });
                bool isConn = clientProvider.ConnectTo(port, "127.0.0.1");
                int c = 100000;

                INetClientProvider netClient = NetClientProvider.CreateNetClientProvider(4096, 4, NetProviderType.Udp);
                netClient.ReceiveOffsetHanlder = new OnReceiveOffsetHandler((sToken, buffer, offset, count) =>
                  {
                      ++cli_c2;
                      Console.WriteLine("from server two:"+cli_c2 + Encoding.UTF8.GetString(buffer, offset, count));
                  });
                isConn = netClient.ConnectTo(port, "127.0.0.1");

                while (c > 0)
                {
                 //   mER.WaitOne();
                    clientProvider.Send(Encoding.UTF8.GetBytes("one abb"+DateTime.Now));
                   // mER.Reset();
                    netClient.Send(Encoding.UTF8.GetBytes("two abb" + DateTime.Now));
                    //Thread.Sleep(10);
                    --c;
                    if (c % 1000 == 0)
                    {
                        Console.WriteLine(string.Format("svc:{0};cli1:{1};cli2:{2}", svc_c, cli_c, cli_c2));
                    }
                    Console.WriteLine(sentcnt);
                }
            }
            Console.WriteLine(string.Format("完成svc:{0};cli1:{1};cli2:{2}", svc_c, cli_c, cli_c2));
            Console.ReadKey();
            serverProvider.Dispose();
            clientProvider.Dispose();
        }

        private static void ProtocolsDemo()
        {
            INetProtocolProvider protocolProvider = NetProtocolProvider.CreateNetProtocolProvider();

            //数据内容打包成字节
            byte[] content = new byte[] { 1, 3, 4, 0x07, 0x01, 0x07 };
            byte[] buffer= protocolProvider.Encode(new Package()
            {
                pHeader = new PackageHeader()
                {
                    packageAttribute = new PackageAttribute()
                    {
                        packageCount = 1,//自定义,指定该消息需要分多少个数据包发送才完成
                        payloadLength = (UInt32)content.Length//数据载体长度
                    },
                    packageFlag = 0x07,//根据业务自定义
                    packageId = 0x10//根据业务自定义
                },
                pPayload = content//携带的数据内容
            });

            //解析数据包成结构信息
            var dePkg = protocolProvider.Decode(buffer, 0, buffer.Length);
        }
    }
}
