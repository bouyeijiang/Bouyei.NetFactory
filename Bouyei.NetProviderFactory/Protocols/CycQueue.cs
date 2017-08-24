using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetProviderFactory.Protocols
{
    internal class CycQueue<T> : IDisposable
    {
        T[] bucket = null;
        int capacity = 4;
        int head = 0;
        int tail = 0;
        int length = 0;

        public int Length { get { return length; } }

        public CycQueue(int capacity)
        {
            if (capacity < 4) capacity = 4;
            this.capacity = capacity;
            bucket = new T[capacity];
        }

        public T this[int index]
        {
            get
            {
                if (IsEmpty()
                    || (index < head || index > tail))
                    return default(T);
                else
                    return bucket[index];
            }
            set
            {
                if (IsEmpty() == false
                    && (index >= head && index <= tail))
                    bucket[index] = value;
            }
        }

        public void Clear()
        {
            head = tail = 0;
            length = 0;
        }

        public bool IsEmpty ()
        {
            //return tail == head;
            return length == 0;
        }

        public bool IsFull()
        {
           // return (tail + 1) % capacity == head;
            return length == capacity;
        }

        public bool EnQueue(T value)
        {
            if (IsFull()) return false;
            bucket[tail] = value;
            tail = (tail+1) % capacity;
            ++length;
            return true;
        }

        public T DeQueue()
        {
            if (IsEmpty()) return default(T);
            T v = bucket[head];
            head = (head+1) % capacity;
            --length;
            return v;
        }

        public void Dispose()
        {
            if (bucket != null)
                bucket = null;
        }
    }
}
