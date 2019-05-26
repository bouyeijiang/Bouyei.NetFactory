using System.Net.Sockets;

namespace Bouyei.NetFactoryCore
{
    /// <summary>
    /// 接受数据处理，返回预设的缓冲区大小和实际接收到的数据偏移和数量
    /// </summary>
    /// <param name="sToken"></param>
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    public delegate void OnReceivedSegmentHandler(SegmentToken segToken);
    /// <summary>
    /// 接收字符串事件
    /// </summary>
    /// <param name="sToken"></param>
    /// <param name="content"></param>
    public delegate void OnReceivedHandler(SocketToken sToken, string content);

    /// <summary>
    /// 发送数据处理
    /// </summary>
    /// <param name="sToken"></param>
    /// <param name="bytesTransferred"></param>
    public delegate void OnSentHandler(SegmentToken segToken);
    
    /// <summary>
    /// 接受连接对象处理
    /// </summary>
    /// <param name="sToken"></param>
    public delegate void OnAcceptedHandler(SocketToken sToken);
    
    /// <summary>
    /// 断开连接对象处理
    /// </summary>
    /// <param name="sToken"></param>
    public delegate void OnDisconnectedHandler(SocketToken sToken);

    /// <summary>
    /// 建立连接对象处理
    /// </summary>
    /// <param name="sToken"></param>
    /// <param name="isConnected"></param>
    public delegate void OnConnectedHandler(SocketToken sToken,bool isConnected);
}