using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetFactory.Base
{
   public static class ByteConvert
    {
        public static byte[] ToBytes(this UInt16 value)
        {
            return new byte[]{
                     (byte)(value >> 8),
                     (byte)value
                };
        }

        public static byte[] ToBytes(this UInt32 value)
        {
            return new byte[] {
                 (byte)(value >> 24),
                 (byte)(value >> 16),
                 (byte)(value >> 8),
                 (byte)value
                 };
        }

        public static byte[] ToBytes(this UInt64 value)
        {
            return new byte[]{
                  (byte)(value >> 56),
                  (byte)(value >> 48),
                  (byte)(value >> 40),
                  (byte)(value >> 32),
                  (byte)(value >> 24),
                  (byte)(value >> 16),
                  (byte)(value >> 8),
                  (byte)value
             };
        }

        public static UInt16 ToUInt16(this byte[] array,int offset=0)
        {
            return (UInt16)((array[offset] << 8) | array[offset+1]);
        }

        public static UInt32 ToUInt32(this byte[] array,int offset=0)
        {
            return (((UInt32)array[offset] << 24)
               | ((UInt32)array[offset+1] << 16)
               | ((UInt32)array[offset + 2] << 8)
               | array[offset + 3]);
        }

        public static UInt64 ToUInt64(this byte[] array, int offset = 0)
        {
            return (((UInt64)array[offset] << 56)
                 | ((UInt64)array[offset + 1] << 48)
                 | ((UInt64)array[offset + 2] << 40)
                 | ((UInt64)array[offset + 3] << 32)
                 | ((UInt64)array[offset + 4] << 24)
                 | ((UInt64)array[offset + 5] << 16)
                 | ((UInt64)array[offset + 6] << 8)
                 | array[offset + 7]);
        }

        internal static unsafe void BulkCopy(byte* src, byte* dest, int count)
        {
            if (count >= 16)
            {
                do
                {
                    *((ulong*)dest) = *((ulong*)src);
                    *((ulong*)(dest + 8)) = *((ulong*)(src + 8));
                    dest += 16;
                    src += 16;
                }
                while ((count -= 16) >= 16);
            }
            if (count > 0)
            {
                if ((count & 8) != 0)
                {
                    *((ulong*)dest) = *((ulong*)src);
                    dest += 8;
                    src += 8;
                }
                if ((count & 4) != 0)
                {
                    *((uint*)dest) = *((uint*)src);
                    dest += 4;
                    src += 4;
                }
                if ((count & 2) != 0)
                {
                    *((ushort*)dest) = *((ushort*)src);
                    dest += 2;
                    src += 2;
                }
                if ((count & 1) != 0)
                {
                    dest[0] = src[0];
                    //dest++;
                    //src++;
                }
            }

        }
    }
}
