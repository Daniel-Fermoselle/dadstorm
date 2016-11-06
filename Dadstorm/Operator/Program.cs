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
        /// OperatorServices constructor.
        /// </summary>
        public OperatorServices()
        {
            threadPool = new ThrPool(THREAD_NUMBER, BUFFER_SIZE, this);
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
        /// Response to a Start command.
        /// Sending read tuples from files to the buffer.
        /// </summary>
        public void Start()
        {
            List<Tuple> tupleList = new List<Tuple>();

            //TODO Here comes the parser 

            foreach(Tuple t in tupleList)
            {
                threadPool.AssyncInvoke(t);
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
        public void Status() { }

        /// <summary>
        /// Response to a Crash command.
        /// </summary>
        public void Crash() { }

        /// <summary>
        /// Response to a Freeze command.
        /// </summary>
        public void Freeze() { }

        /// <summary>
        /// Response to a Unfreeze command.
        /// </summary>
        public void Unfreeze() { }

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