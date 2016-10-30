using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Windows.Forms;

namespace Dadstorm
{
    public class PuppetMaster 
    {
        private const int PCS_PORT = 10000;
        private const int PM_PORT  = 10001;
        private const string PMSERVICE_NAME  = "PMServices";
        private const string PCSSERVER_NAME  = "PCSServer";
        private const string DEFAULT_LOGGING = "light";

        public delegate void StartAsyncDelegate();
        public delegate void IntervalAsyncDelegate(string x_ms);

        private Form form;
        private Delegate printToForm;
        private Dictionary<string, ConfigInfo> config;
        private Dictionary<string, ArrayList>  repServices;
        private ArrayList commands;
        private int nextCommand;
        private PuppetMasterServices PMService;
        private string loggingLvl;

        public PuppetMaster(Form form, Delegate printToForm)
        {
            //Set atributes
            repServices = new Dictionary<string, ArrayList>();
            nextCommand = 0;
            loggingLvl = DEFAULT_LOGGING;
            
            //Set atributes to print to form
            this.form = form;
            this.printToForm = printToForm;
            
            //Creating Channel and publishing PM services
            TcpChannel channel = new TcpChannel(PM_PORT);
            ChannelServices.RegisterChannel(channel, false);
            PMService = new PuppetMasterServices(form, printToForm);
            RemotingServices.Marshal(PMService, PMSERVICE_NAME,typeof(PMServices));
            
        }

        public void StartProcessesPhase (string filePath)
        {
            //Start processes phase
            Parser p = new Parser();
            p.readFile(@filePath);
            config = p.processFile();
            commands = p.Commands;
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
                    //For each url contact the PCS in the ip of that url and tell him to create a replica
                    string urlOnly = getIPFromUrl(url);
                    PCSServices pcs = getPCSServices("tcp://" + urlOnly + ":" +
                                                      PCS_PORT + "/" + PCSSERVER_NAME);
                    RepInfo info = new RepInfo(c.Routing, c.Operation, 
                                               c.OperationParam, getUrlsToSend(), 
                                               getPortFromUrl(url), loggingLvl); //TODO send url for service?
                    //Create replica
                    pcs.createOperator(info);

                    //Save replica service
                    ArrayList array;
                    repServices.TryGetValue(opx, out array);
                    array.Add(getRepServices(url));
                }
            }
        }

        public void Start(string operator_id)
        {
            ArrayList array;
            repServices.TryGetValue(operator_id, out array);
            for (int i = 0; i < array.Count; i++)
            {
                RepServices repS = (RepServices) array[i];

                //Asynchronous call without callback
                // Create delegate to remote method
                StartAsyncDelegate RemoteDel = new StartAsyncDelegate(repS.Start);

                // Call delegate to remote method
                IAsyncResult RemAr = RemoteDel.BeginInvoke(null, null);

                // Wait for the end of the call and then explictly call EndInvoke
                RemAr.AsyncWaitHandle.WaitOne();
                RemoteDel.EndInvoke(RemAr);
                
                SendToLog("Start " + operator_id);
            }
            //lancar excepcao ou aviso de que o id nao existe
        }

        public void Interval(string operator_id, string x_ms)
        {
            ArrayList array;
            repServices.TryGetValue(operator_id, out array);
            for (int i = 0; i < array.Count; i++)
            {
                RepServices repS = (RepServices)array[i];
                
                //Asynchronous call without callback
                // Create delegate to remote method
                IntervalAsyncDelegate RemoteDel = new IntervalAsyncDelegate(repS.Interval);

                // Call delegate to remote method
                IAsyncResult RemAr = RemoteDel.BeginInvoke(x_ms, null, null);

                // Wait for the end of the call and then explictly call EndInvoke
                RemAr.AsyncWaitHandle.WaitOne();
                RemoteDel.EndInvoke(RemAr);

                SendToLog("Interval " + operator_id + " " + x_ms);
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
                    //TODO Maybe make this call receive callback 
                    //Asynchronous call without callback
                    // Create delegate to remote method
                    StartAsyncDelegate RemoteDel = new StartAsyncDelegate(repS.Status);

                    // Call delegate to remote method
                    IAsyncResult RemAr = RemoteDel.BeginInvoke(null, null);

                    // Wait for the end of the call and then explictly call EndInvoke
                    RemAr.AsyncWaitHandle.WaitOne();
                    RemoteDel.EndInvoke(RemAr);

                    SendToLog("Status");
                }
            }
        }

        public void Crash(string processname)
        {
            //Asynchronous call without callback
            // Create delegate to remote method
            StartAsyncDelegate RemoteDel = new StartAsyncDelegate(getRepServices(processname).Crash);

            // Call delegate to remote method
            IAsyncResult RemAr = RemoteDel.BeginInvoke(null, null);

            // Wait for the end of the call and then explictly call EndInvoke
            RemAr.AsyncWaitHandle.WaitOne();
            RemoteDel.EndInvoke(RemAr);

            SendToLog("Crash " + processname);
        }

        public void Freeze(string processname)
        {
            //Asynchronous call without callback
            // Create delegate to remote method
            StartAsyncDelegate RemoteDel = new StartAsyncDelegate(getRepServices(processname).Freeze);

            // Call delegate to remote method
            IAsyncResult RemAr = RemoteDel.BeginInvoke(null, null);

            // Wait for the end of the call and then explictly call EndInvoke
            RemAr.AsyncWaitHandle.WaitOne();
            RemoteDel.EndInvoke(RemAr);

            SendToLog("Freeze " + processname);
        }

        public void Unfreeze(string processname)
        {
            //Asynchronous call without callback
            // Create delegate to remote method
            StartAsyncDelegate RemoteDel = new StartAsyncDelegate(getRepServices(processname).Unfreeze);

            // Call delegate to remote method
            IAsyncResult RemAr = RemoteDel.BeginInvoke(null, null);

            // Wait for the end of the call and then explictly call EndInvoke
            RemAr.AsyncWaitHandle.WaitOne();
            RemoteDel.EndInvoke(RemAr);

            SendToLog("Unfreeze " + processname);
        }

        public void Wait(string x_ms)
        {
            SendToLog("Wait " + x_ms);
            System.Threading.Thread.Sleep(Int32.Parse(x_ms));
        }

        public void ProcessComands()
        {
            while(nextCommand < commands.Count)
            {
                ProcessSingleCommand();
            }
        }

        public void ProcessSingleCommand()
        {
            //TODO add if's to avoid wrong commands
            if(commands == null || nextCommand >= commands.Count)
            {
                SendToLog("No commands to run");
            }
            String command = (string) commands[nextCommand];
            nextCommand++;
            string[] splitedCommand = command.Split(' ');
            if (splitedCommand[0].StartsWith("Star"))
            {
                Start(splitedCommand[1]);
            }
            else if(splitedCommand[0].StartsWith("I"))
            {
                Interval(splitedCommand[1], splitedCommand[2]);
            }
            else if (splitedCommand[0].StartsWith("Stat"))
            {
                Status();
            }
            else if (splitedCommand[0].StartsWith("C"))
            {
                Crash(splitedCommand[1]);
            }
            else if (splitedCommand[0].StartsWith("F"))
            {
                Freeze(splitedCommand[1]);
            }
            else if (splitedCommand[0].StartsWith("U"))
            {
                Unfreeze(splitedCommand[1]);
            }
            else if (splitedCommand[0].StartsWith("W"))
            {
                Wait(splitedCommand[1]);
            }
            else
            {
                throw new InvalidCommandException(splitedCommand[0]);
            }
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

        public void SendToLog(string msg)
        {
            form.Invoke(printToForm, new object[] { msg });
        }
    }
}
