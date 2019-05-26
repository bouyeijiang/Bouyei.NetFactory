/*-------------------------------------------------------------
 *   auth: bouyei
 *   date: 2017/7/29 13:43:52
 *contact: 453840293@qq.com
 *profile: www.openthinking.cn
 *   guid: 49fa9423-0ff2-4625-a2f3-7f763e7df41e
---------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetFactoryCore
{
   public interface INetServerProvider:IDisposable
    {
        OnReceivedHandler ReceivedHandler { get; set; }

        OnSentHandler SentHandler { get; set; }

        OnAcceptedHandler AcceptedHandler { get; set; }

        OnReceivedSegmentHandler ReceivedOffsetHandler { get; set; }
        OnDisconnectedHandler DisconnectedHandler { get; set; }

        bool Start(int port, string ip = "0.0.0.0");

        bool Send(SegmentToken segToken,bool waiting =true);
        bool Send(SocketToken sToken, string content, bool waiting = true);
 
        int SendSync(SegmentToken segToken);

        void Stop();

        void CloseToken(SocketToken sToken);
    }
}
