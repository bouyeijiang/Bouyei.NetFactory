using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Bouyei.NetFactoryDemo
{
    using NetFactory;
    using NetFactory.Protocols.PacketProto;
    using NetFactory.Pools;
    using NetFactory.WebSocket;

    class Program
    {
        static void Main(string[] args)
        {
            //ConnectDemo();
            //ConnectionPoolTest();
            //ProtocolsDemo();
            UdpDemo(); 
            //TcpDemo();
            //ConnectionPoolManagerDemo();
            //PacketSocketDemo();
            //WebSocketDemo();
        }

        private static void TcpDemo()
        {
            int port = 12145;
 
            //服务端
            INetServerProvider serverSocket = NetServerProvider.CreateProvider(maxNumberOfConnections:2);

            serverSocket.ReceivedOffsetHandler = new OnReceivedSegmentHandler((SegmentToken session) =>
            {
                try
                {
                    //feedback message to client
                    serverSocket.Send(new SegmentToken(session.sToken,Encoding.Default.GetBytes("welcome"+DateTime.Now)));

                    Console.WriteLine("from client" + Encoding.Default.GetString(session.Data.buffer,
                        session.Data.offset, session.Data.size) +Environment.NewLine);

                    //string info = Encoding.UTF8.GetString(buff, offset, count);
                   // Console.WriteLine(count);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            });
            serverSocket.AcceptedHandler = new OnAcceptedHandler((sToken) =>
            {
                Console.WriteLine("accept" + sToken.TokenIpEndPoint + "\n");
            });

            serverSocket.DisconnectedHandler = new OnDisconnectedHandler((stoken) =>
            {
                Console.WriteLine(" server show disconnect" + stoken.TokenId);
            });

            bool isOk = serverSocket.Start(port);
            if (isOk)
            {
                Console.WriteLine("已启动服务。。。");

                //客户端
                INetClientProvider clientSocket = NetClientProvider.CreateProvider();

                //同步发送接收
                //isOk = clientSocket.ConnectTo(port, "127.0.0.1");
                //if (isOk)
                //{
                    //SegmentOffset receive = new SegmentOffset(new byte[4096]);
                    //clientSocket.SendSync(new SegmentOffset(Encoding.Default.GetBytes("hello")), receive);
                    //Console.WriteLine(Encoding.Default.GetString(receive.buffer));

                    //同步接收数据
                    //clientSocket.SendSync(new SegmentOffset(Encoding.Default.GetBytes("hello")), null);

                    //var t = Task.Run(() =>
                    //{
                    //    clientSocket.ReceiveSync(receive, (data) =>
                    //    {
                    //        Console.WriteLine(Encoding.Default.GetString(receive.buffer));
                    //    });
                    //});

                    //clientSocket.SendSync(new SegmentOffset(Encoding.Default.GetBytes("hello1111")), null);
                //}
                //return;

                //异步连接
                clientSocket.ReceiveOffsetHandler = new OnReceivedSegmentHandler((SegmentToken session) =>
                {
                    try
                    {
                        Console.WriteLine("from server" + Encoding.Default.GetString(session.Data.buffer,
                            session.Data.offset, session.Data.size) + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {

                    }
                });
                clientSocket.DisconnectedHandler = new OnDisconnectedHandler((stoken) =>
                {
                    Console.WriteLine("clinet show discount");
                });
                again:
                bool rt = clientSocket.ConnectTo(port, "127.0.0.1");/* 10.152.0.71*/
                if (rt)
                {
                    for (int i = 0; i < 100000; i++)
                    {
                        if (i % 1000 == 0)
                        {
                            Console.WriteLine(clientSocket.BufferPoolCount + ":" + i);
                        }
                        clientSocket.Send(new SegmentOffset(Encoding.Default.GetBytes("client send" + DateTime.Now)), false);
                        //break;
                    }
                    //byte[] buffer = System.IO.File.ReadAllBytes("TRANSACTION_EXTRANSACTIONUPLOAD_REQ_52_1000_20171031143825836.json");

                    //clientSocket.Send(buffer);

                    //Console.WriteLine("complete:sent:" + sentlength.ToString() + "rec:" + reclength.ToString());
                    int ab = 0;
                    while (true)
                    {
                        Thread.Sleep(3000);
                        Console.WriteLine("retry :pool:" + clientSocket.BufferPoolCount);
                        if (ab++ >= 1) break;
                    }

                    //var c = Console.ReadKey();
                    //if (c.KeyChar == 'r') goto again;
                    serverSocket.Stop();

                    //clientSocket.Disconnect();
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
            INetServerProvider serverProvider = NetServerProvider.CreateProvider(4096,32, NetProviderType.Udp);
            serverProvider.ReceivedOffsetHandler = new OnReceivedSegmentHandler((SegmentToken session) =>
            {
                ++svc_c;

                Console.WriteLine("from client:" + Encoding.UTF8.GetString(session.Data.buffer, session.Data.offset, session.Data.size));
                serverProvider.Send(new SegmentToken(session.sToken, Encoding.UTF8.GetBytes("i'm server" + DateTime.Now)));
            });
            if (serverProvider.Start(port))
            {                
                clientProvider = NetClientProvider.CreateProvider(4096, 4, NetProviderType.Udp);
                clientProvider.ReceiveOffsetHandler = new OnReceivedSegmentHandler((SegmentToken session) =>
                {
                    Console.WriteLine("from server :"+ Encoding.UTF8.GetString(session.Data.buffer, session.Data.offset,
                        session.Data.size));

                    ++cli_c;
                });
                bool isConn = clientProvider.ConnectTo(port, "127.0.0.1");

                int c = 10;

                while (c>0)
                {

                    //string msg = Console.ReadLine();
                    //if (msg == "exit")
                    //    break;

                    clientProvider.Send(new SegmentOffset(Encoding.UTF8.GetBytes((--c).ToString())));

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
            byte[] content = new byte[] { 1, 3, 4, 0xfe, 0x01, 0xfd, 0x02 };
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
         
            netServerProvider.AcceptedHandler = new OnAcceptedHandler((sToken) => {
                _sToken = sToken;
                tokenPool.InsertToken(new NetConnectionToken()
                {
                    Token = sToken
                });
            });

            bool isOk = netServerProvider.Start(port);
            if (isOk)
            {
                INetClientProvider netClientProvider = NetClientProvider.CreateProvider();
                netClientProvider.DisconnectedHandler = new OnDisconnectedHandler((sToken) =>
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
                    Console.WriteLine("pool count:"+tokenPool.Count);
                    Console.ReadKey();
                }
            }

        }

        private static void ConnectionPoolTest()
        {
            INetServerProvider serverProvider = NetServerProvider.CreateProvider(4096, 2);
            INetTokenPoolProvider poolProvider = NetTokenPoolProvider.CreateProvider(60);
            List<INetClientProvider> clientPool= new List<INetClientProvider>();

            poolProvider.TimerEnable(false);

            int port = 12345;

            serverProvider.DisconnectedHandler = new OnDisconnectedHandler((s) =>
            {
                Console.WriteLine("server disconnected:"+s.TokenId);
            });
            serverProvider.AcceptedHandler = new OnAcceptedHandler((s) =>
            {
                Console.WriteLine("accept:" + s.TokenId);
                poolProvider.InsertToken(new NetConnectionToken(s));
            });
            serverProvider.ReceivedOffsetHandler = new OnReceivedSegmentHandler((token) => {
                Console.WriteLine("server receive"+token.sToken.TokenId+":"+Encoding.Default.GetString(token.Data.buffer,token.Data.offset,token.Data.size));
            });
            bool isStart = serverProvider.Start(port);
            if (isStart)
            {
                again:
                for (int i = 0; i < 3; ++i)
                {
                    INetClientProvider clientProvider = NetClientProvider.CreateProvider();
                    clientProvider.DisconnectedHandler = new OnDisconnectedHandler((s) =>
                    {
                        Console.WriteLine(" client disconnected:"+s.TokenId);
                    });
                    //clientProvider.ReceiveOffsetHandler = new OnReceiveOffsetHandler((SegmentToken session) =>
                    //{
                    //    Console.WriteLine(session.sToken.TokenIpEndPoint + Encoding.Default.GetString(session.Data.buffer,
                    //        session.Data.offset, session.Data.size));
                    //});
                    bool isConnected = clientProvider.ConnectTo(port, "127.0.0.1");
                    if (isConnected) clientPool.Add(clientProvider);

                    Console.WriteLine("connect:"+isConnected);
                }
                send:
                Console.WriteLine(poolProvider.Count);
                string info = Console.ReadLine();
               
                if (info == "send")
                {
                   for(int i=0;i<poolProvider.Count;++i)
                    {
                        var item=poolProvider.GetTokenById(i);
                        if (item == null) continue;
                        
                        serverProvider.Send(new SegmentToken( item.Token, Encoding.Default.GetBytes(DateTime.Now.ToString())));
                        Thread.Sleep(1000);
                        // poolProvider.Clear(true);
                        //var item = poolProvider.GetTopToken();
                        //if (item != null)
                        //{
                        //    serverProvider.CloseToken(item.Token);
                        //    poolProvider.RemoveToken(item, false);
                        //}
                    }
                    goto send;
                }
                else if (info == "stop")
                {
                    serverProvider.Stop();
                    goto again;
                }
                else if (info == "clear")
                {
                    poolProvider.Clear();
                    clientPool.Clear();

                    goto again;
                }
                else if(info=="client")
                {
                    for (int i = 0; i < clientPool.Count; ++i)
                    {
                        clientPool[i].Send(new SegmentOffset(Encoding.Default.GetBytes(DateTime.Now.ToString())));
                        Thread.Sleep(200);
                    }
                    goto send;
                }
                Console.ReadKey();
            }
        }

        private static void ConnectDemo()
        {
            try
            {
                SocketToken sToken = null;
                INetServerProvider serverProvider = NetServerProvider.CreateProvider(4096, 2);
                serverProvider.DisconnectedHandler = new OnDisconnectedHandler((SocketToken stoken) => {
                    Console.WriteLine("client disconnected" + stoken.TokenIpEndPoint);
                });
                serverProvider.AcceptedHandler = new OnAcceptedHandler((token) => {
                    Console.WriteLine("accpet" + token.TokenIpEndPoint);
                    sToken = token;
                });

                bool isOk = serverProvider.Start(12345);
                if (isOk)
                {
                    INetClientProvider clientProvider = NetClientProvider.CreateProvider();
                    clientProvider.ConnectedHandler = new OnConnectedHandler((SocketToken stoken, bool isConnected) =>
                    {
                        Console.WriteLine("connected" + stoken.TokenIpEndPoint);
                    });
                    clientProvider.DisconnectedHandler = new OnDisconnectedHandler((SocketToken stoken) =>
                    {
                        Console.WriteLine("disconnected" + stoken.TokenIpEndPoint);
                    });
                    again:
                    isOk = clientProvider.ConnectTo(12345, "127.0.0.1");
                    Console.WriteLine(isOk);
                    string info = Console.ReadLine();
                    if (info == "again")
                    {
                        //clientProvider.Disconnect();
                        serverProvider.CloseToken(sToken);
                        goto again;
                    }
                    Console.WriteLine("exit");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.Read();
        }

        private static void PacketSocketDemo()
        {
            int port = 13145;
            INetServerProvider netServerProvider = NetServerProvider.CreateProvider();
            INetProtocolProvider netProtocolProvider = NetProtocolProvider.CreateProvider();
            INetPacketProvider netPacketProvider = NetPacketProvider.CreateProvider(4096*32);//最大容量,必须大于发送缓冲区，建议是设置大于8倍以上

            int pktCnt = 0;
            netServerProvider.ReceivedOffsetHandler = new OnReceivedSegmentHandler((SegmentToken session) =>
            {
                bool isEn = netPacketProvider.SetBlocks(session.Data.buffer, session.Data.offset, session.Data.size);
                if (isEn == false)
                {
                    Console.WriteLine("entry queue failed");
                }
                List<Packet> packets = netPacketProvider.GetBlocks();

                pktCnt += packets.Count;

                if (packets.Count > 0)
                {
                    foreach(var pkt in packets)
                    {
                        Console.WriteLine(Encoding.UTF8.GetString(pkt.pPayload));
                    }
                }
                else
                {
                    Console.WriteLine("got null item from queue");
                }

                Console.WriteLine("pktCnt:"+pktCnt);
            });

            bool isStart= netServerProvider.Start(port);
            if (isStart)
            {
                INetClientProvider netClientProvider = NetClientProvider.CreateProvider();
                bool isConneted= netClientProvider.ConnectTo(port, "127.0.0.1");
                if (isConneted)
                {
                    //for (int i = 0; i < content.Length; ++i)
                    //{
                    //    content[i] = (byte)(i > 255 ? 255 : i);
                    //}
                    int i = 0;
                    for (; i < 100000; ++i)
                    {
                        byte[] content = Encoding.UTF8.GetBytes("hello 哈哈 http://anystore.bouyeijiang.com" + DateTime.Now+i.ToString());

                        byte[] buffer = netProtocolProvider.Encode(new Packet()
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

                        netClientProvider.Send(new SegmentOffset(buffer));
                    }
                }

                Console.ReadKey();
            }

        }

        private static void WebSocketDemo()
        {
            WSServerProvider wsService = new WSServerProvider();
            wsService.OnAccepted = new  OnAcceptedHandler((SocketToken sToken) => {
                Console.WriteLine("accepted:" + sToken.TokenIpEndPoint);
            });
            wsService.OnDisconnected = new  OnDisconnectedHandler((SocketToken sToken) => {
                Console.WriteLine("disconnect:" + sToken.TokenIpEndPoint.ToString());
            });
            wsService.OnReceived = new  OnReceivedHandler((SocketToken sToken, string content) => {

                Console.WriteLine("receive:" + content);
                wsService.Send(sToken, "hello websocket client! you said:" + content);

            });
            wsService.OnReceivedBytes = new  OnReceivedSegmentHandler((SegmentToken session) => {
                Console.WriteLine("receive bytes:" + session.Data.size);
            });
            bool isOk = wsService.Start(65531);
            if (isOk)
            {
                Console.WriteLine("waiting for accept...");

                WSClientProvider client = new WSClientProvider();
                client.OnConnected = new OnConnectedHandler((SocketToken sToken,bool isConnected) => {
                    Console.WriteLine("connected websocket server...");
                });
                client.OnReceived = new  OnReceivedHandler((SocketToken sToken, string msg) => {
                    Console.WriteLine(msg);
                });

                isOk = client.Connect("ws://127.0.0.1:65531");
                if (isOk)
                {
                    client.Send("hello websocket");
                }
                Console.ReadKey();
            }
        }
    }
}
