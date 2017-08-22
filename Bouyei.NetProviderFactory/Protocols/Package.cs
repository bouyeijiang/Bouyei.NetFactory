using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetProviderFactory.Protocols
{
    public class Package
    {
        /// <summary>
        /// 报头信息
        /// </summary>
        public PackageHeader pHeader { get; set; }
        /// <summary>
        /// 包携带的数据内容
        /// </summary>
        public byte[] pPayload { get; set; }

        internal byte[] EncodeToBytes()
        {
            int plen = pPayload.Length;
            pHeader.packageAttribute.payloadLength = (UInt32)plen;

            byte[] buffer = new byte[11 + plen];
            buffer[0] = pHeader.packageFlag;
            buffer[1] = (byte)(pHeader.packageId >> 8);
            buffer[2] = (byte)pHeader.packageId;
            buffer[3] = pHeader.packageType;
            buffer[4] = (byte)(pHeader.packageAttribute.packageCount >> 8);
            buffer[5] = (byte)pHeader.packageAttribute.packageCount;
            buffer[6] = (byte)(pHeader.packageAttribute.payloadLength >> 24);
            buffer[7] = (byte)(pHeader.packageAttribute.payloadLength >> 16);
            buffer[8] = (byte)(pHeader.packageAttribute.payloadLength >> 8);
            buffer[9] = (byte)(pHeader.packageAttribute.payloadLength);

            Buffer.BlockCopy(pPayload, 0, buffer, 10, plen);
            buffer[buffer.Length - 1] = pHeader.packageFlag;

            return Escape(buffer, pHeader.packageFlag);
        }

        internal bool DeocdeFromBytes(byte[] buffer, int offset, int size)
        {
            byte[] dst = Restore(buffer, offset, size);

            if (pHeader == null)
                pHeader = new PackageHeader();

            pHeader.packageFlag = dst[0];
            pHeader.packageId = dst.ToUInt16(1);
            pHeader.packageType = dst[3];

            if (pHeader.packageAttribute == null)
                pHeader.packageAttribute = new PackageAttribute();

            pHeader.packageAttribute.packageCount = dst.ToUInt16(4);
            pHeader.packageAttribute.payloadLength = dst.ToUInt32(6);

            pPayload = new byte[pHeader.packageAttribute.payloadLength];
            Buffer.BlockCopy(dst, 10, pPayload, 0, pPayload.Length);

            return true;
        }

        /// <summary>
        /// 转义
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        private unsafe byte[] Escape(byte[] buffer, byte flag)
        {
            int flagCnt = CheckEscapeFlagBitCount(buffer, flag);
            if (flagCnt == 0) return buffer;

            int i = 1, j = 1;
            int blen = buffer.Length - 2;
            int len = buffer.Length;

            byte[] rBuffer = new byte[buffer.Length + flagCnt];
            fixed (byte* dst = rBuffer, src = buffer)
            {
                //起始标识位
                *(dst+0) = buffer[0];

                //消息头和消息体
                while (i <=blen)
                {
                    if (*(src + i) == flag)
                    {
                        *(dst + j) = *(src + i);
                        *(dst + j + 1) = 0x01;
                        j += 2;
                    }
                    else
                    {
                        *(dst + j) = *(src + i);
                        ++j;
                    }
                    ++i;
                }

                //结束标志位
                *(dst + j) = *(src + i);

                return rBuffer;
            }
        }

        /// <summary>
        /// 转义还原
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private unsafe byte[] Restore(byte[] buffer, int offset, int size)
        {
            byte flag = buffer[offset];

            int flagCnt = CheckRestoreFlagBitCount(buffer, flag, offset, size);
            if (flagCnt == 0)
            {
                if (buffer.Length == size) return buffer;
                else
                {
                    byte[] buff = new byte[size];
                    Buffer.BlockCopy(buff, offset, buff, 0, size);
                    return buff;
                }
            }
            byte[] rBuffer = new byte[size - flagCnt];

            int i = offset + 1, j = 1;
            int srcLen = size - 2;

            fixed (byte *dst = rBuffer,src=buffer)
            {
                //开始标志位
                *(dst+0) = *(src + offset);

                //消息头和消息体
                while (i <= srcLen)
                {
                    if (*(src + i) == flag)
                    {
                        if (i + 1 < srcLen && *(src+i+1)==0x01)
                        {
                            *(dst + j) = flag;
                            i += 2;
                            ++j;
                            continue;
                        }
                    }
                    *(dst + j) = *(src + i);
                    ++i;
                    ++j;
                }
                //结束标志位
                *(dst + j) = *(src + i);
                return rBuffer;
            }
        }

        /// <summary>
        /// 检查要转义的标志数
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        private unsafe int CheckEscapeFlagBitCount(byte[] buffer,byte flag)
        {
            int len = buffer.Length - 2;//去头尾标识位
            int i = 1, c = 0;
            fixed (byte* src = buffer)
            {
                while (i < len)
                {
                    if (*(src + i) == flag)
                    {
                        ++c;
                    }
                    ++i;
                }
            }
            return c;
        }

        /// <summary>
        /// 检查被转义的标志数
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="flag"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private unsafe int CheckRestoreFlagBitCount(byte[] buffer, byte flag,int offset, int size)
        {
            int len = size - 2;
            int i = offset + 1, c = 0;
            fixed (byte* src = buffer)
            {
                while (i < len)
                {
                    if (*(src + i) == flag)
                    {
                        if (i + 1 < len && *(src + i + 1) == 0x01)
                        {
                            ++c;
                            i += 2;
                            continue;
                        }
                    }
                    ++i;
                }
            }
            return c;
        }
    }
}
