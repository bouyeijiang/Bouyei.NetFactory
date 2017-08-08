using System;
using System.Collections.Generic;
using System.Threading;

namespace Bouyei.NetProviderFactory
{
    internal class SocketTokenManager<T>
    {
        private Queue<T> stack = null;
        private int used = 0;

        /// <summary>
        /// 栈个数
        /// </summary>
        public int Count
        {
            get { return stack.Count; }
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="capacity"></param>
        public SocketTokenManager(int capacity = 32)
        {
            stack = new Queue<T>(capacity);
        }

        /// <summary>
        /// 出栈
        /// </summary>
        /// <returns></returns>
        public T Pop()
        {
            while (Interlocked.CompareExchange(ref used, 0, 1) != 0)
            {
                Thread.Sleep(1);
            }
            try
            {
                if (stack.Count > 0) return stack.Dequeue();
                else return default(T);
            }
            finally
            {
                Interlocked.Exchange(ref used, 0);
            }
        }

        /// <summary>
        /// 入栈
        /// </summary>
        /// <param name="item"></param>
        public void Push(T item)
        {
            while (Interlocked.CompareExchange(ref used, 0, 1) != 0)
            {
                Thread.Sleep(1);
            }
            try
            {
                stack.Enqueue(item);
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
                stack.Clear();
            }
            finally
            {
                Interlocked.Exchange(ref used, 0);
            }
        }
    }
}