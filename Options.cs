using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace ComMonitor.Main
{
    class Options
    {

        [Value(0,MetaName ="portName",Required = true, HelpText = "Set the port name to connect to.\nEx. COM6")]
        public string portName { get; set; }

        [Value(1, MetaName = "baudRate",Required = true, HelpText = "Set the baud rate.\nEx. 9600")]
        public int baudRate { get; set; }

        [Option('p', "parity", Required = false, HelpText = "Set the parity mode. [None|Odd|Even|Mark|Space]", Default = Parity.None)]
        public Parity setParity { get; set; }

        [Option('d', "databits", HelpText = "The maximum number of bytes to process. [5-8]", Default =8)]
        public int setDataBits { get; set; }

        [Option('s', "stopbits", HelpText = "Set the number of stop bits. [None|One|Two|OnePointFive]", Default = StopBits.One)]
        public StopBits setStopBits { get; set; }

        [Option('t', "type", HelpText = "Set the type of data to receive. [(A)scii|(H)ex|(D)ecimal|(B)inary]", Default = DataType.Ascii)]
        public DataType setDataType { get; set; }

        [Option('r', "retry", HelpText = "If port closes, keep console open and wait for the port to reopen")]
        public bool reconnect { get; set; } 

        [Option('c', "color", HelpText = "Disable console color")]
        public bool setColor { get; set; }

        [Option('f', "frequency", HelpText = "Manually Set the critical frequency of communication (Hz)", Default = 20)]
        public int frequency { get; set; }

        [Option("priority", HelpText = "Take priority of a port if another instance of ComMonitor has it open ( Does not apply to any other app )")]
        public bool priority { get; set; }

    }
}
