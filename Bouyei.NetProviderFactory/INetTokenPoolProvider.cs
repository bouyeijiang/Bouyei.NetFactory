using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetProviderFactory
{
    using Pools;
    public interface INetTokenPoolProvider
    {
        int ConnectionTimeout { get; set; }
        void AddToken(NetConnectionToken ncToken);
        void RemoveToken(NetConnectionToken ncToken);
        NetConnectionToken GetTokenById(int Id);
    }
}
