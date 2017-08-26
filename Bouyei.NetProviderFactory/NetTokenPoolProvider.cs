using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetProviderFactory
{
    using Pools;

    public class NetTokenPoolProvider:INetTokenPoolProvider
    {
        TokenConnectionManager tokenConnectionManager = null;
        public int ConnectionTimeout
        {
            get { return tokenConnectionManager.ConnectionTimeout; }
            set { tokenConnectionManager.ConnectionTimeout = value; }
        }

        public static NetTokenPoolProvider CreateNetTokenPoolProvider(int taskExecutePeriod)
        {
            return new NetTokenPoolProvider(taskExecutePeriod);
        }

        public NetTokenPoolProvider(int taskExecutePeriod)
        {
            tokenConnectionManager = new TokenConnectionManager(taskExecutePeriod);
        }

        public void AddToken(NetConnectionToken ncToken)
        {
            tokenConnectionManager.AddToken(ncToken);
        }

        public void RemoveToken(NetConnectionToken ncToken)
        {
            tokenConnectionManager.RemoveToken(ncToken);
        }

        public NetConnectionToken GetTokenById(int Id)
        {
          return  tokenConnectionManager.GetTokenById(Id);
        }
    }
}
