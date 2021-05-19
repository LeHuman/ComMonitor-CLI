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
*   -w, --wait           If the port is not open when the console starts, wait for it to open
*   --timeout            (Default: 200) Set the number of retries for both -w -r options [Integer]
*   -c, --color          Disable changing color of console, may help with latency
*   -f, --frequency      (Default: 20) Manually Set the critical frequency of communication (Hz)
*   -m, --maxBytes       Set a max number of bytes to print for each line when not in Ascii mode [Integer]
*   --priority           Take priority of a port if inuse by another instance of ComMonitor
*   --help               Display help screen
*   --version            Display version information
*   --jsonPath           Point to a json file that contains a JSON object which maps strings to unique integers, allows non ascii modes to instead print out their matching string, option `m` and `jsonBlock` must be set, refer to JSON Mapping on README

Example:
> ComMonitor.exe COM6 19200 -rc -p Even --priority

**NOTE:** When selecting something on the console, such as with the mouse, the console might freeze. Hitting `Enter`, `Esc`, or `Right Mouse` will unfreeze it.
This can be disabled, however, this would also mean one would not be able to select text at all.

## User Input

Using the `--input` option, it enables the console app to take in input.

Ascii strings are sent as it, however, note that escape characters are not supported.

Hex strings, Binary strings, and Integer strings are all converted into little endian byte arrays of a maximum length 8. Integer strings are decoded as a signed long. These numerical types can be delimited by a space to send multiple numbers at a time.

Example:
> Input: 45684 456 56 4\
> Sending Bytes: 116,178\
> Sending Bytes: 200,1\
> Sending Bytes: 56\
> Sending Bytes: 4

**NOTE:** User input only works when actively connected to a port, input buffer is cleared when port disconnects.

**NOTE:** Currently, input is somewhat *experimental*

`--disableInputPrompt` might help if the console text gets garbled in anyway

Sending too much data at once might freeze up the serial port.

The `Insert` key can also be used to paste any text that is on the clipboard to be immediately sent. The `Right Mouse` button, by default on Windows console, can be used to just paste but not send.

## Special Prefixes
Strings with special prefixes ( not case sensitive ) are highlighted in the console

| Prefix    | Color   |
| --------- | ------- |
| _Default_ | White   |
| [Debug]   | Magenta |
| [Error]   | Red     |
| [Fatal]   | DarkRed |
| [Warn]    | Yellow  |
| [Info]    | Cyan    |

Every newline that is received is interpreted separately and colored accordingly

**NOTE:** Because each message gets searched for keywords (both in ascii and mapped mode), disabling color (-c) might help reduce latency if needed

## JSON Mapping

**NOTE:** Option `--jsonBlock` is not needed, WIP

Consequently, the only supported mapping is to a 8 byte block with the following structure

    |                 Bytes                 |
    |0   |1   |2   |3   |4   |5   |6   |7   |
    |    ID   |String ID|     uint32_t      |

The following is an example json to pass to `--jsonPath`

```json
[
    {
        "ID 0": 1,
        "Main state": 2,
        "Pin library": 3
    },
    {
        "Error!": 1,
        "Initalizing": 2,
        "Look at this! ->": 3
    }
]
```