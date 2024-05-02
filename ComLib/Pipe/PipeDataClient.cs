using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace Pipe {

    public class PipeDataClient(string PipeName, int MaxBytes, string MetaData) {
        public const int TIMEOUT_MS = 5000;
        public bool IsConnected { get => DataPipe != null && DataPipe.IsConnected; }

        private bool Started;
        private readonly string PipeName = PipeName, MaxBytes = MaxBytes.ToString(), MetaData = MetaData;
        private NamedPipeClientStream InfoPipe, DataPipe;
        private static readonly char[] Trimmed = ['\r', '\n'];

        public void Stop() {
            DataPipe?.Close();
            DataPipe?.Dispose();
        }

        public void SendData(byte[] Data) {
            if (IsConnected)
                try {
                    DataPipe.Write(Data, 0, Data.Length);
                } catch (IOException) {
                }
        }

        public void SendData(string msg) {
            SendData(Encoding.UTF8.GetBytes(msg.TrimEnd(Trimmed) + '\n'));
        }

        public bool Start() {
            if (Started)
                return true;
            try {
                Task.Factory.StartNew(WaitForInfoClient, TaskCreationOptions.LongRunning);
                Started = true;
                return true;
            } catch (Exception oEX) {
                Debug.WriteLine(oEX.Message);
            }
            return false;
        }

        private void WaitForInfoClient() { // TODO: instead, periodically check if any new pipes with INFO_PIPE_NAME are accepting input
            while (true) {
                try {
                    InfoPipe = new NamedPipeClientStream(PipeDataServer.INFO_PIPE_NAME);
                    InfoPipe.Connect(TIMEOUT_MS);
                    SendInfo();
                    WaitForReply();
                    InfoPipe.Dispose();
                    DataPipe = new NamedPipeClientStream(PipeName);
                    DataPipe.Connect(TIMEOUT_MS);
                    while (DataPipe.IsConnected) { }
                    Stop();
                } catch (SystemException) {
                }
            }
        }

        private void SendInfo() {
            byte[] Data = Encoding.UTF8.GetBytes(PipeName + ',' + MaxBytes + ',' + MetaData);
            InfoPipe.Write(Data, 0, Data.Length);
            InfoPipe.Flush();
        }

        private void WaitForReply() {
            byte[] buffer = new byte[PipeName.Length];
            Stopwatch Timeout = new();
            Timeout.Start();

            do {
                InfoPipe.Read(buffer, 0, buffer.Length);
            } while (Encoding.UTF8.GetString(buffer) != PipeName && Timeout.ElapsedMilliseconds < TIMEOUT_MS);

            if (Encoding.UTF8.GetString(buffer) != PipeName && Timeout.ElapsedMilliseconds >= TIMEOUT_MS) {
                throw new TimeoutException("Did not receive pipe reply in time");
            }
        }
    }
}
