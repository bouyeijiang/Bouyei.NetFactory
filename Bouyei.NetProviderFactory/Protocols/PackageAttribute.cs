using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetProviderFactory.Protocols
{
    public class PackageAttribute
    {
        public byte paCryptFlag { get; set; }

        public UInt32 payloadLength { get; set; }
    }
}
