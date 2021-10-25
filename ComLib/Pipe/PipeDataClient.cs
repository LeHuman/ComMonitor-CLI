using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace Pipe {

    public class PipeDataClient {
        public bool IsConnected { get => DataConnected && PipeClient != null && PipeClient.IsConnected; }

        private bool DataConnected;
        private string PipeName, MaxBytes, MetaData;

        private NamedPipeClientStream PipeClient;

        public PipeDataClient(string PipeName, int MaxBytes, string MetaData) {
            this.PipeName = PipeName;
            this.MaxBytes = MaxBytes.ToString();
            this.MetaData = MetaData;
        }

        public void Stop() {
            PipeClient?.Close();
            PipeClient?.Dispose();
            DataConnected = false;
        }

        public bool Start() {
            if (IsConnected)
                return true;
            try {
                Task.Run(WaitForInfoClient);
                return true;
            } catch (Exception oEX) {
                Debug.WriteLine(oEX.Message);
            }
            return false;
        }

        private static readonly char[] Trimmed = new char[] { '\r', '\n' };

        public void SendData(string msg) {
            SendData(Encoding.UTF8.GetBytes(msg.TrimEnd(Trimmed) + '\n'));
        }

        public void SendData(byte[] Data) {
            if (IsConnected)
                PipeClient.Write(Data, 0, Data.Length);
        }

        private void SendInfo() {
            byte[] Data = Encoding.UTF8.GetBytes(PipeName + ',' + MaxBytes + ',' + MetaData);
            _ = PipeClient.WriteAsync(Data, 0, Data.Length);
            PipeClient.Flush();
            PipeClient.WaitForPipeDrain();
        }

        private async Task WaitForInfoClient() {
            if (IsConnected)
                return;
            Stop();
            PipeClient = new(PipeDataServer.INFO_PIPE_NAME);
            await PipeClient.ConnectAsync();
            SendInfo();
            WaitForReply();
            Stop();
            PipeClient = new NamedPipeClientStream(PipeName);
            await PipeClient.ConnectAsync();
            DataConnected = true;
        }

        private void WaitForReply() {
            byte[] buffer = new byte[PipeName.Length];
            do {
                PipeClient.Read(buffer, 0, buffer.Length);
            } while (Encoding.UTF8.GetString(buffer) != PipeName);
        }
    }
}
