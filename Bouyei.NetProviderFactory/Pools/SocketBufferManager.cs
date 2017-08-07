using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace Bouyei.NetProviderFactory
{
    internal class SocketBufferManager
    {
        int totalSize;
        int curIndex;
        int blockSize;
        int used = 0;
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
            while (Interlocked.CompareExchange(ref used, 0, 1) != 0)
            {
                Thread.Sleep(1);
            }
            try
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
            finally
            {
                Interlocked.Exchange(ref used, 0);
            }
        }

        /// <summary>
        /// 写入缓冲区
        /// </summary>
        /// <param name="agrs"></param>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="cnt"></param>
        /// <returns></returns>
        public bool WriteBuffer(SocketAsyncEventArgs agrs, byte[] data,int offset,int cnt)
        {
            while (Interlocked.CompareExchange(ref used, 0, 1) != 0)
            {
                Thread.Sleep(1);
            }
            try
            {
                //超出缓冲区则不写入
                if (agrs.Offset + data.Length > this.buffer.Length)
                {
                    return false;
                }

                Buffer.BlockCopy(data, offset, this.buffer, agrs.Offset, cnt);

                agrs.SetBuffer(this.buffer, agrs.Offset, data.Length);

                return true;
            }
            finally
            {
                Interlocked.Exchange(ref used, 0);
            }
        }

        /// <summary>
        /// 释放缓冲区
        /// </summary>
        /// <param name="args"></param>
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            while (Interlocked.CompareExchange(ref used, 0, 1) != 0)
            {
                Thread.Sleep(1);
            }
            try
            {
                this.freeBufferIndexPool.Push(args.Offset);
                args.SetBuffer(null, 0, 0);
            }
            finally
            {
                Interlocked.Exchange(ref used, 0);
            }
        }
    }
}