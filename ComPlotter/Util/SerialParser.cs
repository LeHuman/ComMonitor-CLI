using Log;
using MsgMap;
using Serial;
using System;
using System.Collections.Generic;
using Terminal;

namespace ComPlotter {

    internal class SerialParser {

        public delegate void DelegateParsedData(string Message);

        public delegate void DelegateSerialParser(byte[] Data);

        public DelegateSerialParser Parse { get; }

        private readonly int MaxBytes;
        private readonly DataType DataType;
        private readonly DelegateParsedData Printer;
        private DelegateParsedData ParsedDataListener;
        private readonly List<byte> SaveBuffer = new();
        private readonly Func<byte[], string> dataFunction;

        public SerialParser(DelegateParsedData Printer, DataType DataType, int MaxBytes) {
            this.Printer = Printer;
            this.MaxBytes = MaxBytes;
            this.DataType = DataType;
            dataFunction = SerialType.getTypeFunction(DataType);
            Parse = LoadParser();
        }

        private DelegateSerialParser LoadParser() {
            if (DataType == DataType.Ascii) {
                return AsciiDataReceived;
            } else {
                if (DataType == DataType.Mapped) {
                    return SerialMappedDataReceived;
                } else if (MaxBytes > 0) {
                    return SerialChunkedDataReceived;
                } else {
                    return SerialDataReceived;
                }
            }
        }

        public void SetParsedDataListener(DelegateParsedData ParsedDataListener) {
            this.ParsedDataListener = ParsedDataListener;
        }

        private void PrintMessage(string Message, bool Log = true) {
            Printer.Invoke(Message);
            ParsedDataListener?.Invoke(Message);
        }

        private void AsciiDataReceived(byte[] Data) {
            string data = SerialType.getAscii(Data);
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

        private void SerialDataReceived(byte[] Data) {
            string msg = dataFunction(Data);
            PrintMessage(msg);
        }

        private void SerialChunkedDataReceived(byte[] Data) {
            Span<byte> rawData = Data.AsSpan();
            int remain = rawData.Length;
            int i = 0;
            while (remain >= MaxBytes) {
                PrintMessage(dataFunction(rawData.Slice(i, MaxBytes).ToArray()));
                remain -= MaxBytes;
                i += MaxBytes;
            }
            if (remain > 0) {
                PrintMessage(dataFunction(rawData.Slice(i).ToArray()));
            }
            FileLog.Flush();
        }

        private void SerialMappedDataReceived(byte[] Data) {
            if (Data.Length % MaxBytes != 0)
                Term.WriteLine("WARN: Data may have dysynced, or badly formatted data was received");

            byte[] stichedData;
            Span<byte> rawData;

            if (SaveBuffer.Count > 0) // There is part of a message that was uncomplete, prepend it to our span
            {
                stichedData = new byte[Data.Length + SaveBuffer.Count];
                Buffer.BlockCopy(SaveBuffer.ToArray(), 0, stichedData, 0, SaveBuffer.Count);
                Buffer.BlockCopy(Data, 0, stichedData, SaveBuffer.Count, Data.Length);
                rawData = stichedData.AsSpan();
            } else {
                rawData = Data.AsSpan();
            }

            int remain = rawData.Length;
            int i = 0;
            while (remain >= MaxBytes) {
                PrintMessage(JSONMap.GetMappedMessage(rawData.Slice(i, MaxBytes)), false);
                remain -= MaxBytes;
                i += MaxBytes;
            }
            if (remain > 0) {
                SaveBuffer.AddRange(rawData.Slice(i).ToArray());
            }
        }
    }
}
