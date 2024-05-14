<!-- PROJECT: ComMonitor -->
<!-- TITLE: ComMonitor -->
<!-- FONT: IBM Plex -->
<!-- KEYWORDS: Serial, USB -->
<!-- LANGUAGES: C# -->
<!-- TECHNOLOGY: .NET -->
<!-- STATUS: Active -->

![Logo](Monitor/Resources/MonitorPngs/COM_Monitor_Icon256.png)

# ComMonitor

[About](#about) - [Key Features](#key-features) - [Usage](#usage) - [Related](#related) - [License](#license)

## Status

**`Active`** - *Actively being updated with fixes and new features.*

## About
<!-- DESCRIPTION START -->
A .NET console application to monitor data over a serial port. Has options for logging and even plotting data using a companion GUI app.
<!-- DESCRIPTION END -->

<!-- TODO: Test this -->
**NOTE:** This app has been mainly been tested and used on Windows, but should technically work on anything that has a .NET runtime.

## Key Features

- CLI app
  - This app runs entirely in a terminal, allows for better integration with other setups versus GUI based apps.

## Usage

### Requirements

- Microsoft .NET 8.0 or .NET Framework 4.8
  - Different release versions available, including contained
- An open serial port to read from

### Arguments

Arguments are not case sensitive.

- portName (pos. 0)
  - Set the port name to connect to.
    - Ex. COM6
  - If no value is given, a port selection screen appears unless `--disablePortScreen` is set.

- baudRate (pos. 1)
  - Set the baud rate.
    - Ex. 9600
  - Defaults to `9600`.

- `-p`, `--parity` [`None`|`Odd`|`Even`|`Mark`|`Space`]
  - (Default: `None`)
  - Set the parity mode.

- `-d`, `--databits` [`5`-`8`]
  - (Default: `8`)
  - The maximum number of bytes to process.

- `-s`, `--stopbits` [`None`|`One`|`Two`|`OnePointFive`]
  - (Default: `One`)
  - Set the number of stop bits.

- `-t`, `--type` [`(A)scii`|`(H)ex`|`(D)ecimal`|`(B)inary`|`(M)apped`]
  - (Default: `Ascii`)
  - Set the type of data to receive.

- `-m`, `--maxBytes` [Integer]
  - (Default: 8) Set a max number of bytes to print for each line when not in Ascii mode

- `-r`, `--retry`
  - After connecting, if the port closes, keep the console open and wait for the port to reopen

- `-R`, `--retryDelay` [Integer]
  - (Default: 0)
  - Pass a value in milliseconds to delay reconnecting. Helpful if reconnecting immediately causes issues. Functions as `-r` if the value passed is > 0.

- `-D`, `--dtr`
  - Disable the Data Terminal Ready (DTR) signal during serial communication.

- `-w`, `--wait`
  - If the port is not open when the console starts, wait for it to open.

- `--retries` [Integer]
  - (Default: -1)
  - Set the number of retries for both `-w` `-r` options. Negative values means no limit.
  - NOTE: Counter gets reset with each successful reconnection.

- `--timeout` [Integer]
  - (Default: -1)
  - Set the number of milliseconds before a time-out occurs when writing to serial

- `-c`, `--color`
  - Disable console color, may help with latency.

- `-a`, `--disableAnimation`
  - Disable any text animation used and any text output that is not just a basic string.
  - NOTE: Does not affect input prompt, see option `--disableInputPrompt`.

- `--priority`
  - Take priority of a port if another instance of ComMonitor has it open.
  - NOTE: Does not apply to any other app.

- `-l`, `--log` [Valid Directory Path]
  - Enable logging to a file

- `-g`, `--graph`
  - Graph incoming data using ComPlotter, refer to readme.

- `-i`, `--input` [`(A)scii`|`(H)ex`|`(D)ecimal`|`(B)inary`]
  - (Default: None)
  - Enable input when connected to a port, refer to readme for formatting.

- `-e`, `--extendedInput`
  - When input is in Ascii mode, expand escaped characters such as \n

- `--first`
  - Connect to the first available port when no portName is set.

- `--disablePortScreen`
  - Disable the selection screen from appearing when no option is given to portName.

- `--graphPath`
  - (Default: ComPlotter.exe)
  - Define the path for ComPlotter, used by the `-g` flag.

- `--serialPipe`
  - Enable the underlying serial pipe for other .NET applications to use, refer to readme. Note that this is used by ComPlotter when the `-g` flag is used

- `--logTime`
  - If Logging is enabled, log with a nanosecond timestamp.

- `--singleLog`
  - If Logging is enabled, use a single file for logging, overwriting it each time.

- `--disableInputPrompt`
  - Disable the prompt that appears when inputting data, recommended to paste data instead.
  - NOTE: On windows, right clicking a terminal with text in the clipboard should paste.

<!-- - `--jsonPath`
  - Point to a json file that contains a JSON object which maps strings to unique integers, allows non ascii modes to instead print out their matching string, option `-m` and `--jsonBlock` must be set, refer to readme. -->

<!-- - `--jsonBlock`
  - If jsonPath is set, setup how each message should be interpreted, refer to readme, WIP not needed. -->

- `--help`                    Display this help screen.

- `--version`                 Display version information.

Example:
> ComMonitor.exe COM9 115200 --priority -tH -r -w -l"C:\tmp\LogFiles" --singleLog --input D

<!-- **NOTE:** When using the ASCII data type, `\r` characters are replaced with a space. -->

**NOTE:** On Windows, when selecting something on the console, such as with the mouse, the console might freeze. Hitting `Enter`, `Esc`, or `Right Mouse` will unfreeze it.
This can be disabled, however, this would also mean one would not be able to select text at all.

### User Input

Using the `--input` option, it enables the console app to take in input.

ASCII strings are sent as is, however, escape characters are only converted when using the `-e` flag alongside ASCII mode.

Hex strings, Binary strings, and Integer strings are all converted into little endian byte arrays of a maximum length 8. Integer strings are decoded as a signed long. These numerical types can be delimited by a space to send multiple numbers at a time.

Example:
> Input: 45684 456 56 4\
> Sending Bytes: 116,178\
> Sending Bytes: 200,1\
> Sending Bytes: 56\
> Sending Bytes: 4

**NOTE:** User input only works when actively connected to a port, input buffer is cleared when port disconnects.

**NOTE:** User input is still experimental.

- `--disableInputPrompt` might help if the console text gets garbled in anyway
- Sending too much data at once might freeze up the serial port.
  - Until asynchronous writes are implemented, setting `--timeout` can help discern whether writing is what is freezing the app.

The `Insert` key can also be used to paste any text that is on the clipboard to be immediately sent. The `Right Mouse` button, by default on a Windows terminal, can be used to just paste but not send.

### Special Prefixes

Strings with special prefixes ( not case sensitive ) are highlighted in the console

| Prefix    | Color   |
| --------- | ------- |
| *Default* | White   |
| [Debug]   | Magenta |
| [Error]   | Red     |
| [Fatal]   | DarkRed |
| [Warn]    | Yellow  |
| [Info]    | Cyan    |

Every newline that is received is interpreted separately and colored accordingly

**NOTE:** Because the incoming stream of data is continually searched for keywords (both in ascii and mapped mode), disabling color `-c` helps reduce latency if needed.

<!-- TODO: Mapping documentation -->
<!-- ### JSON Mapping WIP

**NOTE:** Option `--jsonBlock` is not needed, WIP

**NOTE:** Option `-t` Should be set to `Mapped` and a valid json map should be passed to `--jsonPath`

The only supported mapping is to an 8 byte block with the following structure

|                 Bytes                 |
|0   |1   |2   |3   |4   |5   |6   |7   |
|    ID   |String ID|     uint32_t      |

The following is an example json to pass to `--jsonPath`

``` json
[
    {
        "ID 0": 1,
        "Main state": 2,
        "Pin library": 3
    },
    {
        "Error!": 1,
        "Initializing": 2,
        "Look at this! ->": 3
    }
]
``` -->

## Related

- https://roofman.me/2012/09/13/fast-serial-communication-for-c-real-time-applications/

## License

GPLv3
