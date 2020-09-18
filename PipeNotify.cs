using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;

// https://www.codeproject.com/Tips/492231/Csharp-Async-Named-Pipes

namespace ComMonitor
{
    // Delegate for passing received message back to caller
    public delegate void DelegateMessage(string Reply);

    class PipeServer
    {
        public event DelegateMessage PipeMessage;
        string _pipeName;

        public void Listen(string PipeName)
        {
            try
            {
                // Set to class level var so we can re-use in the async callback method
                _pipeName = PipeName;
                // Create the new async pipe 
                NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeName,
                   PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                // Wait for a connection
                pipeServer.BeginWaitForConnection
                (new AsyncCallback(WaitForConnectionCallBack), pipeServer);
            }
            catch (Exception oEX)
            {
                Debug.WriteLine(oEX.Message);
            }
        }

        private void WaitForConnectionCallBack(IAsyncResult iar)
        {
            try
            {
                // Get the pipe
                NamedPipeServerStream pipeServer = (NamedPipeServerStream)iar.AsyncState;
                // End waiting for the connection
                pipeServer.EndWaitForConnection(iar);

                byte[] buffer = new byte[255];

                // Read the incoming message
                pipeServer.Read(buffer, 0, 255);

                // Convert byte buffer to string
                string stringData = Encoding.UTF8.GetString(buffer, 0, buffer.Length);

                // Pass message back to calling form
                PipeMessage.Invoke(stringData);

                // Kill original sever and create new wait server
                pipeServer.Close();
                pipeServer = null;
                pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.In,
                   1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                // Recursively wait for the connection again and again....
                pipeServer.BeginWaitForConnection(
                   new AsyncCallback(WaitForConnectionCallBack), pipeServer);
            }
            catch
            {
                return;
            }
        }

        public void Send(string SendStr, string PipeName, int TimeOut = 1000)
        {
            try
            {
                NamedPipeClientStream pipeStream = new NamedPipeClientStream
                   (".", PipeName, PipeDirection.Out, PipeOptions.Asynchronous);

                pipeStream.Connect(TimeOut);
                Debug.WriteLine("[Client] Pipe connection established");

                byte[] _buffer = Encoding.UTF8.GetBytes(SendStr);
                pipeStream.BeginWrite
                (_buffer, 0, _buffer.Length, new AsyncCallback(AsyncSend), pipeStream);
            }
            catch (TimeoutException oEX)
            {
                Debug.WriteLine(oEX.Message);
            }
        }

        private void AsyncSend(IAsyncResult iar)
        {
            try
            {
                // Get the pipe
                NamedPipeClientStream pipeStream = (NamedPipeClientStream)iar.AsyncState;

                // End the write
                pipeStream.EndWrite(iar);
                pipeStream.Flush();
                pipeStream.Close();
                pipeStream.Dispose();
            }
            catch (Exception oEX)
            {
                Debug.WriteLine(oEX.Message);
            }
        }
    }
}
