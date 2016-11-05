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
            //TODO Call RepServices method to populate class 
            RemotingServices.Marshal(opServices, OPSERVICE_NAME, typeof(OperatorServices));

            System.Console.WriteLine("Press <enter> to terminate server...");
            System.Console.ReadLine();
        }
    }

    internal class OperatorServices : MarshalByRefObject, RepServices
    {

        public OperatorServices() { }

        public void Start() { }

        public void Interval(string x_ms) { }

        public void Status() { }

        public void Crash() { }

        public void Freeze() { }

        public void Unfreeze() { }

        public void ShutDown() { }
    }
}