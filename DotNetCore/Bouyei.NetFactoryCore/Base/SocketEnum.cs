﻿/*-------------------------------------------------------------
 *   auth: bouyei
 *   date: 2017/7/29 13:42:10
 *contact: 453840293@qq.com
 *profile: www.openthinking.cn
 *   guid: 428ffb26-77c1-40b3-ba14-146ae4e11a5a
---------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetFactoryCore
{
    public enum NetProviderType:UInt16
    {
        Tcp = 0,
        Udp = 1,
        WebSocket = 2
    }

    public enum ChannelProviderType:UInt16
    {
        Async = 0,
        AsyncWait = 1,
        Sync = 2
    }
}
