

using System;
using System.Windows.Forms;

namespace Dadstorm
{
    public class PuppetMasterServices : MarshalByRefObject, PMServices
    {
        private Form form;
        private Delegate printToForm;

        public PuppetMasterServices(Form form, Delegate printToForm)
        {
            this.form = form;
            this.printToForm = printToForm;
        }
        public void SendToLog(string msg)
        {
            form.Invoke(printToForm, new object[] { msg });
        }
    }
}
