using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetFactory.Protocols.WebSocketProto
{
    public class UrlInfo
    {
        public string Proto { get; set; }

        public string Domain { get; set; }

        public int Port { get; set; } = 80;
    }
}
