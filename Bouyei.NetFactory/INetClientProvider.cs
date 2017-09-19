/*-------------------------------------------------------------
 *   auth: bouyei
 *   date: 2017/7/27 22:16:10
 *contact: 453840293@qq.com
 *profile: www.openthinking.cn
 *   guid: a396449f-39c1-4f32-9a22-417a6c727364
---------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetFactory
{
    public interface INetClientProvider:IDisposable
    {
        bool IsConnected { get; }
        OnReceiveHandler ReceiveHanlder { get; set; }

        OnSentHandler SentHanlder { get; set; }

        OnReceiveOffsetHandler ReceiveOffsetHanlder { get; set; }

        OnDisconnectedHandler DisconnectedHanlder { get; set; }

        ChannelProviderType ChannelProviderType { get; }
        int SendBufferNumber { get; }
        NetProviderType NetProviderType { get;}
        void Disconnect();

        void Connect(int port, string ip);

        bool ConnectTo(int port, string ip);

        void Send(byte[] buffer, bool isWaiting = true);

        void Send(byte[] buffer, int offset, int size, bool isWaiting = true);

        bool ConnectSync(int port, string ip);

        void SendSync(byte[] buffer, Action<int, byte[]> recAct = null, int recBufferSize = 4096);

        void ReceiveSync(Action<int, byte[]> recAct, int recBufferSize = 4096);
    }
}
