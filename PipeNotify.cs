using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

// https://www.codeproject.com/Tips/492231/Csharp-Async-Named-Pipes

namespace ComMonitor
{
    // Delegate for notifying of connection or error
    public delegate void DelegateNotify();

    class PipeServer
    {
        public event DelegateNotify PipeConnect;
        string _pipeName;

        public void ListenForPing(string PipeName, int retries = 5)
        {
            int f = retries;
            while (f > 0)
            {
                try
                {
                    // Set to class level var so we can re-use in the async callback method
                    _pipeName = PipeName;
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
            pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            pipeServer.BeginWaitForConnection(new AsyncCallback(WaitForConnectionPingCallBack), pipeServer);

        }

        public void Ping(string PipeName, int TimeOut = 1000)
        {
            try
            {
                NamedPipeClientStream pipeStream = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);

                pipeStream.Connect(TimeOut);
                Debug.WriteLine("[Client] Pipe connection Pinged");
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
