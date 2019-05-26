/*-------------------------------------------------------------
 *project:Bouyei.NetFactory.Base
 *   auth: bouyei
 *   date: 2017/10/2 14:32:59
 *contact: 453840293@qq.com
 *profile: www.openthinking.cn
---------------------------------------------------------------*/
using System;
using System.Threading;

namespace Bouyei.NetFactory.Base
{
    internal class LockWait:IDisposable
    {
        private LockParam lParam = null;
        public LockWait(ref LockParam lParam)
        {
            this.lParam = lParam;
            while (Interlocked.CompareExchange(ref lParam.Signal, 1, 0) == 1)
            {
                Thread.Sleep(lParam.SleepInterval);
            }
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref lParam.Signal, 0);
        }
    }

    internal class LockParam
    {
        internal int Signal = 0;

        internal int SleepInterval = 1;//ms
    }
}
