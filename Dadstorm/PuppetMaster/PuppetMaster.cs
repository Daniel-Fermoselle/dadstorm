using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Dadstorm
{
    public class PuppetMaster
    {
        private Form form;
        private Delegate printToForm;

        public PuppetMaster(Form form, Delegate printToForm)
        {
            this.form = form;
            this.printToForm = printToForm;
        }

        public void StartProcessesPhase ()
        {
            //Start processes phase
            Parser p = new Parser();
            p.readFile(@"C:\Users\sigma\Dropbox\repos\dadstorm\Exemplos\configEnunciado.txt");
            Dictionary<string, ConfigInfo> result;
            result = p.processFile();
            foreach(string s in result.Keys)
            {
                form.Invoke(printToForm, new object[] { s });
            }
            //Receive inputs and log phase
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
    }
}
