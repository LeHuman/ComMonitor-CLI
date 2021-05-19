using CommandLine;
using System.IO.Ports;

namespace ComMonitor.Main
{
    internal class Options
    {
        [Value(0, MetaName = "portName", Required = true, HelpText = "Set the port name to connect to.\nEx. COM6")]
        public string PortName { get; set; }

        [Value(1, MetaName = "baudRate", Required = true, HelpText = "Set the baud rate.\nEx. 9600")]
        public int BaudRate { get; set; }

        [Option('p', "parity", Required = false, HelpText = "Set the parity mode. [None|Odd|Even|Mark|Space]", Default = Parity.None)]
        public Parity SetParity { get; set; }

        [Option('d', "databits", HelpText = "The maximum number of bytes to process. [5-8]", Default = 8)]
        public int SetDataBits { get; set; }

        [Option('s', "stopbits", HelpText = "Set the number of stop bits. [None|One|Two|OnePointFive]", Default = StopBits.One)]
        public StopBits SetStopBits { get; set; }

        [Option('t', "type", HelpText = "Set the type of data to receive. [(A)scii|(H)ex|(D)ecimal|(B)inary]", Default = DataType.Ascii)]
        public DataType SetDataType { get; set; }

        [Option('m', "maxBytes", HelpText = "Set a max number of bytes to print for each line when not in Ascii mode [Integer]")]
        public int SetMaxBytes { get; set; }

        [Option('r', "retry", HelpText = "After connecting, if the port closes, keep the console open and wait for the port to reopen")]
        public bool Reconnect { get; set; }

        [Option('w', "wait", HelpText = "If the port is not open when the console starts, wait for it to open")]
        public bool Wait { get; set; }

        [Option("timeout", HelpText = "Set the number of retries for both -w -r options", Default = 200)]
        public int RetryTimeout { get; set; }

        [Option('c', "color", HelpText = "Disable console color, may help with latency")]
        public bool SetColor { get; set; }

        [Option("priority", HelpText = "Take priority of a port if another instance of ComMonitor has it open ( Does not apply to any other app )")]
        public bool Priority { get; set; }

        [Option('f', "frequency", HelpText = "Manually Set the critical frequency of communication (Hz)", Default = 20)]
        public int Frequency { get; set; }

        [Option('l', "log", HelpText = "Enable logging to a file [Valid Path]", Default = "")]
        public string Logging { get; set; }

        [Option("singleLog", HelpText = "If Logging is enabled, use a single file for logging, overwriting it each time", Default = false)]
        public bool SingleLogging { get; set; }

        [Option("jsonPath", HelpText = "Point to a json file that contains a JSON object which maps strings to unique integers, allows non ascii modes to instead print out their matching string, option `m` and `jsonBlock` must be set, refer to readme")]
        public string JsonPath { get; set; }

        [Option("jsonBlock", HelpText = "If jsonPath is set, setup how each message should be interpreted, refer to readme, WIP not needed")]
        public string JsonBlock { get; set; }
    }
}