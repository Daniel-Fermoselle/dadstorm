using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Windows.Forms;

namespace Dadstorm
{
    public class PuppetMaster
    {
        private const int PORT = 1000;

        private Form form;
        private Delegate printToForm;
        private Dictionary<string, ConfigInfo> config;

        public PuppetMaster(Form form, Delegate printToForm)
        {
            this.form = form;
            this.printToForm = printToForm;
            
            //Creating Channel
            TcpChannel channel = new TcpChannel(PORT);
            ChannelServices.RegisterChannel(channel, false);
        }

        public void StartProcessesPhase ()
        {
            //Start processes phase
            Parser p = new Parser();
            p.readFile(@"C:\Users\sigma\Dropbox\repos\dadstorm\Exemplos\dadstorm.config"); //TODO tornar dinamico
            config = p.processFile();
            StartProcesses();
        }
        
        private void StartProcesses()
        {
            foreach(string opx in config.Keys)
            {
                ConfigInfo c;
                config.TryGetValue(opx, out c);
                ArrayList urls = c.Urls;
                foreach(string url in urls)
                {
                    //For each url contact the PCS in the ip of that url and tell his to creat a replica
                    String urlOnly = getIPFromUrl(url);
                    PCSServices pcs = getPCSServices("tcp://" + urlOnly + ":10001/PCSServer");
                    RepInfo info = new RepInfo(c.Routing, c.Operation, c.OperationParam, getUrlsToSend(), getPortFromUrl(url));
                    //Set info to send
                   // pcs.createOperator(info);
                }
            }
        }

        private string getIPFromUrl(string url)
        {
            string[] splitedUrl = url.Split('/');
            return splitedUrl[2].Split(':')[0];
        }

        private string getPortFromUrl(string url)
        {
            string[] splitedUrl = url.Split('/');
            return splitedUrl[2].Split(':')[1];
        }

        public void Start(string operator_id)
        {
            //TODO: implement
        }

        public void Interval(string operator_id, string x_ms)
        {
            //TODO: implement
        }

        public void Status()
        {
            //TODO: implement
        }

        public void Crash(string processname)
        {
            //TODO: implement
        }

        public void Freeze(string processname)
        {
            //TODO: implement
        }

        public void Unfreeze(string processname)
        {
            //TODO: implement
        }

        public void Wait(string x_ms)
        {
            //TODO: implement
        }

        public PCSServices getPCSServices(string url)
        {
            //Getting the PCSServices object 
            PCSServices obj = (PCSServices)Activator.GetObject(typeof(PCSServices), url);
            return obj;
        }

        public RepServices getRepServices(string url)
        {
            //Getting the RepServices object 
            RepServices obj = (RepServices)Activator.GetObject(typeof(RepServices), url);
            return obj;
        }

        public Dictionary<string, Dictionary<string, ArrayList>> getUrlsToSend()
        {
            Dictionary<string, Dictionary<string, ArrayList>> toReturn = new Dictionary<string, Dictionary<string, ArrayList>>();
            foreach (string OPX in config.Keys)
            {
                Dictionary<string, ArrayList> subDic = new Dictionary<string, ArrayList>();
                foreach (string OPX2 in config.Keys)
                {
                    ArrayList outputs = new ArrayList();
                    ConfigInfo c;
                    config.TryGetValue(OPX2, out c);
                    foreach(string input in c.SourceInput)
                    {
                        if (input.Equals(OPX))
                        {
                           foreach(string url in c.Urls)
                            {
                                outputs.Add(url);
                            }
                        }
                    }
                    subDic.Add(OPX2, outputs);
                }
                toReturn.Add(OPX, subDic);
            }
            return toReturn;
        }
    }
}
