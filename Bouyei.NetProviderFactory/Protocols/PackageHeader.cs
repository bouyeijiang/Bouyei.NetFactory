using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetProviderFactory.Protocols
{
    public class PackageHeader
    {
        public byte packageFlag { get;private set; } = 0xff;

        public UInt16 packageId { get; set; }

        public PackageAttribute packageAttribute { get; set; }

    }

}
