using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
        private const string OPSERVICE_NAME = "op";

        static OperatorServices opServices;

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
            opServices = new OperatorServices();
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
        private const int THREAD_NUMBER = 3;
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
        /// Bool that signs when the process will crash.
        /// </summary>
        private bool repFreeze = false;

        /// <summary>
        /// Bool that signs when the process will crash.
        /// </summary>
        private IList<Tuple> tupleProcessed;

        /// <summary>
        /// Dictionaru with methods to process tuples.
        /// </summary>
        private Dictionary<string, processTuple> processors;

        /// <summary>
        /// OperatorServices constructor.
        /// </summary>
        public OperatorServices()
        {
            tupleProcessed = new List<Tuple>();
            threadPool = new ThrPool(THREAD_NUMBER, BUFFER_SIZE, this);
            processors = new Dictionary<string, processTuple>();
            processors.Add("UNIQ", Unique);
            processors.Add("COUNT", Count);
            processors.Add("DUP", Dup);
            processors.Add("FILTER", Filter);
            processors.Add("CUSTOM", Custom);
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

        /// <summary>
        /// RepCrash setter and getter.
        /// </summary>
        public bool RepCrash
        {
            get { return repCrash; }

            set { repCrash = value; }
        }

        /// <summary>
        /// RepFreeze setter and getter.
        /// </summary>
        public bool RepFreeze
        {
            get { return repFreeze; }
            set { repFreeze = value; }
        }

        internal ThrPool ThreadPool
        {
            get { return threadPool; }
            set { threadPool = value; }
        }

        internal IList<Tuple> TupleProcessed
        {
            get { return tupleProcessed;}
            set{ tupleProcessed = value;}
        }

        /// <summary>
        /// Response to a Start command.
        /// Sending read tuples from files to the buffer.
        /// </summary>
        public void Start()
        {
            this.repStatus = "starting";
            IList<Tuple> tupleList = new List<Tuple>();

            foreach(string s in repInfo.Input)
            {
                if (s.Contains(".data"))
                {
                    OpParser parser = new OpParser(s);
                    tupleList = parser.processFile();
                    foreach (Tuple t in tupleList)
                    {
                        threadPool.AssyncInvoke(t);
                    }
                    this.repStatus = "working";
                }
            }
        }

        /// <summary>
        /// Response to a Interval command.
        /// Operator stops for a certain amount of time.
        /// </summary>
        /// <param name="x_ms">Operator will stop for x_ms miliseconds.</param>
        public void Interval(string x_ms)
        {
            this.repStatus = "on interval";
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
            Environment.Exit(0);
        }

        /// <summary>
        /// Response to a Freeze command.
        /// </summary>
        public void Freeze()
        {
            this.repStatus = "frozen";
            repFreeze = true;
        }

        /// <summary>
        /// Response to a Unfreeze command.
        /// </summary>
        public void Unfreeze()
        {
            this.repStatus = "working";
            repFreeze = false;
        }

        /// <summary>
        /// Response to a Populate command.
        /// </summary>
        public void Populate(RepInfo info)
        {
            this.repInfo = info;
            this.repStatus = "Initialized";
        }

        /// <summary>
        /// Unique operator processing.
        /// </summary>
        /// <param name="t">Tuple to be processed.</param>
        public bool processTuple(Tuple t)
        {
            processTuple value;
            processors.TryGetValue(this.repInfo.Operator_spec, out value);
            return value(t);
        }

        /// <summary>
        /// Unique operator processing.
        /// </summary>
        /// <param name="t">Tuple to be processed.</param>
        public bool Unique(Tuple t)
        {
            foreach(Tuple tuple in threadPool.TuplesRead)
            {
                int param = Int32.Parse((string) repInfo.Operator_param[0]);
                if (t.Index(param).Equals(tuple.Index(param)))
                {
                    return false;
                }
            }

            tupleProcessed.Add(t);
            return true;
        }

        /// <summary>
        /// Count operator processing.
        /// </summary>
        /// <param name="t">Tuple to be processed.</param>
        public bool Count(Tuple t)
        {
            IList<string> countTuple = new List<string>();
            countTuple.Add(threadPool.TuplesRead.Count.ToString());
            Tuple tuple = new Tuple(countTuple);
            tupleProcessed.Add(tuple);

            return true;
        }

        /// <summary>
        /// Dup operator processing.
        /// </summary>
        /// <param name="t">Tuple to be processed.</param>
        public bool Dup(Tuple t)
        {
            tupleProcessed.Add(t);
            return true;
        }

        /// <summary>
        /// Filter operator processing.
        /// </summary>
        /// <param name="t">Tuple to be processed.</param>
        public bool Filter(Tuple t)
        {
            tupleProcessed = new List<Tuple>();
            string param = (string) repInfo.Operator_param[2];
            string condition = (string) repInfo.Operator_param[1];
            string value = t.Index(Int32.Parse((string) repInfo.Operator_param[0])-1);
            if (condition.Equals("="))
            {
                tupleProcessed.Add(t);
                return param.Equals(value);
            }
            else
                return false;
        }

        /// <summary>
        /// Custom operator processing.
        /// </summary>
        /// <param name="t">Tuple to be processed.</param>
        public bool Custom(Tuple t)
        {
            string path = (string)repInfo.Operator_param[0];
            byte[] code = File.ReadAllBytes(path);
            string className = (string)repInfo.Operator_param[1];
            Assembly assembly = Assembly.Load(code);
            // Walk through each type in the assembly looking for our class
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsClass == true)
                {
                    if (type.FullName.EndsWith("." + className))
                    {
                        // create an instance of the object
                        object ClassObj = Activator.CreateInstance(type);

                        // Dynamically Invoke the method
                        List<string> l = new List<string>();
                        foreach(string element in t.Elements)
                        {
                            l.Add(element);
                        }
                        object[] args = new object[] { l };
                        object resultObject = type.InvokeMember("CustomOperation",
                          BindingFlags.Default | BindingFlags.InvokeMethod,
                               null,
                               ClassObj,
                               args);
                        IList<IList<string>> result = (IList<IList<string>>)resultObject;
                        Console.WriteLine("Map call result was: ");
                        foreach (IList<string> tuple in result)
                        {
                            tupleProcessed.Add(new Tuple(tuple));
                            Console.Write("tuple: ");
                            foreach (string s in tuple)
                                Console.Write(s + " ,");
                            Console.WriteLine();
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Gets PMServices to send the log.
        /// </summary>
        public PMServices getPMServices()
        {
            //Getting the PMServices object 
            PMServices obj = (PMServices)Activator.GetObject(typeof(PMServices),repInfo.PmsUrl);
            return obj;
        }

        /// <summary>
        /// Sends tuples to the next Operator in the channel.
        /// </summary>
        /// <param name="t">Tuple to be sent.</param>
        public void SendTuple(Tuple t)
        {
            ArrayList urls = new ArrayList();
            foreach (string opx in repInfo.SendInfoUrls.Keys) {
                repInfo.SendInfoUrls.TryGetValue(opx, out urls);
            }
            if (urls.Count == 0)
            {
                foreach(Tuple tuple in tupleProcessed)
                {
                    Console.WriteLine(tuple.toString());
                }
                return;
            }
            //TODO Hashing
            string url;
            url = (string) urls[0];
            //Getting the OperatorServices object 
            OperatorServices obj = (OperatorServices)Activator.GetObject(typeof(OperatorServices), url);
            obj.ThreadPool.AssyncInvoke(t);
        }

        /// <summary>
        /// Notifies PM with a message.
        /// </summary>
        /// <param name="msg">Message sent to PM.</param>
        public void NotifyPM(string msg)
        {
            if (repInfo.LoggingLvl.Equals("full"))
            {
                PMServices service = getPMServices();
                service.SendToLog(msg);
            }
        }

        public void ping(string msg)
        {
            Console.WriteLine(msg);
        }

        /// <summary>
        /// Doesn't let service expire.
        /// </summary>
        public override object InitializeLifetimeService()
        {
            return null;
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
        private IList<string> elements;

        /// <summary>
        /// Tuple Contructor.
        /// </summary>
        public Tuple(IList<string> tuple)
        {
            this.elements = tuple;
        }

        public IList<string> Elements
        {
            get { return elements; }
            set { elements = value;}
        }

        /// <summary>
        /// Index of an element.
        /// </summary>
        public string Index(int i)
        {
            return this.elements[i];
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