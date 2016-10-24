using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Dadstorm
{
    class PCS
    {
        static void Main(string[] args)
        {
            //Creating the channel
            TcpChannel serverChannel = new TcpChannel(10001);
            ChannelServices.RegisterChannel(serverChannel, false);
            PCSServices pcss = new PCSServices();
            RemotingServices.Marshal(pcss, "PCSServer", typeof(PCSServices));
            System.Console.WriteLine("<enter> para sair...");
            System.Console.ReadLine();
        }
    }
}
