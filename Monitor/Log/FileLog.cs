using System;
using System.IO;

namespace ComMonitor.Log {

    public static class FileLog {
        public const long TicksPerMicrosecond = 10;

        public const long NanosecondsPerTick = 100;
        public static bool Enabled { get => _Enabled; }
        public static bool TimestampEnabled { get => _Timestamp; set => _Timestamp = value; }

        private static StreamWriter file;
        private static bool _Timestamp, _Enabled;
        private const string FILENAME = "ComMonitor";

        public static void Flush() {
            if (_Enabled)
                file.Flush();
        }

        public static bool Available() {
            return file != null && file.BaseStream.CanWrite;
        }

        public static void Log(string msg) {
            if (!_Enabled)
                return;
            if (_Timestamp)
                file.Write(Nanoseconds().ToString() + " ");
            file.Write(msg);
        }

        public static void LogLine(string msg) {
            if (!_Enabled)
                return;
            if (_Timestamp)
                file.Write(Nanoseconds().ToString() + " ");
            file.WriteLine(msg);
        }

        public static void SetFile(string path, bool singular) {
            try {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                int epoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                path = Path.Combine(path, $"{FILENAME}{(singular ? "" : $"_{epoch}")}.log");
                file = new StreamWriter(path);
                _Enabled = Available();
            } catch (IOException) {
                throw new SystemException($"Unable to create or access logging directory: {path}");
            } catch (ArgumentException) {
                throw new ArgumentException($"Path is invalid for logging: {path}");
            }
        }

        public static long Nanoseconds() {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond / TicksPerMicrosecond / NanosecondsPerTick;
        }
    }
}
