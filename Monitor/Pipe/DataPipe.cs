using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace Pipe
{
    internal delegate void SerialDataDelegate(string SendingPipeID, byte[] Data);

    internal class DataPipe
    {
        protected const int BUFFER_SIZE = 4096;
        protected const int MAX_CONNECTIONS = 32;

        private readonly string PipeName;
        private readonly string SendingPipeID;
        private readonly PipeDirection direction;
        private NamedPipeServerStream pipeServer;
        private readonly SerialDataDelegate DataReceiver;

        public DataPipe(string PipeName, string PipeID)
        {
            SendingPipeID = PipeID;
            this.PipeName = PipeName;
            direction = PipeDirection.Out;
        }

        public DataPipe(string PipeName, SerialDataDelegate receiver)
        {
            this.PipeName = PipeName;
            DataReceiver = receiver;
            direction = PipeDirection.In;
        }

        public bool Connected()
        {
            return pipeServer != null && pipeServer.IsConnected;
        }

        internal void SendData(string msg)
        {
            if (direction == PipeDirection.Out)
                SendData(Encoding.ASCII.GetBytes(msg));
        }

        public void SendData(byte[] Data)
        {
            if (direction == PipeDirection.Out)
                pipeServer.WriteAsync(Data, 0, Data.Length);
        }

        public bool WaitForConnection(int retries = 5)
        {
            if (Connected())
                return true;
            while (retries > 0)
            {
                try
                {
                    pipeServer = new NamedPipeServerStream(PipeName, direction, direction == PipeDirection.In ? MAX_CONNECTIONS : 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                    pipeServer.BeginWaitForConnection(new AsyncCallback(ConnectionCallBack), pipeServer);
                    return true;
                }
                catch (Exception oEX)
                {
                    Debug.WriteLine(oEX.Message);
                    retries--;
                    Thread.Sleep(50);
                }
            }
            return false;
        }

        private void ConnectionCallBack(IAsyncResult iar)
        {
            NamedPipeServerStream localPipeServer = (NamedPipeServerStream)iar.AsyncState;
            localPipeServer.EndWaitForConnection(iar);

            if (direction == PipeDirection.In)
            {
                pipeServer = new NamedPipeServerStream(PipeName, direction, MAX_CONNECTIONS, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                pipeServer.BeginWaitForConnection(new AsyncCallback(ConnectionCallBack), pipeServer);

                string SendingPipeID = null;
                MemoryStream memoryStream = new MemoryStream();

                byte[] buffer = new byte[BUFFER_SIZE];

                while (localPipeServer.IsConnected || localPipeServer.IsMessageComplete == false)
                {
                    memoryStream.SetLength(0);
                    do
                    {
                        memoryStream.Write(buffer, 0, localPipeServer.Read(buffer, 0, buffer.Length));
                    } while (localPipeServer.IsMessageComplete == false);

                    if (SendingPipeID == null)
                        SendingPipeID = Encoding.ASCII.GetString(memoryStream.ToArray());
                    else
                        DataReceiver.Invoke(SendingPipeID, memoryStream.ToArray());
                }

                localPipeServer.Close();
            }
            else
            {
                SendData(SendingPipeID);
                while (localPipeServer.IsConnected) { }
                pipeServer.Close();
                WaitForConnection();
            }
        }
    }
}
