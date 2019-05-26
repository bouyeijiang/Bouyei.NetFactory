using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetFactoryCore
{
    using Protocols.PacketProto;
    using Base;

    public  class NetPacketProvider:INetPacketProvider
    {
        private PacketQueue packetQueue = null;
        LockParam lockParam = null;

        public NetPacketProvider(int capacity)
        {
            if (capacity < 128) capacity = 128;
            capacity += 1;
            packetQueue = new PacketQueue(capacity);
            lockParam = new LockParam();
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
            using (LockWait lwait = new LockWait(ref lockParam))
            {
                return packetQueue.SetBlock(bufffer, offset, size);
            }
        }

        public List<Packet> GetBlocks()
        {
            using (LockWait lwait = new LockWait(ref lockParam))
            {
                return packetQueue.GetBlocks();
            }
        }

        public void Clear()
        {
            using (LockWait lwait = new LockWait(ref lockParam))
            {
                  packetQueue.Clear();
            }
        }
    }
}
