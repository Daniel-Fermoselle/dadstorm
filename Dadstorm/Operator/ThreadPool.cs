using System;
using System.Threading;
using System.Collections.Generic;

namespace Dadstorm
{

    class ThrPool
    {
        private CircularBuffer<Tuple> buf;
        private Thread[] pool;
        public ThrPool(int thrNum, int bufSize)
        {
            buf = new CircularBuffer<Tuple>(bufSize);
            pool = new Thread[thrNum];
            for (int i = 0; i < thrNum; i++)
            {
                pool[i] = new Thread(new ThreadStart(consomeExec));
                pool[i].Start();
            }
        }

        public void AssyncInvoke(Tuple tuple)
        {
            buf.Produce(tuple);

            Console.WriteLine("Submitted tuple " + tuple.ToString());
        }

        public void consomeExec()
        {
            while (true)
            {
                Tuple t = buf.Consume();
                Console.WriteLine(t.ToString());
            }
        }
    }

    /*
    class Test
    {
        public static void Main()
        {

            int x = 0;
            ThrPool tpool = new ThrPool(5, 10);
            Tuple t;
            List<string> s;
            for (int i = 0; i < 5; i++)
            {
                s = new List<string>();
                x++;
                s.Add(x.ToString()); s.Add((x + 1).ToString()); s.Add((x + 2).ToString());
                t = new Tuple(s);
                tpool.AssyncInvoke(t);
                s = new List<string>();
                x++;
                s.Add(x.ToString()); s.Add((x + 1).ToString()); s.Add((x + 2).ToString());
                t = new Tuple(s);
                tpool.AssyncInvoke(t);
            }
            Console.ReadLine();
        }
    }
    */
}