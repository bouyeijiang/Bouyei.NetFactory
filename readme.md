#�ͻ��ˡ��������������

	int port = 12346;
	//�����
	INetServerProvider serverSocket = NetServerProvider.CreateProvider();      
	//���յ����ݰ��¼�
	serverSocket.ReceiveOffsetHandler = new OnReceiveOffsetHandler((sToken, buff, offset, count) =>
	{
		try
		{
			string info = Encoding.UTF8.GetString(buff, offset, count);
			Console.WriteLine(info);
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
		}
	});
	//���յ��ͻ�������
	serverSocket.AcceptHandler = new OnAcceptHandler((sToken) =>
	{
		serverSocket.Send(new SegmentOffsetToken()
		{
			sToken = sToken,
			dataSegment = new SegmentOffset()
			{
				buffer = Encoding.Default.GetBytes("welcome" + DateTime.Now.ToString())
			}
		}, false);

		Console.WriteLine("accept" + sToken.TokenIpEndPoint);
	});
	//�յ��Ͽ�����
	serverSocket.DisconnectedHandler = new OnDisconnectedHandler((stoken) =>
	{
		Console.WriteLine("disconnect" + stoken.TokenId);
	});
	bool isOk = serverSocket.Start(port);
	if (isOk)
	{
		Console.WriteLine("���������񡣡���");
		//�ͻ���
		INetClientProvider clientSocket = NetClientProvider.CreateProvider();

		//�첽����
		clientSocket.ReceiveOffsetHandler = new OnReceiveOffsetHandler((sToken, buff, offset, count) =>
		{
			try
			{
				Console.WriteLine("rec:" + Encoding.Default.GetString(buff,offset,count));
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
					Thread.Sleep(50);
				if (i % 100 == 0)
				{
					Console.WriteLine(clientSocket.BufferPoolCount + ":" + i);
				}
				bool isTrue = clientSocket.Send(new SegmentOffset(Encoding.Default.GetBytes("hello"+DateTime.Now)), false);
			}
		}
	}

#Э��ģ������

	INetProtocolProvider protocolProvider = NetProtocolProvider.CreateProvider();
	//�������ݴ�����ֽ�
	byte[] content = new byte[] { 1, 3, 4, 0xfe, 0x01, 0xfd, 0x02 };
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

#WebSocket ����

	WSServerProvider wsService = new WSServerProvider();
    wsService.Accepted = new Accepted((SocketToken sToken) => {
        Console.WriteLine("accepted:" + sToken.TokenIpEndPoint);
    });
    wsService.Disconnected = new Disconnected((SocketToken sToken) => {
        Console.WriteLine("disconnect:" + sToken.TokenIpEndPoint.ToString());
    });
    wsService.Received = new Received((SocketToken sToken, string content) => {

        Console.WriteLine("receive:" + content);
        wsService.Send(sToken, "hello websocket client! you said:" + content);

    });
    wsService.ReceivedBytes = new ReceivedBytes((SegmentOffset data) => {
        Console.WriteLine("receive bytes:" + data.size);
    });
    bool isOk = wsService.Start(65531);
    if (isOk)
	{
	   Console.WriteLine("waiting for accept...");
	}