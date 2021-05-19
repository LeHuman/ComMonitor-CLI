using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComMonitor
{
    class FileLog {

        private readonly StreamWriter file;
        private const string FILENAME = "ComMonitor";

        public FileLog(string path, bool singular)
        {
            try
            {
                int epoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                file = new StreamWriter(Path.Combine(path, $"{FILENAME}{(singular ? "" : $"_{epoch}")}.log"));
            }
            catch (IOException)
            {
                return;
            }
            
        }

        public void Write(string msg) {
            file.Write(msg);
        }

        public void WriteLine(string msg) {
            file.WriteLine(msg);
        }

        public bool Available() {
            return file != null && file.BaseStream.CanWrite;
        }

        public void Flush() {
            file.Flush();
        }

    }
}
