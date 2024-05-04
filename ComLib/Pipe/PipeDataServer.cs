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
        internal bool ServerThreadRun = true;

        private readonly DelegateSerialInfo InfoReceiver;
        private DelegateSerialStatus SerialStatusListener;
        private NamedPipeServerStream InfoPipeServer;
        private static readonly char[] InfoSplit = [','];
        private readonly Thread ServerThread;

        public PipeDataServer(DelegateSerialInfo InfoReceiver) {
            this.InfoReceiver = InfoReceiver;
            ServerThread = new Thread(RunThread)
            {
                Name = "Pipe Server Thread"
            };
        }

        public void SetStatusListener(DelegateSerialStatus SerialStatusListener) {
            this.SerialStatusListener = SerialStatusListener;
        }

        public bool Start() {
            try {
                InfoPipeServer = new NamedPipeServerStream(INFO_PIPE_NAME, PipeDirection.InOut, MAX_CONNECTIONS, PipeTransmissionMode.Message);
                ServerThreadRun = true;
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
            ServerThreadRun = false;
            using (NamedPipeClientStream npcs = new("ComInfoPipe")) {
                npcs.Connect(-1);
            }
            ServerThread.Join();
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

            bool cont = false;

            while (ServerThreadRun) {
                try {
                    if (!InfoPipeServer.IsConnected)
                        InfoPipeServer.WaitForConnection(); // TODO: use async version
                    memoryStream.SetLength(0);

                    do {
                        memoryStream.Write(buffer, 0, InfoPipeServer.Read(buffer, 0, buffer.Length));
                    } while (InfoPipeServer.IsMessageComplete == false);

                    string[] PipeData = Encoding.UTF8.GetString(memoryStream.ToArray()).Split(InfoSplit, 3);
                    if (PipeData.Length != 3)
                        continue;
                    SendReply(PipeData[0]);
                    cont = false;
                    Task.Run(() => { string[] _PipeData = PipeData; cont = true; ReceiveData(_PipeData[0], InfoReceiver.Invoke(_PipeData[0], int.Parse(_PipeData[1]), _PipeData[2]), SerialStatusListener); });
                    while (!cont) { }
                    InfoPipeServer.Disconnect();
                } catch (SystemException) {
                    InfoPipeServer?.Close();
                    InfoPipeServer?.Dispose();
                    try {
                        InfoPipeServer = new NamedPipeServerStream(INFO_PIPE_NAME, PipeDirection.InOut, MAX_CONNECTIONS, PipeTransmissionMode.Message);
                    } catch (Exception) {
                    }
                }
            }
        }

        private static void ReceiveData(string PipeName, DelegateSerialData SerialDataReceiver, DelegateSerialStatus SerialClosedHandle) {
            NamedPipeServerStream DataPipeServer = new(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message);
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
