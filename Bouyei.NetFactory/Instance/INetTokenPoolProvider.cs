using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetFactory
{
    using Pools;
    public interface INetTokenPoolProvider
    {
        int ConnectionTimeout { get; set; }
        int Count { get; }
        void TimerEnable(bool isContinue);
        NetConnectionToken GetTopToken();
        void InsertToken(NetConnectionToken ncToken);
        bool RemoveToken(NetConnectionToken ncToken,bool isClose=true);
        void Clear(bool isClose = true);
        NetConnectionToken GetTokenById(int Id);
        NetConnectionToken GetTokenBySocketToken(SocketToken sToken);
        bool RefreshExpireToken(SocketToken sToken);
    }
}
