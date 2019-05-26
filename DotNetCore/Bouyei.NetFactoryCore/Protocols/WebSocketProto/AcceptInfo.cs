using System;
using System.Collections.Generic;
using System.Text;

namespace Bouyei.NetFactoryCore.Protocols.WebSocketProto
{
    public class AcceptInfo : BaseInfo
    {
        /// <summary>
        /// 接入访问验证码
        /// </summary>
        public string SecWebSocketAccept { get; set; }
        /// <summary>
        /// 客户端来源
        /// </summary>
        public string SecWebSocketLocation { get; set; }
        /// <summary>
        /// 服务端来源
        /// </summary>
        public string SecWebSocketOrigin { get; set; }

        //public string SecWebSocketProtocol { get; set; } = "chat";

        public override string ToString()
        {
            if (string.IsNullOrEmpty(HttpProto))
                HttpProto = "HTTP/1.1 101 Switching Protocols";

            //if (Date == DateTime.MinValue)
            //    Date = DateTime.Now;

            return string.Format("{0}{1}{2}{3}",
                HttpProto + Environment.NewLine,
                "Connection: " + Connection + Environment.NewLine,
                "Upgrade: " + Upgrade + Environment.NewLine,
                 //"Sec-WebSocket-Protocol: " + SecWebSocketProtocol + Environment.NewLine,
                 //"Sec-WebSocket-Origin: " + SecWebSocketOrigin + Environment.NewLine,
                // "Server: " + Server + Environment.NewLine,
                // "Date: " + Date.ToString("r") + Environment.NewLine,
                // "Sec-WebSocket-Location: " + SecWebSocketLocation + Environment.NewLine,
                 "Sec-WebSocket-Accept: " + SecWebSocketAccept + Environment.NewLine + Environment.NewLine//很重要，需要两个newline
                );
        }

        public bool IsHandShaked()
        {
            return string.IsNullOrEmpty(SecWebSocketAccept) == false;
        }
    }
}
