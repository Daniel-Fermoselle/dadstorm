using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Dadstorm
{
    public class PuppetMaster
    {
        private Form form;
        private Delegate printToForm;
        private Dictionary<string, ConfigInfo> config;

        public PuppetMaster(Form form, Delegate printToForm)
        {
            this.form = form;
            this.printToForm = printToForm;
        }

        public void StartProcessesPhase ()
        {
            //Start processes phase
            Parser p = new Parser();
            p.readFile(@"C:\Users\sigma\Dropbox\repos\dadstorm\Exemplos\dadstorm.config");
            config = p.processFile();
            Console.Read();

            //Receive inputs and log phase
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
                }
            }
        }

    /*    private string getIPfromUrl(string url)
        {

        }*///

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
    }
}
