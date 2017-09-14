
.net高性能socket异步通信库，包含功能模块：
1、tcp服务端、客户端的同步和异步模块；
2、udp服务端、客户端模块；
3、客户端连接池独立管理模块；
4、数据包协议支持模块；
5、数据包解析缓冲区模块（自动分包处理粘包处理解析）；


tcp服务端客户端例子：

 //服务端


 	   INetServerProvider serverSocket = NetServerProvider.CreateProvider();
 	   serverSocket.ReceiveOffsetHanlder = new OnReceiveOffsetHandler((sToken, buff, offset, count) =>
            {
 		//receive todo
            });
            serverSocket.AcceptHandler = new OnAcceptHandler((sToken) =>
            {
               //accept client todo
            });
            serverSocket.SentHanlder = new OnSentHandler((stoken,buff, offset,count) =>
            {
                //sent complete todo
            });
            serverSocket.DisconnectedHanlder = new OnDisconnectedHandler((stoken) =>
            {
                Console.WriteLine("disconnect" + stoken.TokenId);
            });


	   bool isOk = serverSocket.Start(port);
            if (isOk)
            {
                //客户端
                INetClientProvider clientSocket = NetClientProvider.CreateProvider();
                clientSocket.SentHanlder = new OnSentHandler((stoken, buff,offset,cont) =>
                {
                    
                });
                //异步连接
                clientSocket.ReceiveOffsetHanlder = new OnReceiveOffsetHandler((sToken, buff, offset, 		count) =>
                {
                     
                });
                clientSocket.DisconnectedHanlder = new OnDisconnectedHandler((stoken) =>
                {
                    Console.WriteLine("clinet discount");
                });
                bool rt = clientSocket.ConnectTo(port, "127.0.0.1");
		clientSocket.Send(sendbuffer, false);
          }

      	   //协议模块例子：
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
