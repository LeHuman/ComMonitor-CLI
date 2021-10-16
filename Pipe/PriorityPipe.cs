using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;

// https://www.codeproject.com/Tips/492231/Csharp-Async-Named-Pipes

namespace Pipe
{
    // Delegate for notifying of connection or error
    public delegate void DelegateNotify();

    internal class PriorityPipe
    {
        private event DelegateNotify PipeConnect;

        private readonly string PipeName;

        public PriorityPipe(string PipeName)
        {
            this.PipeName = PipeName;
        }

        public void SetCallback(DelegateNotify callback)
        {
            PipeConnect = callback;
        }

        public void ListenForPing(int retries = 5)
        {
            int f = retries;
            while (f > 0)
            {
                try
                {
                    // Create the new async pipe
                    NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    // Wait for a connection
                    pipeServer.BeginWaitForConnection(new AsyncCallback(WaitForConnectionPingCallBack), pipeServer);
                    return;
                }
                catch (Exception oEX)
                {
                    Debug.WriteLine(oEX.Message);
                    f--;
                    Thread.Sleep(50);
                }
            }
            Console.WriteLine("Warning: Unable to open priority notifier.");
            Console.WriteLine("Is another ComMonitor open?");
        }

        private void WaitForConnectionPingCallBack(IAsyncResult iar)
        {
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

        public void Ping(int TimeOut = 1000)
        {
            try
            {
                NamedPipeClientStream pipeStream = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);

                pipeStream.Connect(TimeOut);
                Debug.WriteLine("[Client] Priority Pipe connection Pinged");
                Console.WriteLine("Notified priority to other monitor");
                pipeStream.Close();
            }
            catch (TimeoutException oEX)
            {
                Console.WriteLine("No monitor to take priority over");
                Debug.WriteLine(oEX.Message);
            }
        }
    }
}
