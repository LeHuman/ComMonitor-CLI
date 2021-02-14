using System;
using System.IO.Ports;
using System.IO;
using System.Collections.Generic;
using CommandLine;
using System.Threading;
using SerialCom;
using System.Diagnostics;
using System.Buffers.Binary;
using System.Linq;

namespace ComMonitor.Main
{

    public class MainClass
    {

        #region defines
        private static string portName = "COMxNULL";
        private static int baudrate = 9600;
        private static Parity parity = Parity.None;
        private static int databits = 8;
        private static StopBits stopbits = StopBits.One;
        private static DataType dataType = DataType.Ascii;

        private string JSON_PATH;
        private string JSON_BLOCKING;
        private static bool mappedMode = false;
        private static Dictionary<long, string> JSON_IDS;
        private static Dictionary<long, string> JSON_STRINGS;

        private const int MAX_RETRY = 200; // We should give it a limit just in case
        private static int retries = MAX_RETRY;
        private static int frequency = 20;
        private static int maxBytes = 0;

        private static bool hasMaxBytes = false;
        private static bool reconnect = false;
        private static bool setColor = true;
        private static bool logKeyword = true;
        private static bool priority = false;

        private string connectStr;
        private string PriorityPipeName = "ComMonitorPriority";
        private static SerialClient _serialReader;
        private static Func<byte[], string> dataFunction;
        private static PipeServer priorityNotify;
        private static ConsoleColor Cfg = ConsoleColor.White;

        Dictionary<string, ConsoleColor> logLevel = new Dictionary<string, ConsoleColor>{
            { "[DEBUG]", ConsoleColor.Magenta },
            { "[FATAL]", ConsoleColor.DarkRed },
            { "[ERROR]", ConsoleColor.Red },
            { "[WARN]", ConsoleColor.Yellow },
            { "[INFO]", ConsoleColor.Cyan }
        };
        #endregion

        #region Methods

        #region Console Methods
        private void ColorConsole(ConsoleColor color)
        {
            if (setColor)
                Console.ForegroundColor = color;
        }

        private void ColorConsole()
        {
            ColorConsole(Cfg);
        }

        private void LogLevelColor(string str) // IMPROVE: Add option to still detect keywords despite datatype
        {
            if (logKeyword)
            {
                foreach (KeyValuePair<string, ConsoleColor> entry in logLevel)
                {
                    if (str.StartsWith(entry.Key))
                    {
                        Console.ForegroundColor = entry.Value;
                        return;
                    }
                }
                Console.ForegroundColor = Cfg;
            }
        }
        #endregion

        #region Data Interpreters
        void AsciiDataReceived(object sender, DataStreamEventArgs e)
        {

            if (e.Data.Length == 0)
                return;

            string data = SerialType.getAscii(e.Data);
            if (data.IndexOf('\n') < data.Length - 1)
            {
                string[] lines = data.Replace('\r', '\0').Split('\n');
                foreach (string line in lines)
                {
                    if (line.Length > 0)
                    {
                        LogLevelColor(line.ToUpper());
                        Console.WriteLine(line);
                    }
                }
            }
            else
            {
                LogLevelColor(data.ToUpper());
                Console.Write(data);
            }

        }

        void SerialDataReceived(object sender, DataStreamEventArgs e)
        {
            if (e.Data.Length == 0)
                return;
            string prnt = dataFunction(e.Data);
            if (prnt.Length > 0)
                Console.WriteLine(prnt);
        }

        void SerialChunkedDataReceived(object sender, DataStreamEventArgs e)
        {
            int remain = e.Data.Length;
            int i = 0;
            while (remain > 0)
            {
                int copyBytes = Math.Min(remain, maxBytes);
                byte[] block = new byte[copyBytes];
                Array.Copy(e.Data, i, block, 0, copyBytes);
                i += maxBytes;
                string prnt = dataFunction(e.Data);
                if (prnt.Length > 0)
                    Console.WriteLine(prnt);
                remain -= maxBytes;
            }
        }

        public static string GetMappedMessage(Span<byte> data)
        {
            // TODO: Implement custom blocking of messages
            long ID_key = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(0, 2));
            long String_key = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(2, 2));
            long num = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(4, 4));
            string id = "Bad ID";
            string str = "Bad String ID";
            JSON_IDS.TryGetValue(ID_key, out id);
            JSON_STRINGS.TryGetValue(String_key, out str);
            return id + " " + str + " " + num + "\n";
        }
        private static List<byte> saveBuffer = new List<byte>();

        void SerialMappedDataReceived(object sender, DataStreamEventArgs e)
        {
            saveBuffer.AddRange(e.Data);
            Span<byte> rawData = new Span<byte>(saveBuffer.ToArray());
            saveBuffer.Clear();
            byte[] data = new byte[0];

            for (int j = 0; j < e.Data.Length; j++)
            {
                if (j / maxBytes >= 1)
                {
                    int b = j - maxBytes + 1;
                    data = rawData.Slice(b).ToArray();
                    saveBuffer.AddRange(rawData.Slice(0, b-1).ToArray());
                    break;
                }
            }

            byte[] block = new byte[maxBytes];
            int remain = data.Length;
            int i = 0;
            while (remain >= maxBytes)
            {
                Array.Copy(data, i, block, 0, maxBytes);
                i += maxBytes;
                Console.Write(GetMappedMessage(new Span<byte>(block)));
                remain -= maxBytes;
            }
        }

        #endregion

        #region Runtime Methods
        private void RetryWait()
        {
            ColorConsole(ConsoleColor.Blue);
            do
            {
                Console.Write("\rRetrying |");
                Thread.Sleep(100);
                Console.Write("\rRetrying \\");
                Thread.Sleep(100);
                Console.Write("\rRetrying -");
                Thread.Sleep(100);
                Console.Write("\rRetrying /");
                Thread.Sleep(100);
            } while (!_serialReader.PortAvailable());
            retries--;
            if (retries == 0)
            {
                throw new Exception("Max number of retries reached");
            }
        }
        private void RetryReset()
        {
            retries = MAX_RETRY;
        }
        private void PriorityStop()
        {
            throw new SerialException("Another instance has taken priority over the current port");
        }
        public void Run()
        {
            if (portName.Equals("COMxNULL"))
                return;

            ColorConsole(ConsoleColor.Yellow);
            Console.WriteLine(connectStr);
            ColorConsole();
            while (true)
            {
                try
                {
                    if (_serialReader.OpenConn())
                    {
                        RetryReset();
                        ColorConsole(ConsoleColor.Green);
                        Console.WriteLine("\r------[ Connect ]-------\n");
                        ColorConsole();
                        while (_serialReader.IsAlive())
                        {
                            Thread.Sleep(500);
                        }
                        ColorConsole(ConsoleColor.Red);
                        Console.WriteLine("\n-----[ Disconnect ]-----\n");
                        ColorConsole();
                    }
                }
                catch (IOException) { }
                catch (SerialException e)
                {
                    Console.WriteLine(e.Message);
                    _serialReader.Dispose();
                    return;
                }
                finally
                {
                    _serialReader.Dispose();
                }
                if (reconnect)
                    break;
                RetryWait();
            }
        }
        #endregion

        #region Exception Handlers
        static void UnhandledExceptionTrapperColor(object sender, UnhandledExceptionEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Exception ex = (Exception)e.ExceptionObject;
            Console.WriteLine(ex.Message);
            Console.ForegroundColor = Cfg;
            Environment.Exit(0);
        }
        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            Console.WriteLine(ex.Message);
            Environment.Exit(0);
        }
        #endregion

        #endregion

        public MainClass(string[] args)
        {

            #region Argument Parser
            Parser parser = new Parser(with => { with.CaseInsensitiveEnumValues = true; with.AutoHelp = true; with.AutoVersion = true; with.HelpWriter = Console.Out; });
            var result = parser.ParseArguments<Options>(args);
            result.WithParsed(options =>
            {
                portName = options.portName.ToUpper();
                baudrate = options.baudRate;
                parity = options.setParity;
                databits = options.setDataBits;
                stopbits = options.setStopBits;
                maxBytes = options.setMaxBytes;
                hasMaxBytes = maxBytes > 0;
                reconnect = !options.reconnect;
                setColor = !options.setColor;
                frequency = options.frequency;
                priority = options.priority;
                dataType = options.setDataType;
                JSON_PATH = options.jsonPath;
                JSON_BLOCKING = options.jsonBlock;
                mappedMode = options.jsonPath != null && maxBytes != 0 /*&& options.jsonBlock != null*/; // TODO: custom message block structure for matching strings
                logKeyword = setColor && options.setDataType == DataType.Ascii;
            });
            #endregion

            #region Load JSON data

            if (JSON_PATH != null)
            {
                Dictionary<long, string>[] maps = JSON.getDataMap(JSON_PATH);
                JSON_IDS = maps[0];
                JSON_STRINGS = maps[1];
            }

            #endregion

            #region Setup Color
            if (setColor)
            {
                Console.CancelKeyPress += delegate { Console.ResetColor(); };
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.ForegroundColor = Cfg;
                AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapperColor;
            }
            else
            {
                AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
            }
            #endregion

            #region Priority Queue Setup
            PriorityPipeName += portName;
            priorityNotify = new PipeServer();
            if (priority)
            {
                priorityNotify.Ping(PriorityPipeName);
                priorityNotify.ListenForPing(PriorityPipeName, 10);
            }
            else
            {
                priorityNotify.ListenForPing(PriorityPipeName);
            }
            priorityNotify.PipeConnect += PriorityStop;
            #endregion

            #region Setup SerialClient

            SerialType.setType(dataType);
            dataFunction = SerialType.getTypeDelegate();

            _serialReader = new SerialClient(portName, baudrate, parity, databits, stopbits, frequency);

            if (dataType == DataType.Ascii)
            {
                _serialReader.SerialDataReceived += AsciiDataReceived;
            }
            else
            {
                if (mappedMode)
                {
                    _serialReader.SerialDataReceived += SerialMappedDataReceived;
                }
                else if (hasMaxBytes)
                {
                    _serialReader.SerialDataReceived += SerialChunkedDataReceived;
                }
                else
                {
                    _serialReader.SerialDataReceived += SerialDataReceived;
                }

            }
            if (!_serialReader.PortAvailable())
            {
                if (portName.Equals("COMxNULL"))
                    return;
                throw new SerialException(string.Format("Unable to find port: {0}", portName));
            }

            #endregion

            connectStr = "Connecting to " + portName + " @ " + baudrate + "\np:" + parity + " d:" + databits + " s:" + stopbits + " cf:" + frequency + (hasMaxBytes ? " j:" + maxBytes : "") + "\n";
        }

        static void Main(string[] args)
        {
            MainClass app = new MainClass(args);
            app.Run();
        }
    }
}
