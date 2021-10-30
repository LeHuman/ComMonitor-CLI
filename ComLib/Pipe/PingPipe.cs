using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;

// https://www.codeproject.com/Tips/492231/Csharp-Async-Named-Pipes

namespace Pipe {

    // Delegate for notifying of connection or error
    public delegate void DelegateNotify();

    public class PingPipe {

        private event DelegateNotify PipeConnect;

        private readonly string PipeName;

        public PingPipe(string PipeName) {
            this.PipeName = PipeName;
        }

        public void SetCallback(DelegateNotify callback) {
            PipeConnect = callback;
        }

        public bool ListenForPing(int retries = 5) {
            while (retries > 0) {
                try {
                    // Create the new async pipe
                    NamedPipeServerStream pipeServer = new(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    // Wait for a connection
                    pipeServer.BeginWaitForConnection(new AsyncCallback(WaitForConnectionPingCallBack), pipeServer);
                    return true;
                } catch (Exception oEX) {
                    Debug.WriteLine(oEX.Message);
                    retries--;
                    Thread.Sleep(50);
                }
            }
            return false;
        }

        private void WaitForConnectionPingCallBack(IAsyncResult iar) {
            // Get the pipe
            NamedPipeServerStream pipeServer = (NamedPipeServerStream)iar.AsyncState;
            // End waiting for the connection
            pipeServer.EndWaitForConnection(iar);
            PipeConnect.Invoke();

            // Kill original sever and create new wait server
            pipeServer.Close();
            pipeServer = null;
            pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            pipeServer.BeginWaitForConnection(new AsyncCallback(WaitForConnectionPingCallBack), pipeServer);
        }

        public bool Ping(int TimeOut = 1000) {
            try {
                NamedPipeClientStream pipeStream = new(".", PipeName, PipeDirection.Out);

                pipeStream.Connect(TimeOut);
                Debug.WriteLine("[Client] Pipe connection Pinged");
                pipeStream.Close();
                return true;
            } catch (TimeoutException oEX) {
                Debug.WriteLine(oEX.Message);
                return false;
            }
        }
    }
}
