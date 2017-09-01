using System;
using System.Collections.Generic;
using System.Threading;

namespace Bouyei.NetProviderFactory
{
    internal class SocketTokenManager<T>
    {
        private Queue<T> collection = null;
        private int used = 0;

        /// <summary>
        /// 栈个数
        /// </summary>
        public int Count
        {
            get { return collection.Count; }
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="capacity"></param>
        public SocketTokenManager(int capacity = 32)
        {
            collection = new Queue<T>(capacity);
        }

        /// <summary>
        /// 取出
        /// </summary>
        /// <returns></returns>
        public T Get()
        {
            while (Interlocked.CompareExchange(ref used, 0, 1) != 0)
            {
                Thread.Sleep(1);
            }
            try
            {
                if (collection.Count > 0) return collection.Dequeue();
                else return default(T);
            }
            finally
            {
                Interlocked.Exchange(ref used, 0);
            }
        }

        /// <summary>
        /// 放回
        /// </summary>
        /// <param name="item"></param>
        public void Set(T item)
        {
            while (Interlocked.CompareExchange(ref used, 0, 1) != 0)
            {
                Thread.Sleep(1);
            }
            try
            {
                collection.Enqueue(item);
            }
            finally
            {
                Interlocked.Exchange(ref used, 0);
            }
        }

        /// <summary>
        /// 清除队列
        /// </summary>
        public void Clear()
        {
            while (Interlocked.CompareExchange(ref used, 0, 1) != 0)
            {
                Thread.Sleep(1);
            }
            try
            {
                collection.Clear();
            }
            finally
            {
                Interlocked.Exchange(ref used, 0);
            }
        }

        /// <summary>
        /// 如果队列为空则等待
        /// </summary>
        /// <param name="isWaitingFor">如果为false则在指定时间超时后返回</param>
        /// <returns></returns>
        public T GetEmptyWait(bool isWaitingFor=true)
        {
            int retry = 1;

            while (true)
            {
                var tArgs = Get();
                if (tArgs != null) return tArgs;
                if (isWaitingFor == false)
                {
                    if (retry > 16) break;
                    ++retry;
                }
                Thread.Sleep(1000 * retry);
            }
            return default(T);
        }
    }
}