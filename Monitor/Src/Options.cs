﻿using CommandLine;
using ComMonitor.Serial;
using System.IO.Ports;

namespace ComMonitor.Main {

    internal class Options {

        [Value(0, MetaName = "portName", Required = false, HelpText = "Set the port name to connect to.\nEx. COM6\nIf no value is given, a port selection screen appears unless --disablePortScreen is set.")]
        public string PortName { get; set; }

        [Value(1, MetaName = "baudRate", Required = false, HelpText = "Set the baud rate.\nEx. 9600\nDefaults to 9600.")]
        public int? BaudRate { get; set; }

        [Option('p', "parity", Required = false, HelpText = "Set the parity mode. [None|Odd|Even|Mark|Space]", Default = Parity.None)]
        public Parity SetParity { get; set; }

        [Option('d', "databits", HelpText = "The maximum number of bytes to process. [5-8]", Default = 8)]
        public int SetDataBits { get; set; }

        [Option('s', "stopbits", HelpText = "Set the number of stop bits. [None|One|Two|OnePointFive]", Default = StopBits.One)]
        public StopBits SetStopBits { get; set; }

        [Option('t', "type", HelpText = "Set the type of data to receive. [(A)scii|(H)ex|(D)ecimal|(B)inary|(M)apped]", Default = DataType.Ascii)]
        public DataType SetDataType { get; set; }

        [Option('m', "maxBytes", HelpText = "Set a max number of bytes to print for each line when not in Ascii mode [Integer]", Default = 8)]
        public int SetMaxBytes { get; set; }

        [Option('r', "retry", HelpText = "After connecting, if the port closes, keep the console open and wait for the port to reopen")]
        public bool Reconnect { get; set; }

        [Option('R', "retryDelay", HelpText = "Pass a value in milliseconds to delay reconnecting. Helpful if reconnecting immediately causes issues. Functions as -r if the value passed is > 0.", Default = 0)]
        public int ReconnectDelay { get; set; }

        [Option('D', "dtr", HelpText = "Disable the Data Terminal Ready (DTR) signal during serial communication.")]
        public bool DisableDtr { get; set; }

        [Option('w', "wait", HelpText = "If the port is not open when the console starts, wait for it to open.")]
        public bool Wait { get; set; }

        [Option("retries", HelpText = "Set the number of retries for both -w -r options. Negative values means no limit.\nNOTE: Counter gets reset with each successful reconnection.", Default = -1)]
        public int MaxRetries { get; set; }

        [Option("timeout", HelpText = "Set the number of milliseconds before a time-out occurs when writing to serial", Default = -1)]
        public int WaitTimeout { get; set; }

        [Option('c', "color", HelpText = "Disable console color, may help with latency.", Default = false)]
        public bool SetColor { get; set; }

        [Option('a', "disableAnimation", HelpText = "Disable any text animation used and any text output that is not just a basic string. NOTE: Does not affect input prompt, see option disableInputPrompt.", Default = false)]
        public bool DisableAnimation { get; set; }

        [Option("priority", HelpText = "Take priority of a port if another instance of ComMonitor has it open ( Does not apply to any other app )", Default = false)]
        public bool Priority { get; set; }

        [Option('l', "log", HelpText = "Enable logging to a file [Valid Directory Path]")]
        public string Logging { get; set; }

        [Option('g', "graph", HelpText = "Graph incoming data using ComPlotter, refer to readme.", Default = false)]
        public bool PlotData { get; set; }

        [Option('i', "input", HelpText = "Enable input when connected to a port, refer to readme for formatting [(A)scii|(H)ex|(D)ecimal|(B)inary]", Default = DataType.None)]
        public DataType EnableInput { get; set; }

        [Option('e', "extendedInput", HelpText = "When input is in Ascii mode, expand escaped characters such as \\n", Default = false)]
        public bool ExpandInput { get; set; }

        [Option("first", HelpText = "Connect to the first available port when no portName is set.", Default = false)]
        public bool UseFirstFound { get; set; }

        [Option("disablePortScreen", HelpText = "Disable the selection screen from appearing when no option is given to portName", Default = false)]
        public bool DisablePortScreen { get; set; }

        [Option("graphPath", HelpText = "Define the path for ComPlotter, used by the -g flag", Default = "ComPlotter.exe")]
        public string PlotterPath { get; set; }

        [Option("serialPipe", HelpText = "Enable the underlying serial pipe for other .Net applications to use, refer to readme. Note that this is used by ComPlotter when the -g flag is used", Default = false)]
        public bool EnableSerialPipe { get; set; }

        [Option("logTime", HelpText = "If Logging is enabled, log with a nanosecond timestamp", Default = false)]
        public bool LogTime { get; set; }

        [Option("singleLog", HelpText = "If Logging is enabled, use a single file for logging, overwriting it each time", Default = false)]
        public bool SingleLogging { get; set; }

        [Option("disableInputPrompt", HelpText = "Disable the prompt that appears when inputting data, recommended to paste data instead.\n NOTE: On windows, right clicking a terminal with text in the clipboard should paste.", Default = false)]
        public bool DisableInputPrompt { get; set; }

        [Option("jsonPath", HelpText = "Point to a json file that contains a JSON object which maps strings to unique integers, allows non ascii modes to instead print out their matching string, option `m` and `jsonBlock` must be set, refer to readme")]
        public string JsonPath { get; set; }

        [Option("jsonBlock", HelpText = "If jsonPath is set, setup how each message should be interpreted, refer to readme, WIP not needed")]
        public string JsonBlock { get; set; }
    }
}
