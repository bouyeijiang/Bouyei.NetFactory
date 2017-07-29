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

namespace Bouyei.NetProviderFactory
{
   public interface INetServerProvider
    {
        OnReceiveHandler ReceiveHanlder { get; set; }

        OnSentHandler SentHanlder { get; set; }

        OnAcceptHandler AcceptHandler { get; set; }

        OnReceiveOffsetHandler ReceiveOffsetHanlder { get; set; }

        OnDisconnectedHandler DisconnectedHanlder { get; set; }

        bool Start(int port, string ip = "0.0.0.0");

        void Send(SocketToken sToken, byte[] buffer);

        void Stop();
    }
}
