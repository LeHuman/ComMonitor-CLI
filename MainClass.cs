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

        private static int MAX_RETRY = 200; // We should give it a limit just in case
        private static int retries = MAX_RETRY;
        private static int frequency = 20;
        private static int maxBytes = 0;

        private static bool hasMaxBytes = false;
        private static bool reconnect = false;
        private static bool setColor = true;
        private static bool logKeyword = true;
        private static bool priority = false;
        private static bool waitForConn = false;

        private string connectStr;
        private string PriorityPipeName = "ComMonitorPriority";
        private static SerialClient _serialReader;
        private static Func<byte[], string> dataFunction;
        private static PipeServer priorityNotify;
        private static ConsoleColor Cfg = ConsoleColor.White;
        readonly Dictionary<string, ConsoleColor> logLevel = new Dictionary<string, ConsoleColor>{
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

        private void LogLevelColor(string str)
        {
            if (logKeyword)
            {
                foreach (KeyValuePair<string, ConsoleColor> entry in logLevel)
                {
                    if (str.Contains(entry.Key)) // What is the performance impact of this vs StartsWith?
                    {
                        Console.ForegroundColor = entry.Value;
                        return;
                    }
                }
                Console.ForegroundColor = Cfg;
            }
        }

        private void ConsolePrint(string str)
        {
            _ConsolePrintData(str, false);
        }        
        private void ConsolePrintLine(string str)
        {
            _ConsolePrintData(str, true);
        }
        private void _ConsolePrintData(string str, bool newline)
        {
            if (str.Length > 0)
            {
                LogLevelColor(str.ToUpper()); // TODO: trim to last bracket to reduce text that is searched
                if (newline)
                    Console.WriteLine(str);
                else
                    Console.Write(str);
                ColorConsole();
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
                    ConsolePrintLine(line);
                }
            }
            else
            {
                ConsolePrint(data);
            }

        }

        void SerialDataReceived(object sender, DataStreamEventArgs e)
        {
            if (e.Data.Length == 0)
                return;
            ConsolePrintLine(dataFunction(e.Data));
        }

        void SerialChunkedDataReceived(object sender, DataStreamEventArgs e)
        {
            Span<byte> rawData = e.Data.AsSpan();
            int remain = rawData.Length;
            int i = 0;
            while (remain >= maxBytes)
            {
                ConsolePrintLine(dataFunction(rawData.Slice(i, maxBytes).ToArray()));
                remain -= maxBytes;
                i += maxBytes;
            }
            if (remain > 0)
            {
                ConsolePrintLine(dataFunction(rawData.Slice(i).ToArray()));
            }
        }

        public static string GetMappedMessage(Span<byte> data)
        {
            // TODO: Implement custom blocking of messages
            long ID_key = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(0, 2));
            long String_key = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(2, 2));
            long num = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(4, 4));
            string id;
            string str;
            if (!JSON_IDS.TryGetValue(ID_key, out id))
                id = "Bad ID";
            if (!JSON_STRINGS.TryGetValue(String_key, out str))
                str = "Bad String ID";
            return id + " " + str + " " + num + "\n";
        }

        List<byte> saveBuffer = new List<byte>();

        void SerialMappedDataReceived(object sender, DataStreamEventArgs e)
        {
            if (e.Data.Length == 0)
                return;
            if (e.Data.Length % maxBytes != 0)
                Console.WriteLine("WARN: Data may have dysynced, or badly formatted data was received");

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
            while (remain >= maxBytes)
            {
                ConsolePrint(GetMappedMessage(rawData.Slice(i, maxBytes)));
                remain -= maxBytes;
                i += maxBytes;
            }
            if (remain > 0)
            {
                saveBuffer.AddRange(rawData.Slice(i).ToArray());
            }
        }

        #endregion

        #region Runtime Methods
        private void RetryWait(bool firstWait = false)
        {
            ColorConsole(ConsoleColor.Blue);
            do
            {
                Console.Write(firstWait ? "\rWaiting for connection to {0} |" : "\rRetrying to connect to {0} |", portName);
                Thread.Sleep(100);
                Console.Write(firstWait ? "\rWaiting for connection to {0} \\" : "\rRetrying to connect to {0} \\", portName);
                Thread.Sleep(100);
                Console.Write(firstWait ? "\rWaiting for connection to {0} -" : "\rRetrying to connect to {0} -", portName);
                Thread.Sleep(100);
                Console.Write(firstWait ? "\rWaiting for connection to {0} /" : "\rRetrying to connect to {0} /", portName);
                Thread.Sleep(100);
            } while (!_serialReader.PortAvailable());
            Console.Write("\r");
            Console.Write(String.Concat(Enumerable.Repeat(" ", ("Waiting for connection to  /".Length + portName.Length))));
            Console.Write("\r");
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
                        Console.WriteLine("\r------[ Connect ]-------");
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
                if (!reconnect)
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
                reconnect = options.reconnect;
                setColor = !options.setColor;
                frequency = options.frequency;
                priority = options.priority;
                dataType = options.setDataType;
                JSON_PATH = options.jsonPath;
                JSON_BLOCKING = options.jsonBlock;
                MAX_RETRY = options.retryTimeout;
                retries = MAX_RETRY;
                waitForConn = options.wait;
                mappedMode = options.jsonPath != null && maxBytes != 0 /*&& options.jsonBlock != null*/; // TODO: custom message block structure for matching strings
                logKeyword = setColor && (options.setDataType == DataType.Ascii || mappedMode);
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
                if (waitForConn)
                    RetryWait(true);
                else
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
