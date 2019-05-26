using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetFactory.Base
{
    internal class CycQueue<T> : IDisposable
    {
        T[] bucket = null;
        int capacity = 4;
        int head = 0;
        int tail = 0;

        public int Length { get { return GetLength(); } }

        public int Capacity { get { return capacity; } }

        public T[] Array { get { return bucket; } }

        public CycQueue(int capacity)
        {
            if (capacity < 4) capacity = 4;
            this.capacity = capacity;
            bucket = new T[capacity];
        }

        public void Clear()
        {
            head = tail = 0;
        }

        private int GetLength()
        {
            return (tail - head + capacity) % capacity;
        }

        public bool IsEmpty ()
        {
            return tail == head;
            //return length == 0;
        }

        public bool IsFull()
        {
            return (tail + 1) % capacity == head;
            //return length == capacity;
        }

        public bool EnQueue(T value)
        {
            if (IsFull()) return false;
            bucket[tail] = value;
            tail = (tail+1) % capacity;
            return true;
        }

        public T DeQueue()
        {
            if (IsEmpty()) return default(T);
            T v = bucket[head];
            head = (head+1) % capacity;
            return v;
        }

        public T[] DeRange(int size)
        {
            if (size > Length) return null;
            T[] array = new T[size];
            int index = 0;
            while (size > 0)
            {
                if (IsEmpty()) return null;

                array[index] = bucket[head];
                head = (head + 1) % capacity;
                --size;
                ++index;
            }
            return array;
        }

        public void Clear(int size)
        {
            int len = size <= Length ? size : Length;

            while (len > 0)
            {
                if (IsEmpty()) break;

                head = (head + 1) % capacity;
                --len;
            }
        }

        public int DeSearchIndex(T value,int offset)
        {
            if (offset > Length) return -1;

            if (offset > 0)
            {
                head = (head + offset) % capacity;
            }
            while (Length > 0)
            {
                if (IsEmpty()) return -1;
                if (value.Equals(bucket[head])) return head;

                head = (head + 1) % capacity;
            }
            return -1;
        }

        public int PeekIndex(T value, int offset)
        {
            if (offset > Length) return -1;

            int _h = (head + offset) % capacity;
            if (bucket[_h].Equals(value)) return _h;
            return -1;
        }

        public void Dispose()
        {
            if (bucket != null)
                bucket = null;
        }
    }
}
