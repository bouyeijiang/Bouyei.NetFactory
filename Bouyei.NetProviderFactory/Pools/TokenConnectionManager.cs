using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Bouyei.NetProviderFactory.Pools
{
    internal class TokenConnectionManager
    {
        LinkedList<NetConnectionToken> list = null;
        int period = 60;//s
        Timer timeoutThreading = null;
        private object lockObject = new object();

        public int ConnectionTimeout { get; set; } = 60;//s

        public TokenConnectionManager(int period)
        {
            if (period < 2) this.period = 2;
            else this.period = period;
            int _period = (period * 1000) >> 1;
            list = new LinkedList<NetConnectionToken>();
            timeoutThreading = new Timer(new TimerCallback(timeoutHandler), null, _period, _period);
        }

        public void TimeoutChange(int period)
        {
            this.period = period;
            if (period < 2) this.period = 2;
            
            int _p = (period * 1000) >> 1;
            timeoutThreading.Change(_p, _p);
        }

        public void AddToken(NetConnectionToken ncToken)
        {
            lock (lockObject)
            {
                list.AddLast(ncToken);
            }
        }

        public bool RemoveToken(NetConnectionToken ncToken)
        {
            lock (lockObject)
            {
                return list.Remove(ncToken);
            }
        }

        public bool RemoveToken(SocketToken sToken)
        {
            lock (lockObject)
            {
                var item = list.Where(x => x.Token.CompareTo(sToken) == 0).FirstOrDefault();
                if (item != null)
                {
                   return list.Remove(item);
                }
            }
            return false;
        }

        public NetConnectionToken GetTokenById(int Id)
        {
            lock (lockObject)
            {
                return list.Where(x => x.Token.TokenId == Id).FirstOrDefault();
            }
        }

        private void timeoutHandler(object obj)
        {
            lock (lockObject)
            {
                foreach (var item in list)
                {
                    if (item.Verification == false 
                        || DateTime.Now.Subtract(item.ConnectionTime).TotalSeconds >= ConnectionTimeout)
                    {
                        item.Token.Close();
                        list.Remove(item);
                    }
                }
            }
        }
    }

    public class NetConnectionToken
    {
        public SocketToken Token { get; set; }

        public DateTime ConnectionTime { get; set; } = DateTime.Now;

        public bool Verification { get; set; } = true;
    }
}
