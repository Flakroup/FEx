using System;
using System.Runtime.Serialization;

namespace FEx.Utilities
{
    [Serializable]
    public class FExException : Exception
    {
        public FExException()
        {
        }

        public FExException(string message)
            : base(message)
        {
        }

        public FExException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FExException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
