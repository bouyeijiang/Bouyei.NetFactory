using System.Net.Sockets;

namespace Bouyei.NetFactory
{
    /// <summary>
    /// 接收数据处理,返回的是实际接收到的数据
    /// </summary>
    /// <param name="sToken"></param>
    /// <param name="buffer"></param>
    public delegate void OnReceiveHandler(SocketToken sToken, byte[] buffer);

    /// <summary>
    /// 接受数据处理，返回的数预设的缓冲区大小和实际接收到的数据偏移和数量
    /// </summary>
    /// <param name="sToken"></param>
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    public delegate void OnReceiveOffsetHandler(SocketToken sToken,byte[] buffer,int offset, int count);

    /// <summary>
    /// 发送数据处理
    /// </summary>
    /// <param name="sToken"></param>
    /// <param name="bytesTransferred"></param>
    public delegate void OnSentHandler(SocketToken sToken, byte[] buffer, int offset, int count);
    
    /// <summary>
    /// 接受连接对象处理
    /// </summary>
    /// <param name="sToken"></param>
    public delegate void OnAcceptHandler(SocketToken sToken);
    
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