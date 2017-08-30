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
        int Count { get; }
        void AddToken(NetConnectionToken ncToken);
        bool RemoveToken(NetConnectionToken ncToken,bool isClose=true);
        NetConnectionToken GetTokenById(int Id);
        NetConnectionToken GetTokenBySocketToken(SocketToken sToken);
        bool RefreshExpireToken(SocketToken sToken);
    }
}
