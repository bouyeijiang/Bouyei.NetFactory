﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetFactory
{
    using Protocols;

    public  class NetPacketProvider:INetPacketProvider
    {
        private PacketQueue packetQueue = null;
        private object lockObject = new object();

        public NetPacketProvider(int capacity)
        {
            if (capacity < 128) capacity = 128;
            capacity += 1;
            packetQueue = new PacketQueue(capacity);
        }

        public static NetPacketProvider CreateProvider(int capacity)
        {
            return new NetPacketProvider(capacity);
        }

        public int Count
        {
            get
            {
                return packetQueue.Count;
            }
        }

        public bool SetBlocks(byte[] bufffer,int offset,int size)
        {
          
            lock (lockObject)
            {
                return packetQueue.SetBlock(bufffer, offset, size);
            }
        }

        public List<Packet> GetBlocks()
        {
            lock (lockObject)
            {
                return packetQueue.GetBlocks();
            }
        }
    }
}
