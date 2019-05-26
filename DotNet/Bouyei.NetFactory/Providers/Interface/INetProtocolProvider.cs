using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetFactory
{
    using Protocols.PacketProto;

   public interface INetProtocolProvider
    {
        Packet Decode(byte[] buffer, int offset, int size);

        byte[] Encode(Packet pkg);

    }
}
