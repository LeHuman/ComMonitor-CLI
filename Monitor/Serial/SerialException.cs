using System;

namespace ComMonitor.Serial {

    [Serializable]
    public class SerialException : Exception {

        public SerialException() {
        }

        public SerialException(string message) : base(message) {
        }

        public SerialException(string message, Exception innerException) : base(message, innerException) {
        }
    }
}
