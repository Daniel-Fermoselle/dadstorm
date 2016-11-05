using System;
using System.Collections;
using System.Collections.Generic;

namespace Dadstorm
{
    [Serializable]
    public class RepInfo
    {
        private string routing;
        private string operator_spec;
        private ArrayList operator_param;
        private Dictionary<string, Dictionary<string, ArrayList>> sendInfoUrls;
        private string port;
        private string loggingLvl;

        public RepInfo()
        {

        }

        public RepInfo(string routing, string operator_spec, ArrayList operator_param, 
                       Dictionary<string, Dictionary<string, ArrayList>> sendInfoUrls,
                       string port, string loggingLvl)
        {
            this.routing = routing;
            this.operator_spec = operator_spec;
            this.operator_param = operator_param;
            this.sendInfoUrls = sendInfoUrls;
            this.port = port;
            this.loggingLvl = loggingLvl;
        }

        public string Routing
        {
            set { routing = value; }
            get { return routing;  }
        }

        public string Operator_spec
        {
            set { operator_spec = value; }
            get { return operator_spec; }
        }
        public ArrayList Operator_param
        {
            set { operator_param = value; }
            get { return operator_param; }
        }
        public Dictionary<string, Dictionary<string, ArrayList>> SendInfoUrls
        {
            set { sendInfoUrls = value; }
            get { return sendInfoUrls; }
        }
        public string Port
        {
            set { port = value; }
            get { return port; }
        }

        public string LoggingLvl
        {
            set { loggingLvl = value; }
            get { return loggingLvl; }
        }

        public void AddOperator_param(string toAdd)
        {
            operator_param.Add(toAdd);
        }

    }
}
