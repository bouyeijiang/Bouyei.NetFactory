using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetProviderFactory
{
    using Protocols;

    public  class NetPacketProvider:INetPacketProvider
    {
        private PacketQueue packetQueue = null;
        private object lockObject = new object();

        public NetPacketProvider(int capacity)
        {
            if (capacity < 128) capacity = 128;
            packetQueue = new PacketQueue(capacity);
        }

        public static NetPacketProvider CreateProvider(int capacity)
        {
            return new NetPacketProvider(capacity);
        }

        public bool SetBlock(byte[] bufffer,int offset,int size)
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
