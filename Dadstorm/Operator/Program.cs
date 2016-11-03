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

    public class RepServer
    {

        private const string REPSERVICE_NAME = "RepServices";

        static void Main(string[] args)
        {
            int port = Int32.Parse(args[0]);

            //Creating the channel
            TcpChannel serverChannel = new TcpChannel(port);
            ChannelServices.RegisterChannel(serverChannel, false);

            // Expose an object form RepServices for remote calls.
            RepServices repServices = new RepServices();
            //TODO Call RepServices method to populate class 
            RemotingServices.Marshal(repServices, REPSERVICE_NAME, typeof(RepServices));

            System.Console.WriteLine("<enter> para sair...");
            System.Console.ReadLine();
        }
    }
}