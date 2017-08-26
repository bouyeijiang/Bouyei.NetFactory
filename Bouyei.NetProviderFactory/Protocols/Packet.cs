using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetProviderFactory.Protocols
{
    public class Packet
    {
        #region variable
        /// <summary>
        /// 包标志位
        /// </summary>
        public static byte packageFlag { get;private set; } = 0xfe;

        public static byte subFlag { get; private set; } = 0xfd;

        /// <summary>
        /// 报头信息
        /// </summary>
        public PacketHeader pHeader { get; set; }
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
            pHeader.packetAttribute.payloadLength = (UInt32)plen;

            byte[] buffer = new byte[11 + plen];
            buffer[0] = packageFlag;
            buffer[1] = (byte)(pHeader.packetId >> 8);
            buffer[2] = (byte)pHeader.packetId;
            buffer[3] = pHeader.packetType;
            buffer[4] = (byte)(pHeader.packetAttribute.packetCount >> 8);
            buffer[5] = (byte)pHeader.packetAttribute.packetCount;
            buffer[6] = (byte)(pHeader.packetAttribute.payloadLength >> 24);
            buffer[7] = (byte)(pHeader.packetAttribute.payloadLength >> 16);
            buffer[8] = (byte)(pHeader.packetAttribute.payloadLength >> 8);
            buffer[9] = (byte)(pHeader.packetAttribute.payloadLength);

            Buffer.BlockCopy(pPayload, 0, buffer, 10, plen);
            buffer[buffer.Length - 1] = packageFlag;

            return Escape(buffer);
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
            //还原转义并且过滤标志位
            byte[] dst = Restore(buffer, offset, size);

            uint plen= dst.ToUInt32(5);

            if (plen >= size - 11)
                return false;

            if (pHeader == null)
                pHeader = new PacketHeader();

            pHeader.packetId = dst.ToUInt16(0);
            pHeader.packetType = dst[2];

            if (pHeader.packetAttribute == null)
                pHeader.packetAttribute = new PacketAttribute();

            pHeader.packetAttribute.packetCount = dst.ToUInt16(3);
            pHeader.packetAttribute.payloadLength = plen;// dst.ToUInt32(5);
         
            pPayload = new byte[pHeader.packetAttribute.payloadLength];
            Buffer.BlockCopy(dst, 9, pPayload, 0, pPayload.Length);

            return true;
        }

        /// <summary>
        /// 转义
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        private unsafe byte[] Escape(byte[] buffer)
        {
           var tCnt = CheckEscapeFlagBitCount(buffer);
            if ((tCnt.pkgFlag + tCnt.subFlag) == 0) return buffer;

            int plen = buffer.Length - 2;

            byte[] rBuffer = new byte[buffer.Length + tCnt.pkgFlag + tCnt.subFlag];
            rBuffer[0] = buffer[0];//起始标识位

            fixed (byte* dst = &(rBuffer[1]), src = &(buffer[1]))
            {
                byte* _dst = dst;
                byte* _src = src;

                //消息头和消息体
                while (plen > 0)
                {
                    if (*_src== packageFlag)
                    {
                        *_dst = subFlag;
                        *(_dst + 1) = 0x01;
                        _dst += 2;
                    }
                    else if(*_src==subFlag)
                    {
                        *_dst = subFlag;
                        *(_dst + 1) = 0x02;
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
            var tCnt = CheckRestoreFlagBitCount(buffer,  offset, size);
            if ((tCnt.subCnt+tCnt.pkgCnt) == 0)
            {
                if (buffer.Length == size) return buffer;
                else
                {
                    byte[] buff = new byte[size];
                    Buffer.BlockCopy(buff, offset, buff, 0, size);
                    return buff;
                }
            }

            int pLen = size - 2;//去掉标志位后的长度
            byte[] rBuffer = new byte[pLen - tCnt.pkgCnt - tCnt.subCnt];

            fixed (byte* dst = rBuffer, src = &(buffer[offset + 1]))
            {
                byte* _src = src;
                byte* _dst = dst;

                //开始标志位
                //*(dst+0) = *(src + offset);

                //消息头和消息体
                while (pLen >= 0)
                {
                    if (*(_src) == subFlag && *(_src + 1) == 0x01)
                    {

                        *(_dst) = packageFlag;
                        _src += 2;
                        _dst += 1;
                        pLen -= 2;
                    }
                    else if (*(_src) == subFlag && *(_src + 1) == 0x02)
                    {
                        *(_dst) = subFlag;
                        _src += 2;
                        _dst += 1;
                        pLen -= 2;
                    }
                    else
                    {
                        *(_dst) = *(_src);
                        _src += 1;
                        _dst += 1;
                        pLen -= 1;
                    }
                }
  
                return rBuffer;
            }
        }

        /// <summary>
        /// 检查要转义的标志数
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        private unsafe (int pkgFlag,int subFlag) CheckEscapeFlagBitCount(byte[] buffer)
        {
            int len = buffer.Length - 2;//去头尾标识位
            int pktCnt = 0, subCnt = 0;
            fixed (byte* src = &(buffer[1]))
            {
                byte* _src = src;
                do
                {
                    if (*_src == packageFlag)
                    {
                        ++pktCnt;
                    }
                    else if (*_src == subFlag)
                    {
                        ++subCnt;
                    }
                    _src += 1;
                    --len;
                } while (len > 0);
            }
            return (pktCnt, subCnt);
        }

        /// <summary>
        /// 检查被转义的标志数
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="flag"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private unsafe (int pkgCnt,int subCnt) CheckRestoreFlagBitCount(byte[] buffer, int offset, int size)
        {
            int len = size - 2;
            int i = offset + 1, pkgCnt = 0, subCnt = 0;
            fixed (byte* src = &(buffer[offset + 1]))
            {
                byte* _src = src;
                do
                {
                    if (*_src == subFlag && *(_src + 1) == 0x01)
                    {
                        ++pkgCnt;
                        _src += 2;
                        len -= 2;
                    }
                    else if (*_src == subFlag && *(_src + 1) == 0x02)
                    {
                        ++subCnt;
                        _src += 2;
                        len -= 2;
                    }
                    else
                    {
                        _src += 1;
                        --len;
                    }
                } while (len > 0);
            }
            return (pkgCnt, subCnt);
        }
        #endregion
    }
}
