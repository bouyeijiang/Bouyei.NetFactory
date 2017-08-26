using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetProviderFactory
{
    public interface INetPacketProvider
    {
        bool SetBlock(byte[] buffer, int offset, int size);

        Protocols.Packet GetBlock();
    }
}
