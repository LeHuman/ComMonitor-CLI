using System;
using System.IO;

namespace ComMonitor
{
    internal class FileLog
    {
        public bool timestamp { get; set; } = false;
        private readonly StreamWriter file;
        private const string FILENAME = "ComMonitor";

        public const long TicksPerMicrosecond = 10;
        public const long NanosecondsPerTick = 100;

        public FileLog(string path, bool singular)
        {
            try
            {
                int epoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                path = Path.Combine(path, $"{FILENAME}{(singular ? "" : $"_{epoch}")}.log");
                file = new StreamWriter(path);
            }
            catch (IOException)
            {
                return;
            }
        }

        public static long Nanoseconds()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond / TicksPerMicrosecond / NanosecondsPerTick;
        }

        public void Write(string msg)
        {
            if (timestamp)
                file.Write(Nanoseconds().ToString() + " ");
            file.Write(msg);
        }

        public void WriteLine(string msg)
        {
            if (timestamp)
                file.Write(Nanoseconds().ToString() + " ");
            file.WriteLine(msg);
        }

        public bool Available()
        {
            return file != null && file.BaseStream.CanWrite;
        }

        public void Flush()
        {
            file.Flush();
        }
    }
}