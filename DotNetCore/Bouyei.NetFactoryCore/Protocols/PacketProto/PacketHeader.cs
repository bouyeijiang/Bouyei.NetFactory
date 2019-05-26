using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetFactoryCore.Protocols.PacketProto
{
    public class PacketHeader
    {
        /// <summary>
        /// 包类型标识(自定义类型值)
        /// </summary>
        public UInt16 packetId { get; set; }
        /// <summary>
        /// 包类型(扩展保留)
        /// </summary>
        public byte packetType { get; set; }
        /// <summary>
        /// 包属性
        /// </summary>
        public PacketAttribute packetAttribute { get; set; }

    }

}
