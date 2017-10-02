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

namespace Bouyei.NetFactory
{
   public interface INetServerProvider:IDisposable
    {
        OnReceiveHandler ReceiveHandler { get; set; }

        OnSentHandler SentHandler { get; set; }

        OnAcceptHandler AcceptHandler { get; set; }

        OnReceiveOffsetHandler ReceiveOffsetHandler { get; set; }

        OnDisconnectedHandler DisconnectedHandler { get; set; }

        bool Start(int port, string ip = "0.0.0.0");

        void Send(SocketToken sToken, byte[] buffer,bool isWaiting=true);

        void Send(SocketToken sToken, byte[] buffer, int offset, int size, bool isWaiting = true);

        int SendSync(SocketToken sToken, byte[] buffer);

        int SendSync(SocketToken sToken, byte[] buffer, int offset, int size);

        void Stop();
        void CloseToken(SocketToken sToken);

    }
}
