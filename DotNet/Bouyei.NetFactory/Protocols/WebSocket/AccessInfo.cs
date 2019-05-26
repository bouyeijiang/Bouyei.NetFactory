using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Bouyei.NetFactory.Protocols.WebSocketProto
{
    public class AccessInfo : BaseInfo
    {
        /// <summary>
        /// 连接主机
        /// </summary>
        public string Host { get; set; }
        /// <summary>
        /// 连接源
        /// </summary>
        public string Origin { get; set; }
        /// <summary>
        /// 安全扩展
        /// </summary>
        public string SecWebSocketExtensions { get; set; }
        /// <summary>
        /// 安全密钥
        /// </summary>
        public string SecWebSocketKey { get; set; }
        /// <summary>
        /// 安全版本
        /// </summary>
        public string SecWebSocketVersion { get; set; }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(HttpProto))
                HttpProto = "GET / HTTP/1.1";

            if (string.IsNullOrEmpty(SecWebSocketVersion))
                SecWebSocketVersion = "13";

            return string.Format("{0}{1}{2}{3}{4}{5}{6}",
                HttpProto + Environment.NewLine,
                "Host: " + Host + Environment.NewLine,
                "Connection: " + Connection + Environment.NewLine,
                "Upgrade: " + Upgrade + Environment.NewLine,
                "Origin: " + Origin + Environment.NewLine,
                "Sec-WebSocket-Version: " + SecWebSocketVersion + Environment.NewLine,
                "Sec-WebSocket-Key: " + SecWebSocketKey + Environment.NewLine+Environment.NewLine);
        }

        public bool IsHandShaked()
        {
            return string.IsNullOrEmpty(SecWebSocketKey) == false;
        }
    }

    public class BaseInfo
    {
        /// <summary>
        /// http连接协议
        /// </summary>
        public string HttpProto { get; set; }
        /// <summary>
        /// 连接类型
        /// </summary>
        public string Connection { get; set; }
        /// <summary>
        /// 连接方式
        /// </summary>
        public string Upgrade { get; set; }

        public BaseInfo()
        {
            Upgrade = "websocket";
            Connection = "Upgrade";
        }
    }
}