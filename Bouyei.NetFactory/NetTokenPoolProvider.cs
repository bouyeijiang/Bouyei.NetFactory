using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetFactory
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

        public int Count { get { return tokenConnectionManager.Count; } }

        public static NetTokenPoolProvider CreateProvider(int taskExecutePeriod)
        {
            return new NetTokenPoolProvider(taskExecutePeriod);
        }

        public NetTokenPoolProvider(int taskExecutePeriod)
        {
            tokenConnectionManager = new TokenConnectionManager(taskExecutePeriod);
        }

        public void TimerEnable(bool isContinue)
        {
            tokenConnectionManager.TimerEnable(isContinue);
        }

        public NetConnectionToken GetTopToken()
        {
           return tokenConnectionManager.GetTopToken();
        }

        public void InsertToken(NetConnectionToken ncToken)
        {
            tokenConnectionManager.InsertToken(ncToken);
        }

        public bool RemoveToken(NetConnectionToken ncToken,bool isClose=true)
        {
          return  tokenConnectionManager.RemoveToken(ncToken,isClose);
        }

        public NetConnectionToken GetTokenById(int Id)
        {
          return  tokenConnectionManager.GetTokenById(Id);
        }

        public NetConnectionToken GetTokenBySocketToken(SocketToken sToken)
        {
            return tokenConnectionManager.GetTokenBySocketToken(sToken); 
        }

        public bool RefreshExpireToken(SocketToken sToken)
        {
            return tokenConnectionManager.RefreshConnectionToken(sToken);
        }

        public void Clear(bool isClose=true)
        {
            tokenConnectionManager.Clear(isClose);
        }
    }
}
