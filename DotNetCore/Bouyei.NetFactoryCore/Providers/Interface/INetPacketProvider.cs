﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetFactoryCore
{
    using Protocols.PacketProto;

    public interface INetPacketProvider
    {
        int Count { get; }
        bool SetBlocks(byte[] buffer, int offset, int size);

        List<Packet> GetBlocks();
        void Clear();
    }
}
