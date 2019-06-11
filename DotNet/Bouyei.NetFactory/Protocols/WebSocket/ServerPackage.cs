﻿using Bouyei.NetFactory.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bouyei.NetFactory.Protocols.WebSocketProto
{
    public class ServerPackage:DataFrameInfo
    {
        private Encoding encoding = Encoding.UTF8;
        private const string acceptMask = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";//固定字符串
        private readonly string[] splitChars = null;

        public ServerPackage()
        {
            splitChars = new string[] { ": " };
        }

        public string RspAcceptPackage(AccessInfo access)
        {
            var accept= new AcceptInfo()
            {
                Connection = access.Connection,
                Upgrade = access.Upgrade,
                SecWebSocketLocation = access.Host,
                SecWebSocketOrigin = access.Origin,
                SecWebSocketAccept = (access.SecWebSocketKey + acceptMask).ToSha1Base64()
            };

            return accept.ToString();
        }

        public SegmentOffset GetBytes(string content)
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

        public SegmentOffset GetBytes(OpCodeType code=OpCodeType.Bin)
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

        public AccessInfo GetHandshakePackage(SegmentOffset segOffset)
        {
            string msg = encoding.GetString(segOffset.buffer, segOffset.offset, segOffset.size);
            string[] items = msg.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            if (items.Length < 6)
                throw new Exception("access format error..." + msg);

            AccessInfo access = new AccessInfo()
            {
                HttpProto = items[0]
            };

            foreach (var item in items)
            {
                string[] kv = item.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                switch (kv[0])
                {
                    case "Connection":
                        access.Connection = kv[1];
                        break;
                    case "Host":
                        access.Host = kv[1];
                        break;
                    case "Origin":
                        access.Origin = kv[1];
                        break;
                    case "Upgrade":
                        access.Upgrade = kv[1];
                        break;
                    case "Sec-WebSocket-Key":
                        access.SecWebSocketKey = kv[1];
                        break;
                    case "Sec-WebSocket-Version":
                        access.SecWebSocketVersion = kv[1];
                        break;
                    case "Sec-WebSocket-Extensions":
                        access.SecWebSocketExtensions = kv[1];
                        break;
                }
            }
            return access;
        }
    }
}
