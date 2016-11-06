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
        private const int THREAD_NUMBER = 4;
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
        /// Pupper Master Start command.
        /// </summary>
        public void Start() { }

        /// <summary>
        /// Pupper Master Interval command.
        /// </summary>
        public void Interval(string x_ms) { }

        /// <summary>
        /// Pupper Master Status command.
        /// </summary>
        public void Status() { }

        /// <summary>
        /// Pupper Master Crash command.
        /// </summary>
        public void Crash() { }

        /// <summary>
        /// Pupper Master Freeze command.
        /// </summary>
        public void Freeze() { }

        /// <summary>
        /// Pupper Master Unfreeze command.
        /// </summary>
        public void Unfreeze() { }

        /// <summary>
        /// Pupper Master ShutDown command.
        /// </summary>
        public void ShutDown() { }

        /// <summary>
        /// Pupper Master Populate command.
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