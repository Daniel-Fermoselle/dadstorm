using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Dadstorm
{
    public class PCSServices : MarshalByRefObject
    {
        public PCSServices()
        {

        }
        public void createOperator(String port, string opName)
        {
            Process op = new Process();
            op.StartInfo.FileName = "..\\..\\..\\Operator\\bin\\Debug\\Operator.exe";
            op.StartInfo.Arguments = port + " " + opName;
            op.Start();


        }
    }
}
