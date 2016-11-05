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
        private const string OPSERVICE_NAME = "OperatorServices";

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
        private RepInfo repInfo;

        public OperatorServices() { }

        public RepInfo RepInfo
        {
            get { return repInfo; }
            set { repInfo = value; }
        }

        public void Start() { }

        public void Interval(string x_ms) { }

        public void Status() { }

        public void Crash() { }

        public void Freeze() { }

        public void Unfreeze() { }

        public void ShutDown() { }

        public void Populate(RepInfo info)
        {
            this.repInfo = info;
        }
    }

    class Tuple
    {
        private List<string> elements;

        public Tuple(List<string> tuple)
        {
            this.elements = tuple;
        }
        override public string ToString()
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