using ComMonitor.Log;
using ComMonitor.MsgMap;
using ComMonitor.Terminal;
using System;
using System.Collections.Generic;

namespace ComMonitor.Serial {

    public delegate void DelegateParsedData(string Message);

    public static class SerialParser {
        public static int MaxBytes { get; private set; } = 8;

        private static Func<byte[], string> dataFunction;
        private static DelegateParsedData ParsedDataListener;
        private static readonly List<byte> saveBuffer = [];

        private static void PrintMessage(string Message, bool Log = true) {
            Term.WriteLine(Message, Log);
            ParsedDataListener?.Invoke(Message);
        }

        private static void AsciiDataReceived(object sender, DataStreamEventArgs e) {
            string data = SerialType.GetAscii(e.Data);
            if (data.IndexOf('\n') < data.Length - 1) {
                string[] lines = data.Split('\n');
                foreach (string line in lines) {
                    PrintMessage(line.Trim('\r'));
                }
            } else {
                Term.Write(data, true);
            }
            FileLog.Flush();
        }

        private static void SerialDataReceived(object sender, DataStreamEventArgs e) {
            string msg = dataFunction(e.Data);
            PrintMessage(msg);
        }

        public static void SetParsedDataListener(DelegateParsedData ParsedDataListener) {
            SerialParser.ParsedDataListener = ParsedDataListener;
        }

        private static void SerialChunkedDataReceived(object sender, DataStreamEventArgs e) {
            Span<byte> rawData = e.Data.AsSpan();
            int remain = rawData.Length;
            int i = 0;
            while (remain >= MaxBytes) {
                PrintMessage(dataFunction(rawData.Slice(i, MaxBytes).ToArray()));
                remain -= MaxBytes;
                i += MaxBytes;
            }
            if (remain > 0) {
                PrintMessage(dataFunction(rawData[i..].ToArray()));
            }
            FileLog.Flush();
        }

        public static EventHandler<DataStreamEventArgs> LoadParser(DataType dataType, int MaxBytes) {
            SerialParser.MaxBytes = MaxBytes;
            dataFunction = SerialType.GetTypeFunction(dataType);

            if (dataType == DataType.Ascii) {
                return AsciiDataReceived;
            } else {
                if (dataType == DataType.Mapped) {
                    return SerialMappedDataReceived;
                } else if (MaxBytes > 0) {
                    return SerialChunkedDataReceived;
                } else {
                    return SerialDataReceived;
                }
            }
        }

        private static void SerialMappedDataReceived(object sender, DataStreamEventArgs e) {
            if (e.Data.Length % MaxBytes != 0)
                Term.WriteLine("WARN: Data may have dysynced, or badly formatted data was received");

            byte[] stichedData;
            Span<byte> rawData;

            if (saveBuffer.Count > 0) // There is part of a message that was uncomplete, prepend it to our span
            {
                stichedData = new byte[e.Data.Length + saveBuffer.Count];
                Buffer.BlockCopy(saveBuffer.ToArray(), 0, stichedData, 0, saveBuffer.Count);
                Buffer.BlockCopy(e.Data, 0, stichedData, saveBuffer.Count, e.Data.Length);
                rawData = stichedData.AsSpan();
            } else {
                rawData = e.Data.AsSpan();
            }

            int remain = rawData.Length;
            int i = 0;
            while (remain >= MaxBytes) {
                PrintMessage(JSONMap.GetMappedMessage(rawData.Slice(i, MaxBytes)), false);
                remain -= MaxBytes;
                i += MaxBytes;
            }
            if (remain > 0) {
                saveBuffer.AddRange(rawData[i..].ToArray());
            }
            FileLog.Flush();
        }
    }
}
