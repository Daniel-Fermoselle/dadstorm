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
        private ArrayList input;
        private string pmsUrl;

        public RepInfo()
        {

        }

        public RepInfo(ArrayList input, string routing, string operator_spec, ArrayList operator_param, 
                       Dictionary<string, Dictionary<string, ArrayList>> sendInfoUrls,
                       string port, string loggingLvl, string pmsUrl)
        {
            this.input = input;
            this.routing = routing;
            this.operator_spec = operator_spec;
            this.operator_param = operator_param;
            this.sendInfoUrls = sendInfoUrls;
            this.port = port;
            this.loggingLvl = loggingLvl;
            this.pmsUrl = pmsUrl;
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

        public string PmsUrl
        {
            set { pmsUrl = value; }
            get { return pmsUrl; }
        }

        public ArrayList Input
        {
            set { input = value; }
            get { return input; }
        }

        public void AddOperator_param(string toAdd)
        {
            operator_param.Add(toAdd);
        }

    }
}
