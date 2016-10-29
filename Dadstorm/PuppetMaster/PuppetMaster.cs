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
        private const int PCS_PORT = 10000;
        private const int PM_PORT  = 10001;

        private Form form;
        private Delegate printToForm;
        private Dictionary<string, ConfigInfo> config;
        private Dictionary<string, ArrayList>  repServices;

        public PuppetMaster(Form form, Delegate printToForm)
        {
            //Set atributes
            this.repServices = new Dictionary<string, ArrayList>();
            //Set atributes to print to form
            this.form = form;
            this.printToForm = printToForm;
            
            //Creating Channel
            TcpChannel channel = new TcpChannel(PM_PORT);
            ChannelServices.RegisterChannel(channel, false);

            //Publish PM Servicies
            //TODO
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
                    PCSServices pcs = getPCSServices("tcp://" + urlOnly + ":" + PCS_PORT + "/PCSServer");
                    RepInfo info = new RepInfo(c.Routing, c.Operation, 
                                               c.OperationParam, getUrlsToSend(), 
                                               getPortFromUrl(url));
                    //Create replica
                    pcs.createOperator(info);

                    //Save replica services
                    ArrayList array;
                    repServices.TryGetValue(opx, out array);
                    array.Add(getRepServices(url));
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
            ArrayList array;
            repServices.TryGetValue(operator_id, out array);
            for (int i = 0; i < array.Count; i++)
            {
                RepServices repS = (RepServices) array[i];
                //TODO make this call Asynchronous
                repS.Start();
                //TODO send action to Log
            }
        }

        public void Interval(string operator_id, string x_ms)
        {
            ArrayList array;
            repServices.TryGetValue(operator_id, out array);
            for (int i = 0; i < array.Count; i++)
            {
                RepServices repS = (RepServices)array[i];
                //TODO make this call Asynchronous
                repS.Interval(x_ms);
                //TODO send action to Log
            }
        }

        public void Status()
        {
            foreach (string operator_id in repServices.Keys)
            {
                ArrayList array;
                repServices.TryGetValue(operator_id, out array);
                for (int i = 0; i < array.Count; i++)
                {
                    RepServices repS = (RepServices)array[i];
                    //TODO make this call Asynchronous and report info in the project paper
                    repS.Status();
                    //TODO send action to Log
                }
            }
        }

        public void Crash(string processname)
        {
            //TODO make this call Asynchronous
            getRepServices(processname).Crash();
            //TODO send action to log
        }

        public void Freeze(string processname)
        {
            //TODO make this call Asynchronous
            getRepServices(processname).Freeze();
            //TODO send action to log        
        }

        public void Unfreeze(string processname)
        {
            //TODO make this call Asynchronous
            getRepServices(processname).Unfreeze();
            //TODO send action to log
        }

        public void Wait(string x_ms)
        {
            //TODO send action to log
            System.Threading.Thread.Sleep(Int32.Parse(x_ms));
        }


        public void ProcessComands()
        {
            //TODO: implement all in a row or step by step
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
