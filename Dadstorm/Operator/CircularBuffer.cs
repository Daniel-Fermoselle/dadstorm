using System.Threading;

namespace Dadstorm

{
    /// <summary>
    /// CircularBuffer is a thread-safe circular buffer (with limited capacity).
    /// </summary>
    public class CircularBuffer<T>
    {
        /// <summary>
        /// Circular Buffer.
        /// </summary>
        private T[] buffer;
        /// <summary>
        /// Circular buffer size.
        /// </summary>
        private int size;
        /// <summary>
        /// Number of occupied positions in the buffer.
        /// </summary>
        private int busy;
        /// <summary>
        /// Cursor for next buffer insertion.
        /// </summary>
        private int InsCur;
        /// <summary>
        /// Cursor for next buffer removal.
        /// </summary>
        private int remCur;

        /// <summary>
        /// CircularBuffer constructor.
        /// </summary>
        /// <param name="size">Size of the circular buffer.</param>
        public CircularBuffer(int size)
        {
            buffer = new T[size];
            this.size = size;
            busy = 0;
            InsCur = 0;
            remCur = 0;
        }

        /// <summary>
        /// Inserts an item in the buffer
        /// </summary>
        /// <param name="o">The inserted object.</param>
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

        /// <summary>
        /// Removes the next element from the circular buffer.
        /// </summary>
        /// <returns>The removed object.</returns>
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

        /// <summary>
        /// Returns a comma-separated list of all the elements in the buffer.
        /// </summary>
        /// <returns>A comma-separated list of all the elements in the buffer.</returns>
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
