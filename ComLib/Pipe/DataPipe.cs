using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace Pipe {

    public class DataPipe : IDisposable {

        public delegate void DelegateSerialData(string SendingPipeID, byte[] Data);

        internal const int BUFFER_SIZE = 4096;
        internal const int MAX_CONNECTIONS = 32;

        private readonly string PipeName;
        private readonly string SendingPipeID;
        private readonly PipeDirection direction;
        private readonly DelegateSerialData DataReceiver;
        private NamedPipeServerStream InfoPipeServer, DataPipeServer;

        public DataPipe(string PipeName, string PipeID) {
            SendingPipeID = PipeID;
            this.PipeName = PipeName;
            direction = PipeDirection.Out;
        }

        public DataPipe(string PipeName, DelegateSerialData receiver) {
            this.PipeName = PipeName;
            DataReceiver = receiver;
            direction = PipeDirection.In;
        }

        public bool Connected() {
            return DataPipeServer != null && DataPipeServer.IsConnected;
        }

        public void Dispose() {
            InfoPipeServer?.Dispose();
            DataPipeServer?.Dispose();
            GC.SuppressFinalize(this);
        }

        public void SendData(string msg) {
            if (direction == PipeDirection.Out)
                SendData(Encoding.ASCII.GetBytes(msg));
        }

        public void SendData(byte[] Data) {
            if (direction == PipeDirection.Out)
                _ = InfoPipeServer.WriteAsync(Data, 0, Data.Length);
        }

        public bool WaitForConnection(int retries = 5) {
            if (Connected())
                return true;
            while (retries > 0) {
                try {
                    InfoPipeServer = new NamedPipeServerStream(PipeName, direction, direction == PipeDirection.In ? MAX_CONNECTIONS : 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    InfoPipeServer.BeginWaitForConnection(new AsyncCallback(InfoConnectionCallBack), InfoPipeServer);
                    return true;
                } catch (Exception oEX) {
                    Debug.WriteLine(oEX.Message);
                    retries--;
                    Thread.Sleep(50);
                }
            }
            return false;
        }

        private void InfoConnectionCallBack(IAsyncResult iar) {
            NamedPipeServerStream localPipeServer = (NamedPipeServerStream)iar.AsyncState;
            localPipeServer.EndWaitForConnection(iar);

            if (direction == PipeDirection.In) {
                InfoPipeServer = new NamedPipeServerStream(PipeName, direction, MAX_CONNECTIONS, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                _ = InfoPipeServer.BeginWaitForConnection(new AsyncCallback(InfoConnectionCallBack), InfoPipeServer);

                string SendingPipeID = null;
                MemoryStream memoryStream = new();

                byte[] buffer = new byte[BUFFER_SIZE];

                while (localPipeServer.IsConnected || localPipeServer.IsMessageComplete == false) {
                    memoryStream.SetLength(0);
                    do {
                        memoryStream.Write(buffer, 0, localPipeServer.Read(buffer, 0, buffer.Length));
                    } while (localPipeServer.IsMessageComplete == false);

                    if (SendingPipeID == null)
                        SendingPipeID = Encoding.ASCII.GetString(memoryStream.ToArray());
                    else
                        DataReceiver.Invoke(SendingPipeID, memoryStream.ToArray());
                }

                localPipeServer.Close();
            } else {
                SendData(SendingPipeID);
                while (localPipeServer.IsConnected) { }
                InfoPipeServer.Close();
                _ = WaitForConnection();
            }
        }
    }
}
