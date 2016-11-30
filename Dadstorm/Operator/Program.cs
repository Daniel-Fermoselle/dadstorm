using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Timers;

namespace Dadstorm
{
    delegate IList<Tuple> processTuple(Tuple t);
    delegate string sendTuplePolicy(ArrayList urls, Tuple t);//added Tuple t but when not need will receive it not using it


    public class OperatorServer
    {

        static OperatorServices opServices;

        /// <summary>
        /// Main publishes OperatorServices at a certain url
        /// </summary>
        /// <param name="args">Port where the service will be published.</param>
        static void Main(string[] args)
        {
            int port = Int32.Parse(args[0]);
            string opName = args[1];
            //Creating the channel
            TcpChannel serverChannel = new TcpChannel(port);
            ChannelServices.RegisterChannel(serverChannel, false);

            // Expose an object form RepServices for remote calls.
            opServices = new OperatorServices();
            RemotingServices.Marshal(opServices, opName, typeof(OperatorServices));

            Console.WriteLine("Eu sou um op e estou no porto " + port);

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
        /// Comments to debug on/off.
        /// </summary>
        private const bool DEBUG_COMMENTS = false;

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
        /// String with the current interval of the replica.
        /// </summary>
        private int repInterval = 0;

        /// <summary>
        /// Bool that signs when the process will crash.
        /// </summary>
        private bool repCrash = false;

        /// <summary>
        /// Bool that signs when the process will freeze.
        /// </summary>
        private bool repFreeze = false;

        /// <summary>
        /// Comments to debug on/off.
        /// </summary>
        private bool comments;

        /// <summary>
        /// Dictionary with methods to process tuples.
        /// </summary>
        private Dictionary<string, processTuple> processors;

        /// <summary>
        /// Dictionary with sending policies.
        /// </summary>
        private Dictionary<string, sendTuplePolicy> policies;

        /// <summary>
        /// Delegate to assync call
        /// </summary>
        public delegate void AsyncDelegate(string x_ms);

        /// <summary>
        /// ArrayList with toBeAcked tuples for the at least once semantic and exactly once.
        /// </summary>
        private ArrayList toBeAcked;

        /// <summary>
        /// ArrayList with to receive ack from tuples for the at least once semantic and exactly once.
        /// </summary>
        private ArrayList toReceiveAck;

        /// <summary>
        /// ArrayList with the timers of resending tuples for the at least once semantic and exactly once.
        /// </summary>
        private ArrayList timerAck;

        /// <summary>
        /// ArrayList of Tuple2TupleProcessed for the exactly once allowing us to know if the tuple was already processed and its result.
        /// </summary>
        private ArrayList tupleToTupleProcessed;

        /// <summary>
        /// Timeout of an ack in ms for the at least once semantic and exactly once.
        /// </summary>
        private const int TIMEOUT = 4;


        /// <summary>
        /// OperatorServices constructor.
        /// </summary>
        public OperatorServices()
        {
            this.comments = DEBUG_COMMENTS;
            threadPool = new ThrPool(THREAD_NUMBER, BUFFER_SIZE, this);
            processors = new Dictionary<string, processTuple>();
            policies = new Dictionary<string, sendTuplePolicy>();
            toReceiveAck = new ArrayList();
            toBeAcked = new ArrayList();
            timerAck = new ArrayList();
            tupleToTupleProcessed = new ArrayList();
            processors.Add("UNIQ", Unique);
            processors.Add("COUNT", Count);
            processors.Add("DUP", Dup);
            processors.Add("FILTER", Filter);
            processors.Add("CUSTOM", Custom);
            policies.Add("primary", Primary);
            policies.Add("random", CRandom);
            policies.Add("hashing", Hashing);
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

        /// <summary>
        /// ThreadPool setter and getter.
        /// </summary>
        public ThrPool ThreadPool
        {
            get { return threadPool; }
            set { threadPool = value; }
        }

        /// <summary>
        /// Comments setter and getter.
        /// </summary>
        public bool Comments
        {
            get { return comments; }
            set { comments = value; }
        }

        /// <summary>
        /// ToBeAcked setter and getter.
        /// </summary>
        public ArrayList ToBeAcked
        {
            get { return toBeAcked; }
            set { toBeAcked = value; }
        }

        /// <summary>
        /// ToReceiveAck setter and getter.
        /// </summary>
        public ArrayList ToReceiveAck
        {
            get { return toReceiveAck; }
            set { toReceiveAck = value; }
        }

        /// <summary>
        /// ToReceiveAck setter and getter.
        /// </summary>
        public ArrayList TimerAck
        {
            get { return timerAck; }
            set { timerAck = value; }
        }

        /// <summary>
        /// ToReceiveAck setter and getter.
        /// </summary>
        public ArrayList TupleToTupleProcessed
        {
            get { return tupleToTupleProcessed; }
            set { tupleToTupleProcessed = value; }
        }

        /// <summary>
        /// Response to a Start command.
        /// Sending read tuples from files to the buffer.
        /// </summary>
        public void Start(RepInfo info)
        {
            this.repInfo = info;
            this.repStatus = "Initialized";
            this.repStatus = "starting";
            IList<Tuple> tupleList = new List<Tuple>();
            IList<Tuple> subTupleList = new List<Tuple>();

            foreach (string s in repInfo.Input)
            {
                if (s.Contains(".dat"))
                {
                    OpParser parser = new OpParser(s);
                    tupleList = parser.processFile();
                    if (info.SiblingsUrls.Count == 1)
                    {
                        //In this case all tuples are read by the replica
                        subTupleList = tupleList;
                    }
                    else
                    {
                        //In this case only a part of the tuples are read by the replica
                        int index = info.SiblingsUrls.IndexOf(info.MyUrl);
                        int tupleListSize = tupleList.Count;
                        int parts = tupleListSize / (info.SiblingsUrls.Count);
                        if (index == info.SiblingsUrls.Count-1)
                        {
                            for (int i = index * parts; i < tupleListSize; i++)
                            {
                                subTupleList.Add(tupleList[i]);
                            }
                        }
                        else
                        {
                            for (int i = index * parts; i < (index + 1) * parts; i++)
                            {
                                subTupleList.Add(tupleList[i]);
                            }
                        }
                    }

                    foreach (Tuple t in subTupleList)
                    {
                        threadPool.AssyncInvoke(t);//Tuples from file dont need to be acked
                    }
                }
            }
            this.repStatus = "working";

        }

        /// <summary>
        /// Response to a Interval command.
        /// Operator stops for a certain amount of time.
        /// </summary>
        /// <param name="x_ms">Operator will stop for x_ms miliseconds.</param>
        public void Interval(string x_ms)
        {
            int interval = Int32.Parse(x_ms);
            this.repInterval = interval;
        }

        /// <summary>
        /// Response to a Status command.
        /// </summary>
        public void Status()
        {
            Console.WriteLine("This replica is " + this.repStatus + "...");
        }

        /// <summary>
        /// Response to a Crash command.
        /// </summary>
        public void Crash()
        {
            //if(RepInfo.Routing.Equals("primary") && RepInfo.SiblingsUrls.Count>1 && RepInfo.SiblingsUrls.IndexOf(RepInfo.MyUrl)==0 )
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
        /// Method to call the right operation on the tuple received.
        /// </summary>
        /// <param name="t">Tuple to be processed.</param>
        public IList<Tuple> processTuple(Tuple t)
        {
            processTuple value;
            bool notInList = true;//is it needed to update the TupleToTupleProcessed array 
            IList<Tuple> result = new List<Tuple>();
            if (RepInfo.Semantics.Equals("exactly-once"))
            {
                foreach (Tuple2TupleProcessed t2t in TupleToTupleProcessed.ToArray())
                {
                    if (t.toString().Equals(t2t.Pre.toString()))
                    {
                        Console.WriteLine("Tuple already processed gonna use the previous result: " + t.toString());
                        //result = t2t.Pos;
                        result = null;
                        notInList = false;
                    }
                }
                if (notInList)
                {
                    processors.TryGetValue(this.repInfo.Operator_spec, out value);
                    result = value(t);
                    Tuple2TupleProcessed temp = new Tuple2TupleProcessed(t, result);
                    TupleToTupleProcessed.Add(temp);
                }
            }
            else
            {
                processors.TryGetValue(this.repInfo.Operator_spec, out value);
                result = value(t);
            }
            if (result != null)
            {
                //Give ack to previous rep
                foreach (AckTuple t2 in ToBeAcked.ToArray())
                {
                    if (t2.AckT.toString().Equals(t.toString()))
                    {
                        if(Comments)Console.WriteLine("Vou ser removido das listas porque ja fui processado : " + t.toString());
                        ackTuple(t, t2.UrlToAck);
                        this.removeToBeAck(t2);
                        break;
                    }
                }
            }
            return result;
        }

        public void ackTuple(Tuple t, String url)//AckTuples if needed
        {
            if (!RepInfo.Semantics.Equals("at-most-once"))
            {
                Console.WriteLine("TupleAcked: " + t.toString());
                OperatorServices obj = (OperatorServices)Activator.GetObject(typeof(OperatorServices), url);
                obj.receivedAck(t);
            }
        }

        /// <summary>
        /// Unique operator processing.
        /// </summary>
        /// <param name="t">Tuple to be processed.</param>
        public IList<Tuple> Unique(Tuple t)
        {
            IList<Tuple> tupleProcessed = new List<Tuple>();

            foreach (Tuple tuple in threadPool.TuplesRead)
            {
                int param = Int32.Parse((string) repInfo.Operator_param[0]) - 1;
                if(comments) Console.WriteLine("Comparing " + t.Index(param) + " ===== " + tuple.Index(param));
                if (t.Index(param).Equals(tuple.Index(param)))
                {
                    return null;
                }
            }

            tupleProcessed.Add(t);
            return tupleProcessed;
        }

        /// <summary>
        /// Count operator processing.
        /// </summary>
        /// <param name="t">Tuple to be processed.</param>
        public IList<Tuple> Count(Tuple t)
        {
            IList<Tuple> tupleProcessed = new List<Tuple>();
            IList<string> countTuple = new List<string>();

            //WARNING THE FOLLOWING CONTENT IS FOR PRO MLG PLAYERS ONLY
            countTuple.Add((threadPool.TuplesRead.Count+1).ToString());
            Tuple tuple = new Tuple(countTuple);
            tupleProcessed.Add(tuple);

            return tupleProcessed;
        }

        /// <summary>
        /// Dup operator processing.
        /// </summary>
        /// <param name="t">Tuple to be processed.</param>
        public IList<Tuple> Dup(Tuple t)
        {
            IList<Tuple> tupleProcessed = new List<Tuple>();

            tupleProcessed.Add(t);
            return tupleProcessed;
        }

        /// <summary>
        /// Filter operator processing.
        /// </summary>
        /// <param name="t">Tuple to be processed.</param>
        public IList<Tuple> Filter(Tuple t)
        {
            IList<Tuple> tupleProcessed = new List<Tuple>();
            string param = (string) repInfo.Operator_param[2];
            string condition = (string) repInfo.Operator_param[1];
            string value = t.Index(Int32.Parse((string) repInfo.Operator_param[0])-1);
            int condResult = String.Compare(value, param);//returns 0 if value equals param, -1 if value > param , 1 if value < param
            if (condition.Equals("="))
            {
                tupleProcessed.Add(t);
                if (condResult==0)
                {
                    return tupleProcessed;
                }
                else { return null; }
            }

            else if (condition.Equals("<"))
            {
                tupleProcessed.Add(t);
                if (condResult == 1)
                {
                    return tupleProcessed;
                }
                else { return null; }

            }

            else if (condition.Equals(">"))
            {
                tupleProcessed.Add(t);
                if (condResult == -1)
                {
                    return tupleProcessed;
                }
                else { return null; }
            }

            else
                return null;
        }

        /// <summary>
        /// Custom operator processing.
        /// </summary>
        /// <param name="t">Tuple to be processed.</param>
        public IList<Tuple> Custom(Tuple t)
        {
            IList<Tuple> tupleProcessed = new List<Tuple>();
            string path = (string)repInfo.Operator_param[0];
            byte[] code = File.ReadAllBytes(path);
            string className = (string)repInfo.Operator_param[1];
            string methodName = (string)repInfo.Operator_param[2];
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
                        object resultObject = type.InvokeMember(methodName,
                          BindingFlags.Default | BindingFlags.InvokeMethod,
                               null,
                               ClassObj,
                               args);
                        IList<IList<string>> result = (IList<IList<string>>)resultObject;
                        if(comments) Console.WriteLine("Map call result was: ");
                        foreach (IList<string> tuple in result)
                        {
                            tupleProcessed.Add(new Tuple(tuple));
                            if (comments)
                            {
                                Console.Write("tuple: ");
                                foreach (string s in tuple)
                                    Console.Write(s + " ,");
                                Console.WriteLine();
                            }
                        }
                        return tupleProcessed;
                    }
                }
            }
            return null;
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
            
            foreach(TimerTuple tt in TimerAck.ToArray())
            {
                tt.Time++;
                if (tt.Time == TIMEOUT)
                {
                    Console.WriteLine("Resending tuple: " + tt.AckT.toString());
                    tt.Time = 0;
                    ActuallySendTuple(tt.AckT);
                }
            }
            ActuallySendTuple(t);

        }

        public void ActuallySendTuple(Tuple t)
        {
            bool last = true;
            ArrayList urls;


            foreach (string opx in repInfo.SendInfoUrls.Keys)
            {
                repInfo.SendInfoUrls.TryGetValue(opx, out urls);
                if (urls.Count >= 1)
                {
                    last = false;
                    sendTuplePolicy value;
                    policies.TryGetValue(this.repInfo.Next_routing, out value);

                    if (value == this.Hashing)
                    {
                        if (comments) Console.WriteLine("Estou no Hashing");
                        OperatorServices obj = (OperatorServices)Activator.GetObject(typeof(OperatorServices), Hashing(urls, t));//TODO change to value in order to have all
                        if (comments) obj.ping("HashPING!");
                        if (RepInfo.Semantics.Equals("exactly-once"))
                        {
                            foreach (Tuple2TupleProcessed t2t in obj.TupleToTupleProcessed.ToArray())
                            {
                                if (t.toString().Equals(t2t.Pre.toString()))
                                {
                                    Console.WriteLine("Tuple already processed by the next rep so not gonna send it again: " + t.toString());
                                    return;
                                }
                            }
                        }
                        AddTupleToReceiveAck(t);//Save tuple to receive ack
                        obj.AddTupleToBeAcked(t, RepInfo.MyUrl);//Send MyUrl to be acked
                        obj.AddTupleToBuffer(t);

                    }

                    else
                    {
                        //Getting the OperatorServices object 
                        OperatorServices obj = (OperatorServices)Activator.GetObject(typeof(OperatorServices), value(urls, t));//the tuple is sent because of the delegate being equal to every policy
                        if (comments) obj.ping("PING!");
                        if (RepInfo.Semantics.Equals("exactly-once"))
                        {
                            foreach (Tuple2TupleProcessed t2t in obj.TupleToTupleProcessed.ToArray())
                            {
                                if (t.toString().Equals(t2t.Pre.toString()))
                                {
                                    Console.WriteLine("Tuple already processed by the next rep so not gonna send it again: " + t.toString());
                                    return;
                                }
                            }
                        }
                        AddTupleToReceiveAck(t);//Save tuple to receive ack
                        obj.AddTupleToBeAcked(t, RepInfo.MyUrl);//Send MyUrl to be acked
                        obj.AddTupleToBuffer(t);

                    }

                }
            }
            if (last)
            {
                if (comments) Console.WriteLine("SOU O ULTIMO");
                return;
            }
        }

        /// <summary>
        /// Returns the primary url of the replica
        /// </summary>
        /// <param name="msg">Message sent to PM.</param>
        private string Primary(ArrayList urls, Tuple t)
        {
            //TODO Needs to be refactored when implementing fault tolerancy
            return (string) urls[0];
        }

        /// <summary>
        /// Returns a random url of a replica
        /// </summary>
        /// <param name="msg">Message sent to PM.</param>
        private string CRandom(ArrayList urls, Tuple t)
        {
            //TODO be carefull when we start having failed rep
            Random r = new Random();
            return (string)urls[r.Next(urls.Count)];
        }

        /// <summary>
        /// Returns a url of a replica acording a hashing function
        /// </summary>
        /// <param name="msg">Message sent to PM.</param>
        /*private string Hashing(ArrayList urls)
        {
            //TODO implement properly HASHING ANTIGO
            return (string)urls[Int32.Parse(repInfo.Next_routing_param)];
        }*/


        /// <summary>
        /// Returns a url of a replica acording a hashing function
        /// </summary>
        private string Hashing(ArrayList urls, Tuple t)
        {
            int field_id = Int32.Parse(repInfo.Next_routing_param);

            return HashFunction(t.Index(field_id),urls);
        }

        /// <summary>
        /// Returns a url of a replica acording to length of the string modulus the the size of urls possible
        /// </summary>
        public string HashFunction(String s, ArrayList urls)
        {
            int index = s.Length % urls.Count;

            if (comments) Console.WriteLine("CENAS" + (string)urls[index]);

            return (string)urls[index];
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

                AsyncDelegate RemoteDel = new AsyncDelegate(service.SendToLog);

                // Call delegate to remote method
                IAsyncResult RemAr = RemoteDel.BeginInvoke(msg, null, null);
            }
        }

        public void ping(string msg)
        {
            Console.WriteLine(msg);
        }

        public RepInfo getRepInfoFromRep()
        {
            return RepInfo;
        }


        public void updateRepInfo(RepInfo repInfo)
        {
            RepInfo = repInfo;
        }

        /// <summary>
        /// Doesn't let service expire.
        /// </summary>
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void AddTupleToBuffer(Tuple t)
        {
            this.threadPool.AssyncInvoke(t);
        }




        /// <summary>
        /// AddTupleToBeAcked inserts tuple in ToBeAcked.
        /// </summary>
        /// <param name="t">Tuple that will be acked</param>
        public void AddTupleToBeAcked(Tuple t, string ackUrl)//Add tuple t to be acked to myUrl
        {
            if (!RepInfo.Semantics.Equals("at-most-once"))
            {
                AckTuple temp = new AckTuple(t, ackUrl);
                ToBeAcked.Add(temp);
                //Console.WriteLine("Tuple to be acked " + t.toString() + " added to the Dictionary");
            }
        }

        /// <summary>
        /// AddTupleToReceiveAck inserts tuple in ToReceiveAck.
        /// </summary>
        /// <param name="t">Tuple that will receive ack</param>
        public void AddTupleToReceiveAck(Tuple t)//Add tuple to receive ack
        {
            if (!RepInfo.Semantics.Equals("at-most-once"))
            {
                ToReceiveAck.Add(t);
                TimerTuple temp = new TimerTuple(t, 0);
                TimerAck.Add(temp);
                //Console.WriteLine("Tuple to receive ack " + t.toString() + " added to the Dictionary");
            }
        }


        public void removeToBeAck(AckTuple t)//Remove tuple that needed to be sent ack from the list
        {
            if (!RepInfo.Semantics.Equals("at-most-once"))
            {
                if (ToBeAcked.Contains(t))
                {
                    Console.WriteLine("removeToBeAck: " + t.AckT.toString());
                    ToBeAcked.Remove(t);
                }
                else
                {
                    Console.WriteLine("removeToBeAck: Error while removing tuple after being acked: " + t.AckT.toString());
                }
            }
        }
        public void receivedAck(Tuple t)//Remove tuple that needed to receive ack from the list
        {
            if (!RepInfo.Semantics.Equals("at-most-once"))
            {
                foreach(Tuple t2 in ToReceiveAck.ToArray())
                {
                    if (t.toString().Equals(t2.toString()))
                    {
                        
                        ToReceiveAck.Remove(t);
                        foreach(TimerTuple t3 in TimerAck.ToArray())
                        {
                            if (t.toString().Equals(t3.AckT.toString()))
                            {
                                TimerAck.Remove(t3);
                                Console.WriteLine("receivedAck: " + t.toString());
                                break;
                            }
                        }
                        return;//We only want to remove 1
                    }
                }
                Console.WriteLine("receivedAck: Error while removing tuple after being acked: " + t.toString());
            }
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

    class AckTuple
    {
        /// <summary>
        /// List with the elements of the Tuple.
        /// </summary>
        private Tuple ackT;

        private string urlToAck;

        /// <summary>
        /// Tuple Contructor.
        /// </summary>
        public AckTuple(Tuple ackT, string urlToAck)
        {
            this.ackT = ackT;
            this.urlToAck = urlToAck;
        }

        public Tuple AckT
        {
            get { return ackT; }
            set { ackT = value; }
        }

        public string UrlToAck
        {
            get { return urlToAck; }
            set { urlToAck = value; }
        }


    }


    class TimerTuple
    {
        /// <summary>
        /// List with the elements of the Tuple.
        /// </summary>
        private Tuple ackT;

        private int time;

        /// <summary>
        /// Tuple Contructor.
        /// </summary>
        public TimerTuple(Tuple ackT, int time)
        {
            this.ackT = ackT;
            this.time = time;
        }

        public Tuple AckT
        {
            get { return ackT; }
            set { ackT = value; }
        }

        public int Time
        {
            get { return time; }
            set { time = value; }
        }


    }

    [Serializable]
    class Tuple2TupleProcessed
    {
        /// <summary>
        /// List with the elements of the Tuple.
        /// </summary>
        private Tuple pre;

        private IList<Tuple> pos;

        /// <summary>
        /// Tuple Contructor.
        /// </summary>
        public Tuple2TupleProcessed(Tuple pre, IList<Tuple> pos)
        {
            this.pre = pre;
            this.pos = pos;
        }

        public Tuple Pre
        {
            get { return pre; }
            set { pre = value; }
        }

        public IList<Tuple> Pos
        {
            get { return pos; }
            set { pos = value; }
        }


    }
}