# ComMonitor

A Console application to monitor ascii data over a serial port

## Requirements

* Microsoft .NET Framework 4.5
* An open port to read from

## Usage

Arguments are not case sensitive

*   portName (pos. 0)    Required. Set the port name to connect to
*   baudRate (pos. 1)    Required. Set the baud rate
*   -p, --parity         (Default: None) Set the parity mode. [None|Odd|Even|Mark|Space]
*   -d, --databits       (Default: 8) The maximum number of bytes to process. [5-8]
*   -s, --stopbits       (Default: One) Set the number of stop bits. [None|One|Two|OnePointFive]
*	-t, --type           (Default: Ascii) Set the type of data to receive. [(A)scii|(H)ex|(D)ecimal|(B)inary]
*   -r, --retry          Keep console open and wait for port to reopen
*   -c, --color          Disable changing color of console
*   -f, --frequency      (Default: 20) Manually Set the critical frequency of communication (Hz)
* --priority             Take priority of a port if inuse by another instance of ComMonitor
*   --help               Display help screen
*   --version            Display version information

###   Example
`ComMonitor.exe COM6 19200 -rc -p Even --priority`

###   Special Prefixes
Strings with special prefixes ( not case sensitive ) are highlighted in the console

| Prefix        | Color         |
| ------------- |---------------|
| _Default_     | White         |
| [Debug]       | Magenta       |
| [Error]       | Red           |
| [Warn]        | Yellow        |
| [Info]        | Cyan          |

Every newline that is received is interpreted separately and colored accordingly
