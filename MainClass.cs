using CommandLine;
using SerialCom;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;

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
        private static bool enableFileLogging = false;
        private static FileLog logger;

        private readonly Dictionary<string, ConsoleColor> logLevel = new Dictionary<string, ConsoleColor>{
            { "[DEBUG]", ConsoleColor.Magenta },
            { "[FATAL]", ConsoleColor.DarkRed },
            { "[ERROR]", ConsoleColor.Red },
            { "[WARN]", ConsoleColor.Yellow },
            { "[INFO]", ConsoleColor.Cyan }
        };

        #endregion defines

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

        private void ConsolePrintData(string str)
        {
            _ConsolePrintData(str, false);
        }

        private void ConsolePrintDataLine(string str)
        {
            _ConsolePrintData(str, true);
        }

        private void _ConsolePrintData(string str, bool newline)
        {
            if (str.Length > 0)
            {
                LogLevelColor(str.ToUpper()); // TODO: trim to last bracket to reduce text that is searched
                if (newline)
                    ConsolePrintLine(str);
                else
                    ConsolePrint(str);
                ColorConsole();
            }
        }

        private void ConsolePrintLine(string str)
        {
            Console.WriteLine(str);
            if (enableFileLogging)
                logger.WriteLine(str);
        }

        private void ConsolePrint(string str)
        {
            Console.Write(str);
            if (enableFileLogging)
                logger.Write(str);
        }

        private void ConsoleFlush()
        {
            if (enableFileLogging)
                logger.Flush();
        }

        #endregion Console Methods

        #region Data Interpreters

        private void AsciiDataReceived(object sender, DataStreamEventArgs e)
        {
            if (e.Data.Length == 0)
                return;

            string data = SerialType.getAscii(e.Data);
            if (data.IndexOf('\n') < data.Length - 1)
            {
                string[] lines = data.Replace('\r', '\0').Split('\n');
                foreach (string line in lines)
                {
                    ConsolePrintDataLine(line);
                }
            }
            else
            {
                ConsolePrintData(data);
            }
        }

        private void SerialDataReceived(object sender, DataStreamEventArgs e)
        {
            if (e.Data.Length == 0)
                return;
            ConsolePrintDataLine(dataFunction(e.Data));
        }

        private void SerialChunkedDataReceived(object sender, DataStreamEventArgs e)
        {
            Span<byte> rawData = e.Data.AsSpan();
            int remain = rawData.Length;
            int i = 0;
            while (remain >= maxBytes)
            {
                ConsolePrintDataLine(dataFunction(rawData.Slice(i, maxBytes).ToArray()));
                remain -= maxBytes;
                i += maxBytes;
            }
            if (remain > 0)
            {
                ConsolePrintDataLine(dataFunction(rawData.Slice(i).ToArray()));
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

        private List<byte> saveBuffer = new List<byte>();

        private void SerialMappedDataReceived(object sender, DataStreamEventArgs e)
        {
            if (e.Data.Length == 0)
                return;
            if (e.Data.Length % maxBytes != 0)
                ConsolePrintLine("WARN: Data may have dysynced, or badly formatted data was received");

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
                ConsolePrintData(GetMappedMessage(rawData.Slice(i, maxBytes)));
                remain -= maxBytes;
                i += maxBytes;
            }
            if (remain > 0)
            {
                saveBuffer.AddRange(rawData.Slice(i).ToArray());
            }
        }

        #endregion Data Interpreters

        #region Runtime Methods

        private void RetryWait(bool firstWait = false)
        {
            if (_serialReader.PortAvailable())
                return;
            ColorConsole(ConsoleColor.Blue);
            string waitMessage = firstWait ? $"Waiting for connection to {portName} " : $"Retrying to connect to {portName} ";
            int[] waitAnimTime = { 80, 40, 30, 30, 20, 20, 10, 20, 20, 30, 30, 40 };
            string[] waitAnim = { "        ", "-       ", "--      ", "---     ", "----    ", " ----   ", "  ----  ", "   ---- ", "    ----", "     ---", "      --", "       -" };
            do
            {
                for (int i = 0; i < waitAnimTime.Length; i++)
                {
                    Console.Write($"\r{waitMessage}{waitAnim[i]}");
                    Thread.Sleep(waitAnimTime[i]);
                    if (_serialReader.PortAvailable())
                        break;
                }
            } while (!_serialReader.PortAvailable());
            Console.Write("\r");
            Console.Write(string.Concat(Enumerable.Repeat(" ", waitMessage.Length + waitAnim[0].Length)));
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
            try
            {
                ConsoleFlush();
            }
            catch (Exception)
            {
            }
            throw new SerialException("Another instance has taken priority over the current port");
        }

        public void Run()
        {
            if (portName.Equals("COMxNULL"))
                return;

            ColorConsole(ConsoleColor.Yellow);
            ConsolePrintLine(connectStr);
            ColorConsole();
            while (true)
            {
                try
                {
                    if (_serialReader.OpenConn())
                    {
                        RetryReset();
                        ColorConsole(ConsoleColor.Green);
                        ConsolePrintLine("\r------[Connect]-------");
                        ColorConsole();
                        while (_serialReader.IsAlive())
                        {
                            Thread.Sleep(500);
                        }
                        ColorConsole(ConsoleColor.Red);
                        ConsolePrintLine("-----[Disconnect]-----");
                        ColorConsole();
                    }
                }
                catch (SerialException e)
                {
                    ConsolePrintLine(e.Message);
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

        #endregion Runtime Methods

        #region Exception Handlers

        private static void UnhandledExceptionTrapperColor(object sender, UnhandledExceptionEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Exception ex = (Exception)e.ExceptionObject;
            Console.WriteLine(ex.Message);
            Console.ForegroundColor = Cfg;
            Environment.Exit(0);
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            Console.WriteLine(ex.Message);
            Environment.Exit(0);
        }

        #endregion Exception Handlers

        #endregion Methods

        public MainClass(string[] args)
        {
            #region Argument Parser

            Parser parser = new Parser(with => { with.CaseInsensitiveEnumValues = true; with.AutoHelp = true; with.AutoVersion = true; with.HelpWriter = Console.Out; });
            var result = parser.ParseArguments<Options>(args);
            result.WithParsed(options =>
            {
                portName = options.PortName.ToUpper();
                baudrate = options.BaudRate;
                parity = options.SetParity;
                databits = options.SetDataBits;
                stopbits = options.SetStopBits;
                maxBytes = options.SetMaxBytes;
                hasMaxBytes = maxBytes > 0;
                reconnect = options.Reconnect;
                setColor = !options.SetColor;
                frequency = options.Frequency;
                priority = options.Priority;
                dataType = options.SetDataType;
                JSON_PATH = options.JsonPath;
                JSON_BLOCKING = options.JsonBlock;
                MAX_RETRY = options.RetryTimeout;
                retries = MAX_RETRY;
                waitForConn = options.Wait;
                mappedMode = options.JsonPath != null && maxBytes != 0 /*&& options.jsonBlock != null*/; // TODO: custom message block structure for matching strings
                logKeyword = setColor && (options.SetDataType == DataType.Ascii || mappedMode);
                if (options.Logging != "")
                {
                    string path = options.Logging;
                    if (Directory.Exists(path))
                    {
                        logger = new FileLog(path, options.SingleLogging);
                        enableFileLogging = logger.Available();
                    }
                    else
                    {
                        ConsolePrintLine($"Logging path does not exist: {path}");
                    }
                }
            });

            #endregion Argument Parser

            #region Load JSON data

            if (JSON_PATH != null)
            {
                Dictionary<long, string>[] maps = JSON.getDataMap(JSON_PATH);
                JSON_IDS = maps[0];
                JSON_STRINGS = maps[1];
            }

            #endregion Load JSON data

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

            #endregion Setup Color

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

            #endregion Priority Queue Setup

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

            #endregion Setup SerialClient

            connectStr = $"Connecting to {portName} @ {baudrate}\np:{parity} d:{databits} s:{stopbits} cf:{frequency} {(hasMaxBytes ? "j:" + maxBytes : "")}\n";
        }

        private static void Main(string[] args)
        {
            MainClass app = new MainClass(args);
            app.Run();
        }
    }
}