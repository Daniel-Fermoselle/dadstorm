using System.Collections;

namespace Dadstorm
{
    public class RepInfo
    {
        private string routing;
        private string operator_spec;
        private ArrayList operator_param;
        private ArrayList sendInfoUrls;

        public RepInfo()
        {

        }

        public RepInfo(string routing, string operator_spec, ArrayList operator_param, ArrayList sendInfoUrls)
        {
            this.routing = routing;
            this.operator_spec = operator_spec;
            this.operator_param = operator_param;
            this.sendInfoUrls = sendInfoUrls;
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
        public ArrayList SendInfoUrls
        {
            set { sendInfoUrls = value; }
            get { return sendInfoUrls; }
        }

        public void AddOperator_param(string toAdd)
        {
            operator_param.Add(toAdd);
        }

        public void AddSendInfoUrls(string toAdd)
        {
            sendInfoUrls.Add(toAdd);
        }
    }
}
