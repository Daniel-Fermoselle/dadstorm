using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using PM_PCS_CommonClass;

namespace ProcessCreationService
{
    class PCS
    {
        static void Main(string[] args)
        {
            //Creating the channel
            TcpChannel serverChannel = new TcpChannel(10001);
            ChannelServices.RegisterChannel(serverChannel, false);
            PCSServer pcss = new PCSServer();
            RemotingServices.Marshal(pcss, "ProcessCreationServiceServer", typeof(PCSServer));
            System.Console.WriteLine("<enter> para sair...");
            System.Console.ReadLine();
        }
    }
}
