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

namespace Bouyei.NetFactoryCore
{
    public interface INetClientProvider:IDisposable
    {
        bool IsConnected { get; }
        OnReceivedHandler ReceivedHandler { get; set; }

        OnSentHandler SentHandler { get; set; }

        OnReceivedSegmentHandler ReceivedOffsetHandler { get; set; }

        OnDisconnectedHandler DisconnectedHandler { get; set; }

        OnConnectedHandler ConnectedHandler { get; set; }

        ChannelProviderType ChannelProviderType { get; }
        int BufferPoolCount { get; }
        NetProviderType NetProviderType { get;}
        void Disconnect();

        void Connect(int port, string ip);

        bool ConnectTo(int port, string ip);

        bool Send(SegmentOffset dataSegment, bool waiting = true);
        bool Send(string content, bool waiting = true);

        bool ConnectSync(int port, string ip);

        void SendSync(SegmentOffset sendSegment,SegmentOffset receiveSegment);

        void ReceiveSync(SegmentOffset receiveSegment,Action<SegmentOffset> receiveAction);
    }
}
