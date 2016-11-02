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
        public void createOperator(String port) //mas depois tera de retornar um operator
        {
            //TODO
            Process op = new Process();
            op.StartInfo.FileName = "..\\..\\..\\Operator\\bin\\Debug\\Operator.exe";//tem de ser \\ porque e o caracter de escape
            op.StartInfo.Arguments = port;
            op.Start();


        }
    }
}
