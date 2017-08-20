using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetProviderFactory.Protocols
{
    public class Package
    {
        public PackageHeader pHeader { get; set; }

        public byte[] pPayload { get; set; }
    }
}
