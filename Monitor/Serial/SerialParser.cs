using ComMonitor.Log;
using ComMonitor.MsgMap;
using ComMonitor.Terminal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Theraot.Collections;

namespace ComMonitor.Serial {

    public static class SerialParser {
        public static int MaxBytes { get; private set; } = 8;

        private static Func<byte[], string> dataFunction;

        //public static Action<string, bool> DataParsedListener { get; set; }
        private static readonly List<byte> saveBuffer = [];

        private static readonly char[] newlineChars = ['\n', '\r'];

        //private static void DataParsed(string message, bool newline = true) {
        //    DataParsedListener?.Invoke(message, newline);
        //}

        private static string ParseASCII(byte[] data) {
            StringBuilder fnl = new(data.Length + 1);
            string message = SerialType.GetAscii(data);

            fnl.Append(message);

            //int newlineIndex = message.IndexOf('\n');
            //if (newlineIndex >= 0) {
            //    string[] lines = message.Split(newlineChars, StringSplitOptions.RemoveEmptyEntries);
            //    foreach (string line in lines) {
            //        DataParsed(line);
            //    }
            //} else {
            //    DataParsed(message, false);
            //}

            FileLog.Flush();
            return fnl.ToString();
        }

        private static string ParseGeneral(byte[] data) {
            StringBuilder fnl = new(data.Length + 1);
            string msg = dataFunction(data);

            fnl.AppendLine(msg);
            return fnl.ToString();
        }

        private static string ParseChunkedSerial(byte[] data) {
            StringBuilder fnl = new(data.Length + 1);
            Span<byte> rawData = data.AsSpan();
            int remain = rawData.Length;
            int i = 0;

            while (remain >= MaxBytes) {
                fnl.AppendLine(dataFunction(rawData.Slice(i, MaxBytes).ToArray()));
                remain -= MaxBytes;
                i += MaxBytes;
            }

            if (remain > 0) {
                fnl.AppendLine(dataFunction(rawData[i..].ToArray()));
            }

            FileLog.Flush();
            return fnl.ToString();
        }

        private static string ParseMappedData(byte[] data) {
            StringBuilder fnl = new(data.Length + 1);

            if (data.Length % MaxBytes != 0)
                fnl.AppendLine("[WARN] Data may have desynced, or badly formatted data was received");// IMPROVE: Don't send as data

            byte[] stichedData;
            Span<byte> rawData;

            if (saveBuffer.Count > 0) // There is part of a message that was incomplete, pre-pend it to our span
            {
                stichedData = new byte[data.Length + saveBuffer.Count];
                Buffer.BlockCopy(saveBuffer.ToArray(), 0, stichedData, 0, saveBuffer.Count);
                Buffer.BlockCopy(data, 0, stichedData, saveBuffer.Count, data.Length);
                rawData = stichedData.AsSpan();
            } else {
                rawData = data.AsSpan();
            }

            int remain = rawData.Length;
            int i = 0;
            while (remain >= MaxBytes) {
                fnl.AppendLine(JSONMap.GetMappedMessage(rawData.Slice(i, MaxBytes)));
                remain -= MaxBytes;
                i += MaxBytes;
            }
            if (remain > 0) {
                saveBuffer.AddRange(rawData[i..].ToArray());
            }

            FileLog.Flush();
            return fnl.ToString();
        }

        public static Func<byte[], string> ObtainParser(DataType dataType, int MaxBytes) {
            SerialParser.MaxBytes = MaxBytes;
            dataFunction = SerialType.GetTypeFunction(dataType);

            if (dataType == DataType.Ascii) {
                return ParseASCII;
            } else {
                if (dataType == DataType.Mapped) {
                    return ParseMappedData;
                } else if (MaxBytes > 0) {
                    return ParseChunkedSerial;
                } else {
                    return ParseGeneral;
                }
            }
        }
    }
}
