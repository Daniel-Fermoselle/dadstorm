using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Operator
{
    class Operator
    {
        //private operation o;

        public Operator(/*args*/)
        {

        }

        static void Main(string[] args)
        {
            TcpChannel serverChannel = new TcpChannel(/*porto recebido nos args*/);
            ChannelServices.RegisterChannel(serverChannel, false);
            Operator pcss = new Operator();
        }
    }
}
