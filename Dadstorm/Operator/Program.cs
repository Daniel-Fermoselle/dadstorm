using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Dadstorm
{
    delegate bool processTuple(Tuple t);

    public class OperatorServer
    {
        //Different names?
        /// <summary>
        /// Name of the service that will be published.
        /// </summary>
        private const string OPSERVICE_NAME = "OperatorServices";

        /// <summary>
        /// Main publishes OperatorServices at a certain url
        /// </summary>
        /// <param name="args">Port where the service will be published.</param>
        static void Main(string[] args)
        {
            int port = Int32.Parse(args[0]);

            //Creating the channel
            TcpChannel serverChannel = new TcpChannel(port);
            ChannelServices.RegisterChannel(serverChannel, false);

            // Expose an object form RepServices for remote calls.
            OperatorServices opServices = new OperatorServices();
            RemotingServices.Marshal(opServices, OPSERVICE_NAME, typeof(OperatorServices));

            System.Console.WriteLine("Press <enter> to terminate server...");
            System.Console.ReadLine();
        }
    }

    internal class OperatorServices : MarshalByRefObject, RepServices
    {
        /// <summary>
        /// Number of threads to be created.
        /// </summary>
        private const int THREAD_NUMBER = 2;
        /// <summary>
        /// Size of the circular buffers.
        /// </summary>
        private const int BUFFER_SIZE = 10;

        /// <summary>
        /// Information concerning the Replica.
        /// </summary>
        private RepInfo repInfo;

        /// <summary>
        /// Thread pool.
        /// </summary>
        private ThrPool threadPool;

        /// <summary>
        /// String with the current status of the replica.
        /// </summary>
        private string repStatus = "Unitialized";

        /// <summary>
        /// String with the current status of the replica.
        /// </summary>
        private int repInterval = 0;

        /// <summary>
        /// Bool that signs when the process will crash.
        /// </summary>
        private bool repCrash = false;

        /// <summary>
        /// Dictionaru with methods to process tuples.
        /// </summary>
        private Dictionary<string, processTuple> processors;

        /// <summary>
        /// OperatorServices constructor.
        /// </summary>
        public OperatorServices()
        {
            threadPool = new ThrPool(THREAD_NUMBER, BUFFER_SIZE, this);
            processors = new Dictionary<string, processTuple>();
        }

        /// <summary>
        /// RepInfo setter and getter.
        /// </summary>
        public RepInfo RepInfo
        {
            get { return repInfo; }
            set { repInfo = value; }
        }

        /// <summary>
        /// RepInterval setter and getter.
        /// </summary>
        public int RepInterval
        {
            get { return repInterval; }
            set { repInterval = value; }
        }

        /// <summary>
        /// RepStatus setter and getter.
        /// </summary>
        public string RepStatus
        {
            get { return repStatus; }

            set { repStatus = value; }
        }

        public bool RepCrash
        {
            get { return repCrash; }

            set { repCrash = value; }
        }

        /// <summary>
        /// Response to a Start command.
        /// Sending read tuples from files to the buffer.
        /// </summary>
        public void Start()
        {
            this.repStatus = "Starting";
            List<Tuple> tupleList = new List<Tuple>();

            //TODO Here comes the parser 

            foreach(Tuple t in tupleList)
            {
                threadPool.AssyncInvoke(t);
            }

            //TODO Add entries to the dictionary.
        }

        /// <summary>
        /// Response to a Interval command.
        /// Operator stops for a certain amount of time.
        /// </summary>
        /// <param name="x_ms">Operator will stop for x_ms miliseconds.</param>
        public void Interval(string x_ms)
        {
            this.repStatus = "on Interval";
            int interval = Int32.Parse(x_ms);
            this.repInterval = interval;
        }

        /// <summary>
        /// Response to a Status command.
        /// </summary>
        public void Status()
        {
            Console.Write("This replica is " + this.repStatus + "...");
        }

        /// <summary>
        /// Response to a Crash command.
        /// </summary>
        public void Crash()
        {
            this.repCrash = true;
        }

        /// <summary>
        /// Response to a Freeze command.
        /// </summary>
        public void Freeze()
        {

        }

        /// <summary>
        /// Response to a Unfreeze command.
        /// </summary>
        public void Unfreeze()
        {

        }

        /// <summary>
        /// Response to a ShutDown command.
        /// </summary>
        public void ShutDown() { }

        /// <summary>
        /// Response to a Populate command.
        /// </summary>
        public void Populate(RepInfo info)
        {
            this.repInfo = info;
        }

        /// <summary>
        /// Unique operator processing.
        /// </summary>
        public bool processTuple(Tuple t)
        {
            //TODO Do I have to initialize value?
            processTuple value;
            processors.TryGetValue(this.repInfo.Operator_spec, out value);
            return value(t);
        }

        /// <summary>
        /// Unique operator processing.
        /// </summary>
        public bool Unique(Tuple t)
        {
            foreach(Tuple tuple in threadPool.TuplesRead)
            {
                if (threadPool.TuplesRead.Contains(t))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Count operator processing.
        /// </summary>
        public bool Count(Tuple t)
        {
            return true;
        }

        /// <summary>
        /// Dup operator processing.
        /// </summary>
        public bool Dup(Tuple t)
        {
            return true;
        }

        /// <summary>
        /// Filter operator processing.
        /// </summary>
        public bool Filter(Tuple t)
        {
            return true;
        }

        /// <summary>
        /// Custom operator processing.
        /// </summary>
        public bool Custom(Tuple t)
        {
            return true;
        }


    }

    /// <summary>
    /// Tuple is collections of elements.
    /// </summary>
    [Serializable]
    class Tuple
    {
        /// <summary>
        /// List with the elements of the Tuple.
        /// </summary>
        private List<string> elements;

        /// <summary>
        /// Tuple Contructor.
        /// </summary>
        public Tuple(List<string> tuple)
        {
            this.elements = tuple;
        }

        /// <summary>
        /// Tuple ToString.
        /// </summary>
        public string toString()
        {
            string result = "";
            foreach (string s in elements)
            {
                result = result + "," + s;
            }
            return result.Remove(0, 1);
        }
    }
}