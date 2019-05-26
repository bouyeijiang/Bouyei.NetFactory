using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetFactory.Protocols.PacketProto
{
    public class PacketAttribute
    {
        /// <summary>
        /// 数据内容包总数
        /// </summary>
        public UInt16 packetCount { get; set; } = 1;
        /// <summary>
        /// 数据内容长度
        /// </summary>
        public UInt32 payloadLength { get;internal set; }
    }
}
