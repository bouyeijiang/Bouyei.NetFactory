using System;
using System.Text;
using System.Threading;
using Bouyei.NetFactoryCore;

namespace NetFactoryCoreDemo
{
    using Bouyei.NetFactoryCore.WebSocket;
    using Bouyei.NetFactoryCore.Protocols.WebSocketProto;
    using System.Collections.Generic;

    class Program
    {
        static void Main(string[] args)
        {
           WebSocketDemo();
             //TcpDemo();
            //UdpDemo();
            //ConnectionPoolTest();
        }

        private static void TcpDemo()
        {
            int port = 12346;
            //服务端
            INetServerProvider serverSocket = NetServerProvider.CreateProvider();
            INetTokenPoolProvider poolProvider = NetTokenPoolProvider.CreateProvider(1000 * 180);
             
            serverSocket.ReceivedOffsetHandler = new OnReceivedSegmentHandler((SegmentToken session) =>
            {
                try
                {
                    Console.WriteLine("from client "+Encoding.Default.GetString(session.Data.buffer,session.Data.offset,session.Data.size));
                    serverSocket.Send(new SegmentToken(session.sToken, Encoding.Default.GetBytes("i'm server")));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            });
            serverSocket.AcceptedHandler = new OnAcceptedHandler((sToken) =>
            {
                poolProvider.InsertToken(new Bouyei.NetFactoryCore.Pools.NetConnectionToken(sToken));

                serverSocket.Send(new SegmentToken()
                {
                    sToken = sToken,
                    Data = new SegmentOffset()
                    {
                        buffer = Encoding.Default.GetBytes("welcome" + DateTime.Now.ToString())
                    }
                }, false);

                Console.WriteLine("accept" + sToken.TokenIpEndPoint);
            });

            serverSocket.DisconnectedHandler = new OnDisconnectedHandler((stoken) =>
            {
                poolProvider.RemoveToken(new Bouyei.NetFactoryCore.Pools.NetConnectionToken(stoken));

                Console.WriteLine("disconnect" + stoken.TokenId);
            });

            bool isOk = serverSocket.Start(port);
            if (isOk)
            {
                Console.WriteLine("已启动服务。。。");

                //客户端
                INetClientProvider clientSocket = NetClientProvider.CreateProvider();

                //异步连接
                clientSocket.ReceivedOffsetHandler = new OnReceivedSegmentHandler((SegmentToken session) =>
                {
                    try
                    {
                       Console.WriteLine("from server:" + Encoding.Default.GetString(session.Data.buffer,session.Data.offset,session.Data.size));
                    }
                    catch (Exception ex)
                    {

                    }
                });
                clientSocket.DisconnectedHandler = new OnDisconnectedHandler((stoken) =>
                {
                    Console.WriteLine("clinet discount");
                });
                again:
                bool rt = clientSocket.ConnectTo(port, "127.0.0.1");/* 10.152.0.71*/
                if (rt)
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        // Thread.Sleep(50);
                        if (i % 100 == 0)
                        {
                            Console.WriteLine(clientSocket.BufferPoolCount + ":" + i);
                        }
                        bool isTrue = clientSocket.Send(new SegmentOffset(Encoding.Default.GetBytes("hello"+DateTime.Now)), false);
                        //if (isTrue == false) break;
                        //break;
                    }
                }
            }
            Console.ReadKey();
            serverSocket.Dispose();
        }
       
        private static void WebSocketDemo()
        {
            WSServerProvider wsService = new WSServerProvider();
            wsService.OnAccepted = new OnAcceptedHandler((SocketToken sToken) => {
                Console.WriteLine("accepted:"+sToken.TokenIpEndPoint);
            });
            wsService.OnDisconnected = new OnDisconnectedHandler((SocketToken sToken) => {
                Console.WriteLine("disconnect:"+sToken.TokenIpEndPoint.ToString());
            });
            wsService.OnReceived = new OnReceivedHandler((SocketToken sToken,string content) => {

                Console.WriteLine("receive:" +content);
                wsService.Send(sToken, "hello websocket client! you said:" + content);

            });
            wsService.OnReceivedBytes = new  OnReceivedSegmentHandler((SegmentToken data) => {
                Console.WriteLine("receive bytes:"+Encoding.UTF8.GetString(data.Data.buffer,
                    data.Data.offset,data.Data.size));
            });
            bool isOk = wsService.Start(65531);
            if(isOk)
            {
                Console.WriteLine("waiting for accept...");

                WSClientProvider client = new WSClientProvider();
                client.OnConnected = new OnConnectedHandler((SocketToken sToken,bool isConnected) => {
                    Console.WriteLine("connected websocket server...");
                });
                client.OnReceived = new OnReceivedHandler((SocketToken sToken,string msg) => {
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

        private static void ConnectionPoolTest()
        {
            INetServerProvider serverProvider = NetServerProvider.CreateProvider(4096, 2);
            INetTokenPoolProvider poolProvider = NetTokenPoolProvider.CreateProvider(60);
            List<INetClientProvider> clientPool = new List<INetClientProvider>();

            poolProvider.TimerEnable(false);

            int port = 12345;

            serverProvider.DisconnectedHandler = new OnDisconnectedHandler((s) =>
            {
                Console.WriteLine("server disconnected:" + s.TokenId);
            });
            serverProvider.AcceptedHandler = new OnAcceptedHandler((s) =>
            {
                Console.WriteLine("accept:" + s.TokenId);
                poolProvider.InsertToken(new Bouyei.NetFactoryCore.Pools.NetConnectionToken(s));
            });
            serverProvider.ReceivedOffsetHandler = new  OnReceivedSegmentHandler((token) => {
                Console.WriteLine("server receive" + token.sToken.TokenId + ":" + Encoding.Default.GetString(token.Data.buffer, token.Data.offset, token.Data.size));
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
                        Console.WriteLine(" client disconnected:" + s.TokenId);
                    });
                    //clientProvider.ReceiveOffsetHandler = new OnReceiveOffsetHandler((SegmentToken session) =>
                    //{
                    //    Console.WriteLine(session.sToken.TokenIpEndPoint + Encoding.Default.GetString(session.Data.buffer,
                    //        session.Data.offset, session.Data.size));
                    //});
                    bool isConnected = clientProvider.ConnectTo(port, "127.0.0.1");
                    if (isConnected) clientPool.Add(clientProvider);

                    Console.WriteLine("connect:" + isConnected);
                }
                send:
                Console.WriteLine(poolProvider.Count);
                string info = Console.ReadLine();

                if (info == "send")
                {
                    for (int i = 0; i < poolProvider.Count; ++i)
                    {
                        var item = poolProvider.GetTokenById(i);
                        if (item == null) continue;

                        serverProvider.Send(new SegmentToken(item.Token, Encoding.Default.GetBytes(DateTime.Now.ToString())));
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
                else if (info == "client")
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

        private static void UdpDemo()
        {
            int port = 12345;
            int svc_c = 0, cli_c = 0, cli_c2 = 0;
            INetClientProvider clientProvider = null;
            INetServerProvider serverProvider = NetServerProvider.CreateProvider(256, 4, NetProviderType.Udp);
            serverProvider.ReceivedOffsetHandler = new  OnReceivedSegmentHandler((SegmentToken session) =>
            {
                ++svc_c;
                Console.WriteLine("from client:" + Encoding.UTF8.GetString(session.Data.buffer, session.Data.offset, session.Data.size));
                serverProvider.Send(new SegmentToken(session.sToken, Encoding.UTF8.GetBytes("i'm server" + DateTime.Now)));
            });
            if (serverProvider.Start(port))
            {
                clientProvider = NetClientProvider.CreateProvider(4096, 4, NetProviderType.Udp);
                clientProvider.ReceivedOffsetHandler = new OnReceivedSegmentHandler((SegmentToken session) =>
                {
                    Console.WriteLine("from server :" + Encoding.UTF8.GetString(session.Data.buffer, session.Data.offset,
                        session.Data.size));
                    ++cli_c;
                });
                bool isConn = clientProvider.ConnectTo(port, "127.0.0.1");

                int c = 10;

                while (c > 0)
                {

                    //string msg = Console.ReadLine();
                    //if (msg == "exit")
                    //    break;

                    clientProvider.Send(new SegmentOffset(Encoding.UTF8.GetBytes((--c).ToString())));

                   // Thread.Sleep(500);

                }
            }
            Console.WriteLine(string.Format("完成svc:{0};cli1:{1};cli2:{2}", svc_c, cli_c, cli_c2));

            Console.ReadKey();
            serverProvider.Dispose();
            clientProvider.Dispose();
        }
    }
}
