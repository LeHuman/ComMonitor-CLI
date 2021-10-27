using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pipe {

    public class PipeDataServer {

        public delegate void DelegateSerialData(byte[] Data);

        public delegate void DelegateSerialStatus(string PipeName, bool IsConnected);

        public delegate DelegateSerialData DelegateSerialInfo(string PipeName, int MaxBytes, string MetaData);

        internal const int BUFFER_SIZE = 4096;
        internal const int MAX_CONNECTIONS = 64;
        internal const string INFO_PIPE_NAME = "ComInfoPipe";

        private readonly DelegateSerialInfo InfoReceiver;
        private DelegateSerialStatus SerialStatusListener;
        private NamedPipeServerStream InfoPipeServer;
        private readonly Thread ServerThread;

        public PipeDataServer(DelegateSerialInfo InfoReceiver) {
            this.InfoReceiver = InfoReceiver;
            ServerThread = new(RunThread);
        }

        public void SetStatusListener(DelegateSerialStatus SerialStatusListener) {
            this.SerialStatusListener = SerialStatusListener;
        }

        public bool Start() {
            try {
                InfoPipeServer = new(INFO_PIPE_NAME, PipeDirection.InOut, MAX_CONNECTIONS, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                ServerThread.Start();
                return true;
            } catch (Exception oEX) {
                Debug.WriteLine(oEX.Message);
            }
            return false;
        }

        public void Stop() {
            InfoPipeServer?.Close();
            InfoPipeServer?.Dispose();
        }

        private void SendReply(string msg) {
            byte[] Data = Encoding.UTF8.GetBytes(msg);
            _ = InfoPipeServer.WriteAsync(Data, 0, Data.Length);
            InfoPipeServer.Flush();
            InfoPipeServer.WaitForPipeDrain();
        }

        private void RunThread() {
            MemoryStream memoryStream = new();
            byte[] buffer = new byte[BUFFER_SIZE];

            while (true) {
                try {
                    InfoPipeServer.WaitForConnection();
                    memoryStream.SetLength(0);

                    do {
                        memoryStream.Write(buffer, 0, InfoPipeServer.Read(buffer, 0, buffer.Length));
                    } while (InfoPipeServer.IsMessageComplete == false);

                    string[] PipeData = Encoding.UTF8.GetString(memoryStream.ToArray()).Split(',', 3);
                    SendReply(PipeData[0]);
                    Task.Run(() => { ReceiveData(PipeData[0], InfoReceiver.Invoke(PipeData[0], int.Parse(PipeData[1]), PipeData[2]), SerialStatusListener); });
                } catch (SystemException) {
                    Stop();
                    InfoPipeServer = new(INFO_PIPE_NAME, PipeDirection.InOut, MAX_CONNECTIONS, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                }
            }
        }

        private static void ReceiveData(string PipeName, DelegateSerialData SerialDataReceiver, DelegateSerialStatus SerialClosedHandle) {
            NamedPipeServerStream DataPipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            DataPipeServer.WaitForConnection();

            SerialClosedHandle?.Invoke(PipeName, true);

            MemoryStream memoryStream = new();
            byte[] buffer = new byte[BUFFER_SIZE];

            try {
                while (DataPipeServer.IsConnected || !DataPipeServer.IsMessageComplete) {
                    memoryStream.SetLength(0);
                    do {
                        memoryStream.Write(buffer, 0, DataPipeServer.Read(buffer, 0, buffer.Length));
                    } while (!DataPipeServer.IsMessageComplete);

                    SerialDataReceiver.Invoke(memoryStream.ToArray());
                }

                DataPipeServer.Close();
            } catch (IOException) {
            } finally {
                DataPipeServer.Dispose();
                SerialClosedHandle?.Invoke(PipeName, false);
            }
        }
    }
}
