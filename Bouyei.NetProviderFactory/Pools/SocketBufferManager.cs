using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Bouyei.NetProviderFactory
{
    internal class SocketBufferManager
    {
        int totalSize;
        int curIndex;
        int blockSize;
        byte[] buffer;
        Stack<int> freeBufferIndexPool;

        /// <summary>
        /// 缓冲区管理构造
        /// </summary>
        /// <param name="maxCounts"></param>
        /// <param name="blockSize"></param>
        public SocketBufferManager(int maxCounts, int blockSize)
        {
            this.blockSize = blockSize;
            this.curIndex = 0;
            totalSize = maxCounts * blockSize;
            buffer = new byte[totalSize];
            freeBufferIndexPool = new Stack<int>(maxCounts);
        }

        public void Clear()
        {
            freeBufferIndexPool.Clear();
        }

        /// <summary>
        /// 设置缓冲区
        /// </summary>
        /// <param name="agrs"></param>
        /// <returns></returns>
        public bool SetBuffer(SocketAsyncEventArgs agrs)
        {
            if (freeBufferIndexPool.Count > 0)
            {
                agrs.SetBuffer(this.buffer,
                    this.freeBufferIndexPool.Pop(),
                    blockSize);
            }
            else
            {
                if ((totalSize - blockSize) < curIndex) return false;

                agrs.SetBuffer(this.buffer, this.curIndex, this.blockSize);

                this.curIndex += this.blockSize;
            }
            return true;
        }

        /// <summary>
        /// 写入缓冲区
        /// </summary>
        /// <param name="data"></param>
        /// <param name="agrs"></param>
        /// <returns></returns>
        public bool WriteBuffer(byte[] data,SocketAsyncEventArgs agrs)
        {
            //超出缓冲区则不写入
            if(agrs.Offset+data.Length>this.buffer.Length)
            {
                return false;
            }
            
            Buffer.BlockCopy(data, 0, this.buffer, agrs.Offset, data.Length);

            agrs.SetBuffer(this.buffer, agrs.Offset, data.Length);

            return true;
        }

        /// <summary>
        /// 释放缓冲区
        /// </summary>
        /// <param name="args"></param>
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            this.freeBufferIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}