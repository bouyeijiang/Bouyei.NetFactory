using Bouyei.NetFactoryCore.Base;
using System;
using System.Text;

namespace Bouyei.NetFactoryCore.Protocols.WebSocketProto
{
    public class ClientPackage:DataFrameInfo
    {
        private Encoding encoding = Encoding.UTF8;

        public string ReqAccessPackage()
        {
            return new AccessInfo().ToString();
        }

        public AcceptInfo GetAcceptPackage(string msg)
        {
            string[] msgs = msg.Split(Environment.NewLine);
            var acceptInfo = new AcceptInfo
            {
                HttpProto = msgs[0]
            };

            foreach (var item in msgs)
            {
                string[] kv = item.Split(": ");
                switch (kv[0])
                {
                    case "Upgrade":
                        acceptInfo.Upgrade = kv[1];
                        break;
                    case "Connection":
                        acceptInfo.Connection = kv[1];
                        break;
                    case "Sec-WebSocket-Accept":
                        acceptInfo.SecWebSocketAccept = kv[1];
                        break;
                    case "Sec-WebSocket-Location":
                        acceptInfo.SecWebSocketLocation = kv[1];
                        break;
                    case "Sec-WebSocket-Origin":
                        acceptInfo.SecWebSocketOrigin = kv[1];
                        break;
                }
            }
            return acceptInfo;
        }

        public SegmentOffset GetPackageBytes(string content)
        {
            var buf = encoding.GetBytes(content);
            Payload = new SegmentOffset()
            {
                buffer = buf
            };

            //Mask = true;
            //MaskKey = new byte[4] {
            //     1,3,
            //     2,4
            //};

            //Payload.buffer = encoding.GetBytes(content);
            PayloadLength = Payload.buffer.LongLength;

            return new SegmentOffset(EncodingToBytes());
        }

        public SegmentOffset GetPackageBytes(byte[] buf)
        {
            Payload = new SegmentOffset(buf);

            //Mask = true;
            //MaskKey = new byte[4] {
            //     1,3,
            //     2,4
            //};

            PayloadLength = Payload.buffer.LongLength;

            return new SegmentOffset(EncodingToBytes());
        }

        public SegmentOffset GetPackageBytes(SegmentOffset buf)
        {
            Payload = buf;

            //Mask = true;
            //MaskKey = new byte[4] {
            //     1,3,
            //     2,4
            //};

            PayloadLength = Payload.buffer.LongLength;

            return new SegmentOffset(EncodingToBytes());
        }

        public SegmentOffset GetBytes(OpCodeType code = OpCodeType.Bin)
        {
            OpCode = (byte)code;
            if (Payload == null)
            {
                Payload = new SegmentOffset();
                Payload.buffer = new byte[] { };
            }

            PayloadLength = Payload.buffer.LongLength;

            return new SegmentOffset(EncodingToBytes());
        }
    }

    [Flags]
    public enum OpCodeType:byte
    {
        Attach = 0x0,
        Text = 0x1,
        Bin = 0x2,
        Close = 0x8,
        Bing = 0x9,
        Bong = 0xA,
    }
}
