using System;
using System.Collections.Generic;
using System.Text;

namespace Dadstorm
{
    //Parser exceptions
    public class InvalidPathException : ApplicationException
    {

        public InvalidPathException() { }

        public InvalidPathException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) 
            : base(info, context) {   }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }

    public class InvalidKeyWordException : ApplicationException
    {
        private string line;
        public InvalidKeyWordException(string line)
        {
            this.line = line;
        }

        public InvalidKeyWordException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            info.GetString("line");
        }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, 
                                           System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("line", line);
        }
    }

}
