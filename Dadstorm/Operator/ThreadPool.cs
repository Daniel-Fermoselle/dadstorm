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
        private IList<Tuple> tuplesRead;

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
            pool[i++].Start();
            pool[i] = new Thread(new ThreadStart(StateRefresh));
            pool[i++].Start();
        }

        /// <summary>
        /// TuplesRead setter and getter.
        /// </summary>
        internal IList<Tuple> TuplesRead
        {
            get { return tuplesRead; }
            set { tuplesRead = value; }
        }

        /// <summary>
        /// Pool setter and getter.
        /// </summary>
        public Thread[] Pool
        {
            get { return pool; }
            set { pool = value; }
        }

        /// <summary>
        /// AssyncInvoke inserts tuple in bufferRead.
        /// </summary>
        /// <param name="t">Tuple that will be added</param>
        public void AssyncInvoke(Tuple t)
        {
            //Mark tuple as read
            tuplesRead.Add(t);

            //Add tuple to bufferRead
            bufferRead.Produce(t);

            Console.WriteLine("Submitted tuple " + t.toString() + "to buffer of Read Tuples");
        }

        /// <summary>
        /// ConsumeRead gets tuple from bufferRead and processes it.
        /// </summary>
        public void ConsumeRead()
        {
            while (true)
            {
                //Checks availability to process a tuple
                while (operatorService.RepInterval != 0 || operatorService.RepFreeze) { }
                if (operatorService.RepCrash)
                {
                    Console.WriteLine("HELP ME I AM GOING TO CRASH!! NOOOOO!!");
                    return;
                }

                //Get tuple from bufferRead
                Tuple t = bufferRead.Consume();

                Console.WriteLine("Consumed tuple " + t.toString() + "from buffer of Read Tuples");

                //Processing tuple
                if (operatorService.processTuple(t))
                {
                    foreach (Tuple tuple in operatorService.TupleProcessed)
                    {
                        bufferProcessed.Produce(tuple);
                        operatorService.NotifyPM("<" + tuple.toString() + ">");
                    }
                    Console.WriteLine("Processed tuple " + t.toString() + "and accepted.");
                }
                else
                {
                    Console.WriteLine("Processed tuple " + t.toString() + "and rejected.");
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

                Console.WriteLine("Consumed tuple " + t.toString() + "from buffer of Processed Tuples");

                //Sends tuple to the next Operator
                //WARNING
                operatorService.SendTuple(t);

                if (operatorService.RepCrash)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// State Refresh checks if there is any commands that affect the threads.
        /// </summary>
        public void StateRefresh()
        {
            while (true)
            {
                //Check if Interval action was requested
                if (operatorService.RepInterval != 0)
                {
                    Console.WriteLine("Sleeping for: " + operatorService.RepInterval.ToString());
                    Thread.Sleep(operatorService.RepInterval);
                    operatorService.RepInterval = 0;
                    operatorService.RepStatus = "working";
                }
                if (operatorService.RepFreeze)
                {
                    Thread.Sleep(100);
                }
                if (operatorService.RepCrash)
                {
                    Console.WriteLine("HELP ME I AM GOING TO CRASH!! NOOOOO!!");
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