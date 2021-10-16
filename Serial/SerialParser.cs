using Log;
using MsgMap;
using System;
using System.Collections.Generic;
using Terminal;

namespace Serial
{
    internal class SerialParser
    {
        public static int MaxBytes { get; private set; } = 8;
        private static Func<byte[], string> dataFunction;

        public static EventHandler<DataStreamEventArgs> LoadParser(DataType dataType, int MaxBytes)
        {
            SerialParser.MaxBytes = MaxBytes;
            dataFunction = SerialType.getTypeFunction(dataType);

            if (dataType == DataType.Ascii)
            {
                return AsciiDataReceived;
            }
            else
            {
                if (dataType == DataType.Mapped)
                {
                    return SerialMappedDataReceived;
                }
                else if (MaxBytes > 0)
                {
                    return SerialChunkedDataReceived;
                }
                else
                {
                    return SerialDataReceived;
                }
            }
        }

        private static void AsciiDataReceived(object sender, DataStreamEventArgs e)
        {
            string data = SerialType.getAscii(e.Data);
            if (data.IndexOf('\n') < data.Length - 1)
            {
                string[] lines = data.Replace('\r', ' ').Split('\n');
                foreach (string line in lines)
                {
                    Term.WriteLine(line.Trim(), true);
                }
            }
            else
            {
                Term.Write(data, true);
            }
            FileLog.Flush();
        }

        private static void SerialDataReceived(object sender, DataStreamEventArgs e)
        {
            string msg = dataFunction(e.Data);
            Term.WriteLine(msg, true);
        }

        private static void SerialChunkedDataReceived(object sender, DataStreamEventArgs e)
        {
            Span<byte> rawData = e.Data.AsSpan();
            int remain = rawData.Length;
            int i = 0;
            while (remain >= MaxBytes)
            {
                Term.WriteLine(dataFunction(rawData.Slice(i, MaxBytes).ToArray()), true);
                remain -= MaxBytes;
                i += MaxBytes;
            }
            if (remain > 0)
            {
                Term.WriteLine(dataFunction(rawData.Slice(i).ToArray()), true);
            }
            FileLog.Flush();
        }

        private static List<byte> saveBuffer = new List<byte>();

        private static void SerialMappedDataReceived(object sender, DataStreamEventArgs e)
        {
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
            }
            else
            {
                rawData = e.Data.AsSpan();
            }

            int remain = rawData.Length;
            int i = 0;
            while (remain >= MaxBytes)
            {
                Term.Write(JSONMap.GetMappedMessage(rawData.Slice(i, MaxBytes)));
                remain -= MaxBytes;
                i += MaxBytes;
            }
            if (remain > 0)
            {
                saveBuffer.AddRange(rawData.Slice(i).ToArray());
            }
            FileLog.Flush();
        }
    }
}
