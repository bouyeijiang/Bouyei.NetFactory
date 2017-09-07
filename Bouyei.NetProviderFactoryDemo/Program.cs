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
    using NetProviderFactory.Pools;

    class Program
    {
        static void Main(string[] args)
        {
            ConnectionPoolTest();
            //ProtocolsDemo();
            //UdpDemo();
            //TcpDemo();
            //ConnectionPoolManagerDemo();
        }

        private static void TcpDemo()
        {
            int port = 13145;
            int svc_send_cnt = 0, svc_rec_cnt = 0, client_send_cnt = 0, client_rec_cnt = 0;
            //服务端
            INetServerProvider serverSocket = NetServerProvider.CreateProvider();
            SocketToken s = null;
            byte[] sendbuffer = new byte[16];
            for (int i = 0; i < sendbuffer.Length; ++i)
            {
                sendbuffer[i] = (byte)(i > 255 ? 255 : i);
            }

            //将数据内容打包成指定格式发送
            INetProtocolProvider protocolProvider = NetProtocolProvider.CreateProvider();
           
            //已经截取接收到的真实数据
            serverSocket.ReceiveOffsetHanlder = new OnReceiveOffsetHandler((sToken, buff, offset, count) =>
            {
                try
                {
                    byte[] buffer = protocolProvider.Encode(new Packet()
                    {
                        pHeader = new PacketHeader()
                        {
                            packetAttribute = new PacketAttribute()
                            {
                                packetCount = 1,//自定义,指定该消息需要分多少个数据包发送才完成
                            },
                            packetId = 0x10//根据业务自定义，标志该数据包的类型
                        },
                        pPayload = sendbuffer//携带的数据内容
                    });

                    serverSocket.Send(sToken, buffer);
                    svc_rec_cnt += 1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            });
            serverSocket.AcceptHandler = new OnAcceptHandler((sToken) =>
            {
                s = sToken;
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
                INetClientProvider clientSocket = NetClientProvider.CreateProvider();
                clientSocket.SentHanlder = new OnSentHandler((stoken, buff,offset,cont) =>
                {
                    client_send_cnt += 1;
                });
                INetPacketProvider pkgProvider = NetPacketProvider.CreateProvider(4096);
                int exactPkgCnt = 0,failePkgCnt=0;
                //异步连接
                clientSocket.ReceiveOffsetHanlder = new OnReceiveOffsetHandler((sToken, buff, offset, count) =>
                {
                    try
                    {
                       bool isIn= pkgProvider.SetBlocks(buff, offset, count);
                        if (isIn == false)
                        {

                        }
                        if (pkgProvider.Count > 0)
                        {
                            var pkgs = pkgProvider.GetBlocks();
                            if (pkgs.Count > 0)
                            {
                                exactPkgCnt += pkgs.Count;
                            }
                            else
                            {
                                ++failePkgCnt;
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                });
                clientSocket.DisconnectedHanlder = new OnDisconnectedHandler((stoken) =>
                {
                    Console.WriteLine("clinet discount");
                });
                bool rt = clientSocket.ConnectTo(port, "127.0.0.1");
                if (rt)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        if (i % 10 == 0)
                        {
                            Console.WriteLine(clientSocket.SendBufferNumber + ":" + i);
                            Console.WriteLine(string.Format("svc[send:{0},rec:{1}],client[send{2},rec:{3}]", svc_send_cnt, svc_rec_cnt, client_send_cnt, client_rec_cnt));
                        }
                        clientSocket.Send(sendbuffer,false);
                    }
                   // serverSocket.DisconnectToken(s); 
                    clientSocket.Disconnect();

                    Console.WriteLine("complete");
                    Console.ReadKey();
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
            INetServerProvider serverProvider = NetServerProvider.CreateProvider(4096, 64, NetProviderType.Udp);
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
                
                clientProvider = NetClientProvider.CreateProvider(4096, 4, NetProviderType.Udp);
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

                INetClientProvider netClient = NetClientProvider.CreateProvider(4096, 4, NetProviderType.Udp);
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
            INetProtocolProvider protocolProvider = NetProtocolProvider.CreateProvider();

            //数据内容打包成字节
            byte[] content = new byte[] { 1, 3, 4, 0xfe, 0x01, 0xfd,0x02 };
            byte[] buffer= protocolProvider.Encode(new Packet()
            {
                pHeader = new PacketHeader()
                {
                    packetAttribute = new PacketAttribute()
                    {
                        packetCount = 1,//自定义,指定该消息需要分多少个数据包发送才完成
                    },
                    packetId = 0x10//根据业务自定义
                },
                pPayload = content//携带的数据内容
            });

            //使用接收管理缓冲池解析数据包
            INetPacketProvider pkgProvider = NetPacketProvider.CreateProvider(1024);
            bool rt= pkgProvider.SetBlocks(buffer, 0, buffer.Length);
            rt = pkgProvider.SetBlocks(buffer, 0, buffer.Length);
            var dePkg= pkgProvider.GetBlocks();

            //解析数据包成结构信息
           // var dePkg = protocolProvider.Decode(buffer, 0, buffer.Length);
        }

        private static void ConnectionPoolManagerDemo()
        {
            int port = 13145;

            INetServerProvider netServerProvider = NetServerProvider.CreateProvider();
            INetTokenPoolProvider tokenPool = NetTokenPoolProvider.CreateProvider(60);
            tokenPool.ConnectionTimeout = 60;
            SocketToken _sToken = null;

            netServerProvider.AcceptHandler = new OnAcceptHandler((sToken) => {
                _sToken = sToken;
                tokenPool.AddToken(new NetConnectionToken()
                {
                    Token = sToken
                });
            });

            bool isOk = netServerProvider.Start(port);
            if (isOk)
            {
                INetClientProvider netClientProvider = NetClientProvider.CreateProvider();
                netClientProvider.DisconnectedHanlder = new OnDisconnectedHandler((sToken) =>
                {
                    Console.WriteLine("client disconnected");
                });
                bool rt = netClientProvider.ConnectTo(port, "127.0.0.1");
                if (rt)
                {
                    while (tokenPool.Count == 0)
                    {
                        Thread.Sleep(10);
                    }
                    var rtToken = tokenPool.GetTokenBySocketToken(_sToken);
                    bool refreshRt = tokenPool.RefreshExpireToken(_sToken);
                    rt = tokenPool.RemoveToken(new NetConnectionToken() { Token = _sToken });
                    Console.WriteLine("pool count:"+tokenPool.Count);
                    Console.ReadKey();
                }
            }

        }

        private static void ConnectionPoolTest()
        {
            INetServerProvider serverProvider = NetServerProvider.CreateProvider(4096, 2);
            INetTokenPoolProvider poolProvider = NetTokenPoolProvider.CreateProvider(60);
            poolProvider.TimerEnable(false);

            int port = 12345;

            serverProvider.DisconnectedHanlder = new OnDisconnectedHandler((s) =>
            {
                Console.WriteLine(s.TokenIpEndPoint + "server disconnected");
            });
            serverProvider.AcceptHandler = new OnAcceptHandler((s) => {
                poolProvider.AddToken(new NetConnectionToken(s));
            });
            bool isStart = serverProvider.Start(port);
            if (isStart)
            {
                again:
                for (int i = 0; i < 3; ++i)
                {
                    INetClientProvider clientProvider = NetClientProvider.CreateProvider();
                    clientProvider.DisconnectedHanlder = new OnDisconnectedHandler((s) =>
                    {
                        Console.WriteLine(s.TokenIpEndPoint + " client disconnected");
                    });
                    bool isConnected = clientProvider.ConnectTo(port, "127.0.0.1");

                    Console.WriteLine(isConnected);
                }
                string info = Console.ReadLine();
                if (info == "again")
                {
                    poolProvider.Clear();
                    goto again;
                }
                else if (info == "stop")
                {
                    serverProvider.Stop();
                    goto again;
                }
                Console.ReadKey();
            }
        }
    }
}
