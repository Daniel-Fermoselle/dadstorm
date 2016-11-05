using System.Threading;

namespace Dadstorm

{
    public class CircularBuffer<T>
    {
        private T[] buffer;
        private int size;
        private int busy;
        private int InsCur;
        private int remCur;

        public CircularBuffer(int size)
        {
            buffer = new T[size];
            this.size = size;
            busy = 0;
            InsCur = 0;
            remCur = 0;
        }

        public void Produce(T o)
        {
            lock (this)
            {
                while (busy == size)
                {
                    Monitor.Wait(this);
                }
                buffer[InsCur] = o;
                InsCur = ++InsCur % size;
                busy++;
                if (busy == 1)
                {
                    Monitor.Pulse(this);
                }
            }
        }

        public T Consume()
        {
            T o;
            lock (this)
            {
                while (busy == 0)
                {
                    Monitor.Wait(this);
                }
                o = buffer[remCur];
                buffer[remCur] = default(T);
                remCur = ++remCur % size;
                busy--;
                if (busy == size - 1)
                {
                    Monitor.Pulse(this);
                }
            }
            return o;
        }

        public string toString()
        {
            string s = "";
            lock (this)
            {
                for (int i = 0; i < size; i++)
                {
                    s += buffer[i].ToString() + " ,";
                }
            }
            return s;
        }
    }
}
