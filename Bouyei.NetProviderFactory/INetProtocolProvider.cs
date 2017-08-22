using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetProviderFactory
{
    using Protocols;

   public interface INetProtocolProvider
    {
        Package Decode(byte[] buffer, int offset, int size);

        byte[] Encode(Package pkg);

    }
}
