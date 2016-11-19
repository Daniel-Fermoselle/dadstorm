using System;
using System.Collections;
using System.Collections.Generic;

namespace Dadstorm
{
    [Serializable]
    public class RepInfo
    {
        private string routing;
        private string routing_param;
        private string next_routing;
        private string next_routing_param;
        private string operator_spec;
        private ArrayList operator_param;
        private Dictionary<string, ArrayList> sendInfoUrls;
        private string port;
        private string loggingLvl;
        private ArrayList input;
        private string pmsUrl;

        public RepInfo()
        {

        }

        public RepInfo(ArrayList input, string routing, string routing_param, string next_routing, string next_routing_param, string operator_spec, ArrayList operator_param, 
                       Dictionary<string, ArrayList> sendInfoUrls, string port, string loggingLvl, 
                       string pmsUrl)
        {
            this.input = input;
            this.routing = routing;
            this.routing_param = routing_param;
            this.next_routing = next_routing;
            this.next_routing_param = next_routing_param;
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

        public string Routing_param
        {
            set { routing_param = value; }
            get { return routing_param; }
        }

        public string Next_routing
        {
            set { next_routing = value; }
            get { return next_routing; }
        }

        public string Next_routing_param
        {
            set { next_routing_param = value; }
            get { return next_routing_param; }
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
        public  Dictionary<string, ArrayList> SendInfoUrls
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
