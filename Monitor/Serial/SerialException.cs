using System;
using System.Runtime.Serialization;

namespace Serial
{
    [Serializable]
    public class SerialException : Exception
    {
        public SerialException()
        {
        }

        public SerialException(string message) : base(message)
        {
        }

        public SerialException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SerialException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
