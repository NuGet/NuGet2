using System;
using System.Runtime.Serialization;

namespace NuGet.ShimV3
{
    [Serializable]
    public class ShimException : Exception
    {
        public ShimException()
            : base()
        {

        }

        public ShimException(string message)
            : base(message)
        {

        }

        public ShimException(string message, Exception ex)
            : base(message, ex)
        {

        }

        protected ShimException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
