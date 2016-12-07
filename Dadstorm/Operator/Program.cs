using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;

namespace Dadstorm
{
    delegate IList<Tuple> processTuple(Tuple t);//delegate with the type of processing of this replica
    delegate string sendTuplePolicy(ArrayList urls, Tuple t);//delegate related with the sending policy of this replica
    delegate void sendTupleDelegate(Tuple t);//Delegate to the ResendTuple
    delegate void sendAlivesDelegate(OperatorServices ri);//Delegate to the am alive messages


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

            Console.WriteLine("I am an op replica and I am located at the port: " + port);

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
        private const int TIMEOUT = 8000;

        /// <summary>
        /// Am alive timer to inform my siblings that i am alive
        /// </summary>
        private Timer siblingsTimer;

        /// <summary>
        /// Am alive timer to inform my parents(a.k.a senders) that i am alive
        /// </summary>
        private Timer parentsTimer;
        
        /// <summary>
        /// Timeout of an am alive in ms.
        /// </summary>
        private const int ALIVE_TIMEOUT = 10000;

        /// <summary>
        /// ArrayList with the url of the parents of this replica
        /// </summary>
        private ArrayList parentsUrl;

        /// <summary>
        /// ArrayList with the url of the OP ids
        /// </summary>
        private ArrayList opsIds;

        /// <summary>
        /// ArrayList with the tuples that are replicated on the other replicas to allow the operator to recover in case a replica dies
        /// </summary>
        private ArrayList replicatedTuples;

        private string operatorName;

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
            opsIds = new ArrayList();
            replicatedTuples = new ArrayList();
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
        /// TupleToTupleProcessed setter and getter.
        /// </summary>
        public ArrayList TupleToTupleProcessed
        {
            get { return tupleToTupleProcessed; }
            set { tupleToTupleProcessed = value; }
        }

        public void addTupleToTupleProcessed(Tuple2TupleProcessed tt)
        {
            TupleToTupleProcessed.Add(tt);
        }

        /// <summary>
        /// SiblingsTimer setter and getter.
        /// </summary>
        public Timer SiblingsTimer
        {
            get { return siblingsTimer; }
            set { siblingsTimer = value; }
        }

        /// <summary>
        /// ParentsTimer setter and getter.
        /// </summary>
        public Timer ParentsTimer
        {
            get { return parentsTimer; }
            set { parentsTimer = value; }
        }


        /// <summary>
        /// ParentsUrl setter and getter.
        /// </summary>
        public ArrayList ParentsUrl
        {
            get { return parentsUrl; }
            set { parentsUrl = value; }
        }


        /// <summary>
        /// OpsIds setter and getter.
        /// </summary>
        public ArrayList OpsIds
        {
            get { return opsIds; }
            set { opsIds = value; }
        }

        /// <summary>
        /// ReplicatedTuples setter and getter.
        /// </summary>
        public ArrayList ReplicatedTuples
        {
            get { return replicatedTuples; }
            set { replicatedTuples = value; }
        }
        
        public void addRepTuple(Tuple t)
        {
            ReplicatedTuples.Add(t);
        }

        public void removeRepTuple(Tuple t)
        {
            ReplicatedTuples.Remove(t);
        }


        public string OperatorName
        {
            get { return operatorName; }
            set { operatorName = value; }
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
            SiblingsTimer = new Timer(AlivesParents.Method, this, ALIVE_TIMEOUT, ALIVE_TIMEOUT);
            ParentsTimer = new Timer(AlivesSiblings.Method, this, ALIVE_TIMEOUT, ALIVE_TIMEOUT);
            startChildrenList();
            OperatorName = RepInfo.OperatorId + RepInfo.SiblingsUrls.IndexOf(info.MyUrl);
            foreach (string opx in repInfo.SendInfoUrls.Keys)
            {
                OpsIds.Add(opx);
            }

                foreach (string s in repInfo.Input)
            {
                if (s.Contains(".dat"))
                {
                    OpParser parser = new OpParser(s, OperatorName);
                    tupleList = parser.processFile();
                    if (info.SiblingsUrls.Count == 1)
                    {
                        subTupleList = tupleList;
                        //In this case all tuples are read by the replica
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

        public void startChildrenList()//Start array with every url of a replica which is going to receive tuples from this one
        {
            ParentsUrl = new ArrayList();

            ArrayList temp;
            foreach (string opx in repInfo.SendInfoUrls.Keys)
            {
                repInfo.SendInfoUrls.TryGetValue(opx, out temp);
                ParentsUrl.AddRange(temp);
            }
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
                Console.WriteLine("Tuple: " + t.toString() + " id: " + t.Id);
                foreach (Tuple2TupleProcessed t2t in TupleToTupleProcessed.ToArray())//List of tuples already processed
                {
                    if (t.Id.Equals(t2t.Pre.Id))//Checking if the new processing tuple was already processed
                    {
                        if (Comments) Console.WriteLine("Tuple already processed going to reject it");
                        result = null;//rejects duplicated tuples
                        notInList = false;
                    }
                }
                if (notInList)
                {
                    processors.TryGetValue(this.repInfo.Operator_spec, out value);//getting the processor method for this operator
                    result = value(t);//Processing the tuple
                    Tuple2TupleProcessed temp = new Tuple2TupleProcessed(t, result);
                    TupleToTupleProcessed.Add(temp);
                    foreach (string url in RepInfo.SiblingsUrls.ToArray())//in this semantics (exactly-once) the sibling tuples receive a copy of the already processed tuples
                    {
                        if (!url.Equals(RepInfo.MyUrl))
                        {
                            try
                            {
                                OperatorServices obj = (OperatorServices)Activator.GetObject(typeof(OperatorServices), url);
                                obj.addTupleToTupleProcessed(temp);
                                Console.WriteLine("TupleToTupleProcessed added: " + t.toString() + " to the sibling: " + url);
                            }
                            catch (System.Net.Sockets.SocketException e)
                            {//if the other replica is dead there is no problem in not having it receiving the copy of the already processed Tuples aka TupleToTupleProcessed
                            }
                        }
                    }
                }
            }
            else
            {
                processors.TryGetValue(this.repInfo.Operator_spec, out value);//getting the processor method for this operator
                result = value(t);//Processing the tuple
            }

            //Give ack to previous rep
            foreach (AckTuple t2 in ToBeAcked.ToArray())
            {
                if (t2.AckT.Id.Equals(t.Id))
                {
                    Console.WriteLine("Going to remove the tuple from to be acked list : " + t.toString());
                    ackTuple(t);//This might get confused when more than one OP is used as input to this operator
                    this.removeToBeAck(t2);
                    break;
                }
            }

            if (!RepInfo.Semantics.Equals("at-most-once")) {
                foreach (string url in RepInfo.SiblingsUrls.ToArray())//Sharing with this rep siblings the tuples that need to receive ack in order to allow fault tolerance
                {
                    if (!url.Equals(RepInfo.MyUrl))
                    {
                        try
                        {
                            OperatorServices obj = (OperatorServices)Activator.GetObject(typeof(OperatorServices), url);
                            obj.addRepTuple(t);
                            Console.WriteLine("Added tuple: " + t.toString() + " to the sibling in: " + url);
                        }

                        catch (System.Net.Sockets.SocketException e)
                        {//If the other replicas is dead there is no problem in not having it receiving the copy of the tuples that need to be processed in case of failure of the "main" replica
                        }
                    }
                }
            }
            
            return result;
        }

        public void ackTuple(Tuple t)//AckTuples if needed
        {
            if (!RepInfo.Semantics.Equals("at-most-once"))
            {
                Console.WriteLine("TupleAcked: " + t.toString());
                foreach (string url in RepInfo.ReceiveInfoUrls.ToArray())//Acking tuples to the previous op, if at least one receives the ack the tuple is acked and propagated
                {
                    try
                    {
                        OperatorServices obj = (OperatorServices)Activator.GetObject(typeof(OperatorServices), url);
                        obj.receivedAck(t, true);
                        if (comments) Console.WriteLine("At least one replica received the ack");
                        break;
                    }
                    catch(System.Net.Sockets.SocketException e)//It s only needed to receive at least one ack per operator i.e just one replica of an operator needs to receive the ack
                    {
                        continue;
                    }
                }
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

            Tuple newTuple = new Tuple(t.Elements, OperatorName);
            tupleProcessed.Add(newTuple);
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
            Tuple tuple = new Tuple(countTuple, OperatorName);
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
            Tuple newTuple = new Tuple(t.Elements, OperatorName);
            tupleProcessed.Add(newTuple);
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
            Tuple newTuple = new Tuple(t.Elements, OperatorName);
            tupleProcessed.Add(newTuple);
            if (condition.Equals("="))
            {
                if (condResult==0)
                {
                    return tupleProcessed;
                }
                else { return null; }
            }

            else if (condition.Equals("<"))
            {
                if (condResult == 1)
                {
                    return tupleProcessed;
                }
                else { return null; }

            }

            else if (condition.Equals(">"))
            {
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
                            tupleProcessed.Add(new Tuple(tuple, OperatorName));
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


        public void ResendTuple(Tuple t)//Resends the Tuple t saying it is a resending
        {
            foreach (TimerTuple t3 in TimerAck.ToArray())
            {
                if (t3.AckT.Id.Equals(t.Id))
                {
                    Console.WriteLine("Resending tuple: " + t.toString());
                    SendTuple(t,true);//true states its resending
                }
            }
                
        }


        //This method is called when some sibling dies without receiving ack for tuples it had to send 
        //So all its siblings will check the replicas of tuples and test if they have to process them
        public void RecoverySend (ArrayList list)
        {
            string myUrl = RepInfo.MyUrl;
            sendTuplePolicy value;
            string tempUrl;
            foreach(Tuple t in list.ToArray())
            {
                policies.TryGetValue(RepInfo.Routing, out value);
                tempUrl=value(RepInfo.SiblingsUrls, t);
                if (tempUrl.Equals(myUrl))
                {
                    if (Comments) Console.WriteLine("I am replica: " + myUrl + " I am resending tuple: " + t.toString());
                    processTuple(t);
                }
            }
        }



        /// <summary>
        /// Sends tuples to the next Operator in the channel.
        /// </summary>
        /// <param name="t">Tuple to be sent.</param>
        public void SendTuple(Tuple t, bool resend)
        {
            bool last = true;
            ArrayList urls;


            foreach (string opx in OpsIds.ToArray())
            {
                repInfo.SendInfoUrls.TryGetValue(opx, out urls);
                if (urls.Count >= 1)
                {
                    last = false;
                    sendTuplePolicy value;
                    policies.TryGetValue(this.repInfo.Next_routing, out value);
                    //Getting the OperatorServices object 
                    try
                    {
                        OperatorServices obj = (OperatorServices)Activator.GetObject(typeof(OperatorServices), value(urls, t));
                        if (Comments) obj.ping("PING!");
                        
                        if (!resend && !RepInfo.Semantics.Equals("at-most-once"))
                        {
                            AddTupleToReceiveAck(t, resend);//Save tuple to receive ack stating if it is a resend or not
                            foreach (string url2 in RepInfo.SiblingsUrls.ToArray())//share the acks that need to be received with its siblings
                            {
                                if (!url2.Equals(RepInfo.MyUrl))
                                {
                                    OperatorServices obj2 = (OperatorServices)Activator.GetObject(typeof(OperatorServices), url2);
                                    obj2.AddTupleToReceiveAck(t,resend);
                                    Console.WriteLine("Added receive ack of tuple: " + t.toString() + " to the sibling: " + url2);
                                }
                            }
                        }
                        if (!RepInfo.Semantics.Equals("at-most-once"))
                        {
                            obj.AddTupleToBeAcked(t, RepInfo.MyUrl);//Added tuple to be acked in the receiving replica
                        }
                        obj.AddTupleToBuffer(t);
                    }
                    catch(System.Net.Sockets.SocketException e)
                    {// if the other replica rejects the connection the tuple is not send and the timer will make this replica resend the tuple to one of the possible sending replicas
                    }



                }
            }
            if (last)
            {
                if (comments) Console.WriteLine("I am one of the last operator's replica");
                return;
            }
        }

        /// <summary>
        /// Returns the primary url of the replica
        /// </summary>
        /// <param name="msg">Message sent to PM.</param>
        private string Primary(ArrayList urls, Tuple t)
        {
            return (string) urls[0];
        }

        /// <summary>
        /// Returns a random url of a replica
        /// </summary>
        /// <param name="msg">Message sent to PM.</param>
        private string CRandom(ArrayList urls, Tuple t)
        {
            Random r = new Random();
            return (string)urls[r.Next(urls.Count)];
        }
        

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

            if (comments) Console.WriteLine("Index tuple" + (string)urls[index]);

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

        public string getPing()
        {
            return "pong";
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
            }
        }

        /// <summary>
        /// AddTupleToReceiveAck inserts tuple in ToReceiveAck.
        /// </summary>
        /// <param name="t">Tuple that will receive ack</param>
        public void AddTupleToReceiveAck(Tuple t, bool resend)//Add tuple to receive ack
        {
            if (!RepInfo.Semantics.Equals("at-most-once"))
            {
                if (!resend)
                {
                    TimerMethod temptimer = new TimerMethod(ResendTuple);//Starts timer with timeout defined as a constant at the top of this file
                    Timer stateTimer = new Timer(temptimer.ResendTupleMethod, t, TIMEOUT, TIMEOUT);
                    TimerTuple temp = new TimerTuple(t, stateTimer);
                    TimerAck.Add(temp);
                    ToReceiveAck.Add(t);
                }
                
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
        public void receivedAck(Tuple t, bool notRep)//Remove tuple that needed to receive ack from the list
        {
            if (!RepInfo.Semantics.Equals("at-most-once"))
            {
                foreach (Tuple t2 in ToReceiveAck.ToArray())
                {
                    if (t.Id.Equals(t2.Id))
                    {
                        
                        ToReceiveAck.Remove(t);
                        removeRepTuple(t);//removes if exists the replica of a a tuple processed
                        foreach(TimerTuple t3 in TimerAck.ToArray())
                        {
                            if (t.Id.Equals(t3.AckT.Id))
                            {
                                t3.Time.Dispose();
                                TimerAck.Remove(t3);
                                Console.WriteLine("receivedAck: " + t.toString());
                                break;
                            }
                        }
                        if (notRep)
                        {
                            foreach (string url in RepInfo.SiblingsUrls.ToArray())//removes replicated receive acks from siblings
                            {
                                if (!url.Equals(RepInfo.MyUrl))
                                {
                                    try
                                    {
                                        OperatorServices obj = (OperatorServices)Activator.GetObject(typeof(OperatorServices), url);
                                        obj.receivedAck(t, false);
                                        Console.WriteLine("Removed ack of tuple: " + t.toString() + " from sibling: " + url);
                                    }
                                    catch (System.Net.Sockets.SocketException e)
                                    {//if the replica that should received ack is dead no problem
                                    }
                                }
                            }
                        }
                        return;//We only want to remove 1
                    }
                }
                Console.WriteLine("receivedAck: Error while removing tuple after being acked: " + t.toString());
            }
        }


        //If the routing of this replica is primary we want to share the readtuples var
        //in order to have consistency while counting or check if a tuple is unique
        public void addTupleRead(Tuple t)
        {
            threadPool.addTupleRead(t);
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

        private static int staticID = 0;//This allow the id to be unique everytime a new tuple is created

        private string id;//Only to be used when the semantics is exactly once

        /// <summary>
        /// Tuple Contructor.
        /// </summary>
        public Tuple(IList<string> tuple, string opId)
        {
            this.elements = tuple;
            int tempId = System.Threading.Interlocked.Increment(ref staticID);
            string stringId = tempId.ToString();
            this.id = opId + stringId;
        }

        public IList<string> Elements
        {
            get { return elements; }
            set { elements = value;}
        }

        public string Id
        {
            get { return id; }
            set { id = value; }
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

    //Tuple that as the url of the sender replica
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

    //Tuple with the associated timeout of resending
    class TimerTuple
    {
        /// <summary>
        /// List with the elements of the Tuple.
        /// </summary>
        private Tuple ackT;

        private Timer time;

        /// <summary>
        /// Tuple Contructor.
        /// </summary>
        public TimerTuple(Tuple ackT, Timer time)
        {
            this.ackT = ackT;
            this.time = time;
        }

        public Tuple AckT
        {
            get { return ackT; }
            set { ackT = value; }
        }

        public Timer Time
        {
            get { return time; }
            set { time = value; }
        }


    }


    //Tuple associated with the respective result of being processed by a certain operator
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


    //Class to allow timer method through a delegate
    class TimerMethod
    {

        private sendTupleDelegate sender;

        public TimerMethod(sendTupleDelegate s)
        {
            sender = s;
        }



        // This method is called by the timer delegate.
        public void ResendTupleMethod(Object stateInfo)
        {
            sender((Tuple)stateInfo);
        }
    }
    

    //Classe with the timers to send pings and recovering from faults regarding the replicas which are going to receive tuples from this replica
    class AlivesParents
    {
        public AlivesParents() { }

        public static void sendAliveParents(OperatorServices me)
        {
            if (!(me.RepFreeze && me.RepCrash))
            {
                
                foreach (string url in me.ParentsUrl.ToArray())
                {
                    try { 
                        OperatorServices obj = (OperatorServices)Activator.GetObject(typeof(OperatorServices), url);
                        String ping = obj.getPing();
                    }
                    catch (System.Net.Sockets.SocketException e)//if a parent of a replica dies happens the following
                    {
                        me.ParentsUrl.Remove(url);
                        ArrayList temp;
                        ArrayList renewList = new ArrayList();
                        string renewOpx = "";
                        foreach (string opx in me.OpsIds.ToArray())
                        {
                            me.RepInfo.SendInfoUrls.TryGetValue(opx, out temp);
                            foreach(string s in temp.ToArray())
                            {
                                if (s.Equals(url))
                                {
                                    temp.Remove(url);
                                    renewList = temp;
                                    renewOpx = opx;
                                    break;
                                }
                            }
                            
                        }
                        me.RepInfo.SendInfoUrls[renewOpx] = renewList;//Updating the dictionary of replicas

                    }
                }
            }

        }

        // This method is called by the timer delegate.
        public static void Method(Object stateInfo)
        {
            sendAliveParents((OperatorServices)stateInfo);
        }
    }


    //Classe with the timers to send pings and recovering from faults regarding the sibling replicas
    class AlivesSiblings
    {
        public AlivesSiblings() { }

        public static void sendAliveSiblings(OperatorServices me)
        {
            if (!(me.RepFreeze && me.RepCrash))
            {
                foreach(string url in me.RepInfo.SiblingsUrls.ToArray())
                {
                    if (!url.Equals(me.RepInfo.MyUrl))
                    {
                        try { 
                            OperatorServices obj = (OperatorServices)Activator.GetObject(typeof(OperatorServices), url);
                            String ping = obj.getPing();
                        }
                        catch(System.Net.Sockets.SocketException e)//If a sibling of a replica dies happens the following
                        {
                            me.RepInfo.SiblingsUrls.Remove(url);
                            me.RecoverySend(me.ReplicatedTuples);
                        }
                    }
                }
            }
        }

        // This method is called by the timer delegate.
        public static void Method(Object stateInfo)
        {
            sendAliveSiblings((OperatorServices)stateInfo);
        }
    }

}