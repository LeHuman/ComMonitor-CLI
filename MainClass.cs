using System;
using System.IO.Ports;
using System.IO;
using System.Collections.Generic;
using CommandLine;
using System.Threading;
using SerialCom;
using System.Diagnostics;

namespace ComMonitor.Main
{

    public class MainClass
    {

        #region defines
        private SerialClient _serialReader;
        private bool reconnect;
        private bool setColor;
        private string portName;
        private PipeServer priorityNotify;
        private bool priorityStop;
        private string connectStr;
        private string PriorityPipeName = "ComMonitorPriority";
        private static ConsoleColor Cfg = ConsoleColor.White;
        private static int retries = MAX_RETRY;
        private const int MAX_RETRY = 200; // We should give it a limit just in case

        Dictionary<string, ConsoleColor> logLevel = new Dictionary<string, ConsoleColor>{
            { "[DEBUG]", ConsoleColor.Magenta },
            { "[ERROR]", ConsoleColor.Red },
            { "[WARN]", ConsoleColor.Yellow },
            { "[INFO]", ConsoleColor.Cyan }
        };
        #endregion

        #region methods
        #region console
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
            if (setColor)
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

        void SerialDataReceived(object sender, DataStreamEventArgs e)
        {

            if (e.Data.Length == 0)
                return;

            string data = System.Text.Encoding.ASCII.GetString(e.Data);
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

        private void PriorityStop(string Reply)
        {
            try
            {
                string portMatch = Reply.TrimEnd('\0');
                if (portMatch.Equals(portName))
                    priorityStop = true;
            }
            catch (ArgumentOutOfRangeException oEX)
            {
                Debug.WriteLine(oEX.Message);
                return;
            }

        }

        #endregion

        public MainClass(string portName, int baudrate, Parity parity, int databits, StopBits stopbits, bool reconnect, bool setColor, int frequency, bool priority)
        {

            this.portName = portName;
            this.reconnect = reconnect;
            this.setColor = setColor;
            PriorityPipeName += portName;
            priorityNotify = new PipeServer();
            if (priority)
            {
                priorityNotify.Send(portName, PriorityPipeName, 250);
                Thread.Sleep(250);
            }
            priorityNotify.Listen(PriorityPipeName);
            priorityNotify.PipeMessage += PriorityStop;

            _serialReader = new SerialClient(portName, baudrate, parity, databits, stopbits, frequency);
            _serialReader.SerialDataReceived += SerialDataReceived;
            if (!_serialReader.PortAvailable())
            {
                throw new SerialException(string.Format("Unable to find port: {0}", portName));
            }
            connectStr = "Connecting to " + portName + " @ " + baudrate + "\np:" + parity + " d:" + databits + " s:" + stopbits + " cf:" + frequency + "\n";
        }

        public void Run()
        {
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
                        while (_serialReader.IsAlive())
                        {
                            Thread.Sleep(500);
                            if (priorityStop)
                                throw new SerialException("Another instance has taken priority over the current port");
                        }
                        ColorConsole(ConsoleColor.Red);
                        Console.WriteLine("\n-----[ Disconnect ]-----\n");
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

        static void Main(string[] args)
        {

            string portName = "COMx";
            int baudrate = 9600;
            Parity parity = Parity.None;
            int databits = 8;
            StopBits stopbits = StopBits.One;
            bool reconnect = false;
            bool setColor = true;
            bool priority = false;
            int frequency = 20;

            Parser parser = new Parser(with => { with.CaseInsensitiveEnumValues = true; with.AutoHelp = true; with.AutoVersion = true; with.HelpWriter = Console.Out; });
            var result = parser.ParseArguments<Options>(args);
            result.WithParsed(options =>
            {
                portName = options.portName.ToUpper();
                baudrate = options.baudRate;
                parity = options.setParity;
                databits = options.setDataBits;
                stopbits = options.setStopBits;
                reconnect = !options.reconnect;
                setColor = !options.setColor;
                frequency = options.frequency;
                priority = options.priority;
            });

            if (portName.Equals("COMx"))
                return;

            if (setColor)
            {
                Console.CancelKeyPress += delegate { Console.ResetColor(); };
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.ForegroundColor = Cfg;
            }

            try
            {
                MainClass app = new MainClass(portName, baudrate, parity, databits, stopbits, reconnect, setColor, frequency, priority);
                app.Run();
            }
            catch (Exception e)
            {
                if (setColor)
                    Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                //throw e;
            }
            finally
            {
                if (setColor)
                    Console.ForegroundColor = Cfg;
            }


        }
    }
}
