using System.Collections;

/*
 * This class is meant to pass information from the parser to the PuppetMaster it could also be used
 * to send information from the PuppetMaster to the PCS (needs to implement MarshalByRefObject in that case i think)
 *///
namespace Dadstorm
{
    public class ConfigInfo
    {
        private string operatorId;
        private ArrayList sourceInput;
        private int repFactor;
        private string routing;
        private string routing_param;
        private string next_routing;
        private string next_routing_param;
        private ArrayList urls;
        private string operation;
        private ArrayList operationParam;

        public ConfigInfo(string operatorId, ArrayList sourceInput, int repFactor, string routing, 
                          string routing_param, string next_routing, string next_routing_param,
                          ArrayList urls, string operation, ArrayList operationParam)
        {
            this.operatorId = operatorId;
            this.sourceInput = sourceInput;
            this.repFactor = repFactor;
            this.routing = routing;
            this.next_routing = next_routing;
            this.next_routing_param = next_routing_param;
            this.routing_param = routing_param;
            this.urls = urls;
            this.operation = operation;
            this.operationParam = operationParam;
        }

        public ConfigInfo()
        {
            sourceInput = new ArrayList();
            urls = new ArrayList();
            operationParam = new ArrayList();
        }

        public string OperatorId
        {
            set { operatorId = value; }
            get { return operatorId;  }
        }

        public ArrayList SourceInput
        {
            set { sourceInput = value; }
            get { return sourceInput;  }
        }

        public int RepFactor
        {
            set { repFactor = value; }
            get { return repFactor;  }
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

        public ArrayList Urls
        {
            set { urls = value; }
            get { return urls;  }
        }

        public string Operation
        {
            set { operation = value; }
            get { return operation;  }
        }

        public ArrayList OperationParam
        {
            set { operationParam = value; }
            get { return operationParam;  }
        }

        public void AddSourceInput(string toAdd)
        {
            sourceInput.Add(toAdd);
        }

        public void AddUrls(string toAdd)
        {
            urls.Add(toAdd);
        }
   
        public void AddOperationParam(string toAdd)
        {
            operationParam.Add(toAdd);
        }
    }
}
