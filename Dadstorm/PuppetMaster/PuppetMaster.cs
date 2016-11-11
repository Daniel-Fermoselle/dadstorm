using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Windows.Forms;

namespace Dadstorm
{
    public class PuppetMaster 
    {
        private const int PCS_PORT = 10001;
        private const int PM_PORT  = 10000;
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
            loggingLvl = p.LoggingLvl;
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
                    RepInfo info = new RepInfo(c.SourceInput ,c.Routing, c.Operation, 
                                               c.OperationParam, getUrlsToSend(opx), 
                                               getPortFromUrl(url), loggingLvl, 
                                               "tcp://" + GetLocalIPAddress() + ":" + PM_PORT + "/" + PMSERVICE_NAME);

                    //Connection with the operator
                    RepServices rs = getRepServices(url);

                    //Create replica
                    pcs.createOperator(info.Port);
                    rs.ping("POGCHAMP");
                    rs.Populate(info); //Initializating operator

                    if (!repServices.ContainsKey(opx))
                    {
                        repServices.Add(opx, new ArrayList());
                    }
                    //Save replica service
                    ArrayList array;
                    repServices.TryGetValue(opx, out array);
                    array.Add(rs); //rs=getRepServices(url)
                }
            }
        }

        public void Start(string operator_id)
        {
            //TODO Start needs to send repInfo
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
            //TODO: lancar excepcao ou aviso de que o id nao existe
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

        public void Crash(string opx, string rep)
        {   
            //Asynchronous call without callback
            // Create delegate to remote method
            StartAsyncDelegate RemoteDel = new StartAsyncDelegate(getReplicaServiceFromProcessname(opx, rep).Crash);

            // Call delegate to remote method
            IAsyncResult RemAr = RemoteDel.BeginInvoke(null, null);

            // Wait for the end of the call and then explictly call EndInvoke
            RemAr.AsyncWaitHandle.WaitOne();
            RemoteDel.EndInvoke(RemAr);

            SendToLog("Crash " + opx + " " + rep);
        }

        public void Freeze(string opx, string rep)
        {
            //Asynchronous call without callback
            // Create delegate to remote method
            StartAsyncDelegate RemoteDel = new StartAsyncDelegate(getReplicaServiceFromProcessname(opx, rep).Freeze);

            // Call delegate to remote method
            IAsyncResult RemAr = RemoteDel.BeginInvoke(null, null);

            // Wait for the end of the call and then explictly call EndInvoke
            RemAr.AsyncWaitHandle.WaitOne();
            RemoteDel.EndInvoke(RemAr);

            SendToLog("Freeze " + opx + " " + rep);
        }

        public void Unfreeze(string opx, string rep)
        {
            //Asynchronous call without callback
            // Create delegate to remote method
            StartAsyncDelegate RemoteDel = new StartAsyncDelegate(getReplicaServiceFromProcessname(opx, rep).Unfreeze);

            // Call delegate to remote method
            IAsyncResult RemAr = RemoteDel.BeginInvoke(null, null);

            // Wait for the end of the call and then explictly call EndInvoke
            RemAr.AsyncWaitHandle.WaitOne();
            RemoteDel.EndInvoke(RemAr);

            SendToLog("Unfreeze " + opx + " " + rep);
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
            if (splitedCommand[0].StartsWith("Start"))
            {
                Start(splitedCommand[1]);
            }
            else if(splitedCommand[0].StartsWith("Interval"))
            {
                Interval(splitedCommand[1], splitedCommand[2]);
            }
            else if (splitedCommand[0].StartsWith("Status"))
            {
                Status();
            }
            else if (splitedCommand[0].StartsWith("Crash"))
            {
                Crash(splitedCommand[1], splitedCommand[2]);
            }
            else if (splitedCommand[0].StartsWith("Freeze"))
            {
                Freeze(splitedCommand[1], splitedCommand[2]);
            }
            else if (splitedCommand[0].StartsWith("Unfreeze"))
            {
                Unfreeze(splitedCommand[1], splitedCommand[2]);
            }
            else if (splitedCommand[0].StartsWith("Wait"))
            {
                Wait(splitedCommand[1]);
            }
            else
            {
                throw new InvalidCommandException(splitedCommand[0]);
            }
        }

        public void shutdownRep()
        {
            foreach(string opx in repServices.Keys)
            {
                ArrayList replicas;
                repServices.TryGetValue(opx, out replicas);
                for (int i = 0; i < replicas.Count; i++)
                {
                    ((RepServices)replicas[i]).Crash();
                }
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

        public Dictionary<string, ArrayList> getUrlsToSend(string OPX)
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
            return subDic;
        }

        private RepServices getReplicaServiceFromProcessname(string opx, string rep)
        {
            ArrayList replicasServices;
            repServices.TryGetValue(opx, out replicasServices);
            int i = Int32.Parse(rep);
            return (RepServices) replicasServices[i];
        }

        public void SendToLog(string msg)
        {
            form.Invoke(printToForm, new object[] { msg });
        }

        public string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }
    }
}
