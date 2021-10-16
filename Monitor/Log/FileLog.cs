using System;
using System.IO;

namespace Log
{
    internal static class FileLog
    {
        private static StreamWriter file;
        public static bool Enabled = false;
        public static bool Timestamp { get; set; } = false;
        private const string FILENAME = "ComMonitor";

        public const long TicksPerMicrosecond = 10;
        public const long NanosecondsPerTick = 100;

        public static void SetFile(string path, bool singular)
        {
            try
            {
                if (!Directory.Exists(path))
                    throw new IOException();

                int epoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                path = Path.Combine(path, $"{FILENAME}{(singular ? "" : $"_{epoch}")}.log");
                file = new StreamWriter(path);
                Enabled = Available();
            }
            catch (IOException)
            {
                if (path.Length != 0)
                    throw new SystemException($"Logging path does not exist: {path}");
                return;
            }
        }

        public static long Nanoseconds()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond / TicksPerMicrosecond / NanosecondsPerTick;
        }

        public static void Log(string msg)
        {
            if (!Enabled)
                return;
            if (Timestamp)
                file.Write(Nanoseconds().ToString() + " ");
            file.Write(msg);
        }

        public static void LogLine(string msg)
        {
            if (!Enabled)
                return;
            if (Timestamp)
                file.Write(Nanoseconds().ToString() + " ");
            file.WriteLine(msg);
        }

        public static bool Available()
        {
            return file != null && file.BaseStream.CanWrite;
        }

        public static void Flush()
        {
            if (Enabled)
                file.Flush();
        }

        public static void EnableTimeStamp(bool enable)
        {
            Timestamp = enable;
        }
    }
}
