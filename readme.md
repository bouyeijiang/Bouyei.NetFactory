
.net������socket�첽ͨ�ſ⣬��������ģ�飺
1��tcp����ˡ��ͻ��˵�ͬ�����첽ģ�飻
2��udp����ˡ��ͻ���ģ�飻
3���ͻ������ӳض�������ģ�飻
4�����ݰ�Э��֧��ģ�飻
5�����ݰ�����������ģ�飨�Զ��ְ�����ճ�������������


tcp����˿ͻ������ӣ�

 //�����


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
                //�ͻ���
              INetClientProvider clientSocket = NetClientProvider.CreateProvider();
              clientSocket.SentHanlder = new OnSentHandler((stoken, buff,offset,cont) =>
                {
                    
                });
                //�첽����
            clientSocket.ReceiveOffsetHanlder = new OnReceiveOffsetHandler((sToken, buff, offset, count)=>
                {
                     
                });
                clientSocket.DisconnectedHanlder = new OnDisconnectedHandler((stoken) =>
                {
                    Console.WriteLine("clinet discount");
                });
                bool rt = clientSocket.ConnectTo(port, "127.0.0.1");
		clientSocket.Send(sendbuffer, false);
          }

      	   //Э��ģ�����ӣ�
            INetProtocolProvider protocolProvider = NetProtocolProvider.CreateProvider();

            //�������ݴ�����ֽ�
            byte[] content = new byte[] { 1, 3, 4, 0xfe, 0x01, 0xfd,0x02 };
            byte[] buffer= protocolProvider.Encode(new Packet()
            {
                pHeader = new PacketHeader()
                {
                    packetAttribute = new PacketAttribute()
                    {
                        packetCount = 1,//�Զ���,ָ������Ϣ��Ҫ�ֶ��ٸ����ݰ����Ͳ����
                    },
                    packetId = 0x10//����ҵ���Զ���
                },
                pPayload = content//Я������������
            });

            //ʹ�ý��չ�����ؽ������ݰ�
            INetPacketProvider pkgProvider = NetPacketProvider.CreateProvider(1024);
            bool rt= pkgProvider.SetBlocks(buffer, 0, buffer.Length);
            rt = pkgProvider.SetBlocks(buffer, 0, buffer.Length);
            var dePkg= pkgProvider.GetBlocks();


	   //���ӳع���ģ�飺	

            int port = 13145;
            INetServerProvider netServerProvider = NetServerProvider.CreateProvider();
            INetTokenPoolProvider tokenPool = NetTokenPoolProvider.CreateProvider(60);
            tokenPool.ConnectionTimeout = 60;
         
            netServerProvider.AcceptHandler = new OnAcceptHandler((sToken) => {
                tokenPool.InsertToken(new NetConnectionToken()
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
                    Console.WriteLine("pool count:"+tokenPool.Count);
                    Console.ReadKey();
                }
            }