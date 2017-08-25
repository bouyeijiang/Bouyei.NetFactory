using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetProviderFactory.Protocols
{
    public class Package
    {
        #region variable
        /// <summary>
        /// 报头信息
        /// </summary>
        public PackageHeader pHeader { get; set; }
        /// <summary>
        /// 包携带的数据内容
        /// </summary>
        public byte[] pPayload { get; set; }

        #endregion

        #region method
        /// <summary>
        /// 编码
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 解码
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        internal bool DeocdeFromBytes(byte[] buffer, int offset, int size)
        {
            byte[] dst = Restore(buffer, offset, size);

            uint plen= dst.ToUInt32(6);

            if (plen >= size - 11)
                throw new Exception("content buffer overflow...");

            if (pHeader == null)
                pHeader = new PackageHeader();

            pHeader.packageFlag = dst[0];
            pHeader.packageId = dst.ToUInt16(1);
            pHeader.packageType = dst[3];

            if (pHeader.packageAttribute == null)
                pHeader.packageAttribute = new PackageAttribute();

            pHeader.packageAttribute.packageCount = dst.ToUInt16(4);
            pHeader.packageAttribute.payloadLength = plen;// dst.ToUInt32(6);
         
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

            int plen = buffer.Length - 2;

            byte[] rBuffer = new byte[buffer.Length + flagCnt];
            rBuffer[0] = buffer[0];//起始标识位

            fixed (byte* dst = &(rBuffer[1]), src = &(buffer[1]))
            {
                byte* _dst = dst;
                byte* _src = src;

                //消息头和消息体
                while (plen >= 0)
                {
                    if (*(_src) == flag)
                    {
                        *(_dst) = *(_src);
                        *(_dst + 1) = 0x01;
                        _dst += 2;
                    }
                    else
                    {
                        *_dst = *_src;
                        _dst += 1;
                    }
                    _src += 1;
                    plen -= 1;
                }

                //结束标志位
                *_dst = *_src;

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

            rBuffer[0] = flag; //标志位
            int pLen = size - 2;//去掉标志位后的长度

            fixed (byte* dst = &(rBuffer[1]), src = &(buffer[offset + 1]))
            {
                byte* _src = src;
                byte* _dst = dst;

                //开始标志位
                //*(dst+0) = *(src + offset);

                //消息头和消息体
                while (pLen >= 0)
                {
                    if (*(_src) == flag)
                    {
                        if ((pLen - 1) >= 0
                            && *(_src + 1) == 0x01)
                        {
                            *(_dst) = flag;
                            _src += 2;
                            _dst += 1;
                            pLen -= 2;

                            continue;
                        }
                    }
                    *(_dst) = *(_src);
                    _src += 1;
                    _dst += 1;
                    pLen -= 1;
                }
                //结束标志位
                *(_dst) = *(_src);
                return rBuffer;
            }
        }

        /// <summary>
        /// 检查要转义的标志数
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        private unsafe int CheckEscapeFlagBitCount(byte[] buffer, byte flag)
        {
            int len = buffer.Length - 2;//去头尾标识位
            int c = 0;
            fixed (byte* src = &(buffer[1]))
            {
                byte* _src = src;
                do
                {
                    if (*_src == flag)
                    {
                        ++c;
                    }
                    _src += 1;
                    --len;
                } while (len > 0);
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
        private unsafe int CheckRestoreFlagBitCount(byte[] buffer, byte flag, int offset, int size)
        {
            int len = size - 2;
            int i = offset + 1, c = 0;
            fixed (byte* src = &(buffer[offset + 1]))
            {
                byte* _src = src;
                do
                {
                    if (*_src == flag)
                    {
                        if ((len - 1) >= 0 && *(_src + 1) == 0x01)
                        {
                            ++c;
                            _src += 2;
                            len -= 2;
                            continue;
                        }
                    }
                    _src += 1;
                    --len;
                } while (len > 0);
            }
            return c;
        }
        #endregion
    }
}
