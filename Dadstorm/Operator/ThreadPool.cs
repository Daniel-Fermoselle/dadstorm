using System;
using System.Threading;
using System.Collections.Generic;

namespace Dadstorm
{

    class ThrPool
    {
        /// <summary>
        /// CircularBuffer with read tuples.
        /// </summary>
        private CircularBuffer<Tuple> bufferRead;
        /// <summary>
        /// CircularBuffer with processed tuples.
        /// </summary>
        private CircularBuffer<Tuple> bufferProcessed;
        /// <summary>
        /// Thread pool.
        /// </summary>
        private Thread[] pool;
        /// <summary>
        /// Operator that owns the Thread Pool.
        /// </summary>
        private OperatorServices operatorService;
        /// <summary>
        /// List of tuples read.
        /// </summary>
        private List<Tuple> tuplesRead;

        /// <summary>
        /// CircularBuffer constructor.
        /// </summary>
        /// <param name="thrNum">Number of threads to be created.</param>
        /// <param name="size">Size of the circular buffers.</param>
        /// <param name="operatorService">Operator that owns the Thread Pool.</param>
        public ThrPool(int thrNum, int bufSize, OperatorServices operatorService)
        {       
            //Initialize attributes    
            bufferRead = new CircularBuffer<Tuple>(bufSize);
            bufferProcessed = new CircularBuffer<Tuple>(bufSize);
            pool = new Thread[thrNum];
            this.operatorService = operatorService;
            tuplesRead = new List<Tuple>();

            //Start threads
            int i = 0;
            pool[i] = new Thread(new ThreadStart(ConsumeRead));
            pool[i++].Start();
            pool[i] = new Thread(new ThreadStart(ConsumeProcessed));
            pool[i].Start();
        }

        /// <summary>
        /// TuplesRead setter and getter.
        /// </summary>
        internal List<Tuple> TuplesRead
        {
            get { return tuplesRead; }
            set { tuplesRead = value; }
        }

        /// <summary>
        /// AssyncInvoke inserts tuple in bufferRead.
        /// </summary>
        /// <param name="tuple">Tuple that will be added</param>
        public void AssyncInvoke(Tuple tuple)
        {
            //Add tuple to bufferRead
            bufferRead.Produce(tuple);
            tuplesRead.Add(tuple);

            Console.WriteLine("Submitted tuple " + tuple.ToString() + "to buffer of Read Tuples");
        }

        /// <summary>
        /// ConsumeRead gets tuple from bufferRead and processes it.
        /// </summary>
        public void ConsumeRead()
        {
            while (true)
            {
                //Get tuple from bufferRead
                Tuple t = bufferRead.Consume();

                if (operatorService.processTuple(t))
                {
                    bufferProcessed.Produce(t);
                }

                if (operatorService.RepInterval != 0)
                {
                    Thread.Sleep(operatorService.RepInterval);
                    operatorService.RepInterval = 0;
                }
                if (operatorService.RepCrash)
                {
                    return;
                }

                
            }
        }

        /// <summary>
        /// ConsumeRead gets tuple from bufferProcessed and sends it to the next operator.
        /// </summary>
        public void ConsumeProcessed()
        {
            while (true)
            {
                //Gets tuple from bufferProcessed
                Tuple t = bufferRead.Consume();

                //TODO Send tuple to the next Operator

                if (operatorService.RepCrash)
                {
                    return;
                }

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