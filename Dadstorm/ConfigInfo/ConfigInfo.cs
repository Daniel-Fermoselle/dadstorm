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
        private ArrayList urls;
        private string operation;
        private ArrayList operationParam;

        public ConfigInfo(string operatorId, ArrayList sourceInput, int repFactor, string routing,
                          ArrayList urls, string operation, ArrayList operationParam)
        {
            this.operatorId = operatorId;
            this.sourceInput = sourceInput;
            this.repFactor = repFactor;
            this.routing = routing;
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
