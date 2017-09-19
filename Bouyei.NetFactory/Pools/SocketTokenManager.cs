using System;
using System.Collections.Generic;
using System.Threading;

namespace Bouyei.NetFactory
{
    internal class SocketTokenManager<T>
    {
        private Queue<T> collection = null;
        private int used = 0;
        private int capacity = 4;
 
        public int Count
        {
            get { return collection.Count; }
        }


        public int Capacity { get { return capacity; } }
        
        public SocketTokenManager(int capacity = 32)
        {
            this.capacity = capacity;
            collection = new Queue<T>(capacity);
        }

      
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

        public void ClearToCloseToken()
        {
            while (Interlocked.CompareExchange(ref used, 0, 1) != 0)
            {
                Thread.Sleep(1);
            }
            try
            {
                while (collection.Count > 0)
                {
                    var token = collection.Dequeue() as SocketToken;
                    if (token != null) token.Close();
                }
            }
            finally
            {
                Interlocked.Exchange(ref used, 0);
            }
        }

        public void ClearToCloseArgs()
        {
            while (Interlocked.CompareExchange(ref used, 0, 1) != 0)
            {
                Thread.Sleep(1);
            }
            try
            {
                while (collection.Count > 0)
                {
                    var token = collection.Dequeue() as System.Net.Sockets.SocketAsyncEventArgs;
                    if (token != null) token.Dispose();
                }
            }
            finally
            {
                Interlocked.Exchange(ref used, 0);
            }
        }
 
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