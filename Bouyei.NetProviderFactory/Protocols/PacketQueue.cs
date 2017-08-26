using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetProviderFactory.Protocols
{
    internal class PacketQueue
    {
        CycQueue<byte> bucket = null;

        public PacketQueue(int maxCount)
        {
            bucket = new CycQueue<byte>(maxCount);
        }

        public bool SetBlock(byte[] buffer, int offset, int size)
        {
            if (bucket.Capacity - bucket.Length < size)
                return false;

            for (int i = 0; i < size; ++i)
            {
                bool rt = bucket.EnQueue(buffer[i + offset]);
                if (rt == false) return false;
            }
            return true;
        }

        public Packet GetBlock()
        {
            int head = -1;
            for (int i = bucket.Head; i < bucket.Tail; ++i)
            {
                if (bucket.Array[i] != Packet.packageFlag)
                {
                    bucket.DeQueue();
                }
                else
                {
                    head = i;
                    break;
                }
            }
            if (head == -1) return null;

            //数据包长度
            int pkgLength = CheckCompletePackageLength(bucket.Array, head);
            if (pkgLength == 0) return null;

            Packet pkg = new Packet();
            bool rt = pkg.DeocdeFromBytes(bucket.Array, head, pkgLength);
          
            for(int i = head; i < pkgLength; ++i)
            {
                bucket.DeQueue();
            }

            return pkg;
        }

        private unsafe int CheckCompletePackageLength(byte[] buff,int offset)
        {
            fixed (byte* src = &(buff[offset+1]))
            {
                int i = offset;
                int c = 0;
                while (i <bucket.Length)
                {
                    ++c;
                    ++i;
                    if (*(src+i) == Packet.packageFlag)
                    {
                        return c+2;//加上标志位
                    }
                }
                c = 0;
                return c;
            }
        }
    }
}
