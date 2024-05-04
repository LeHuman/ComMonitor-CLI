using ComMonitor.Log;
using ComMonitor.Serial;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace ComMonitor.Terminal {

    public static class Term {

        #region Defines

        public static readonly ConsoleColor DefaultConsoleColor = ConsoleColor.White;

        private static int lastInputLen = 0;
        private static bool updateInput = false;
        private static bool enableInputPrompt = false;
        private static bool expandInput;
        private static DataType inputDataType = DataType.None;

        private static bool colorEnabled = false;
        private static bool logColorEnabled = false;
        private static readonly object inputLock = new object();

        private static readonly Dictionary<string, ConsoleColor> logLevels = new()
        {
            { "[DEBUG]", ConsoleColor.Magenta },
            { "[FATAL]", ConsoleColor.DarkRed },
            { "[ERROR]", ConsoleColor.Red },
            { "[WARN]", ConsoleColor.Yellow },
            { "[INFO]", ConsoleColor.Cyan }
        };

        #endregion Defines

        #region Color

        public static void ColorConsole() {
            ColorConsole(DefaultConsoleColor);
        }

        public static void EnableLogColor(bool enabled) {
            logColorEnabled = colorEnabled && enabled;
        }

        public static void ColorConsole(ConsoleColor color) {
            if (colorEnabled)
                Console.ForegroundColor = color;
        }

        public static void ColorSingle(ConsoleColor color, string line) {
            ColorConsole(color);
            Console.WriteLine(line);
            ColorConsole();
        }

        public static void ColorEnable(bool enabled) {
            colorEnabled = enabled;
            Console.CancelKeyPress += delegate { Console.ResetColor(); };
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = DefaultConsoleColor;
        }

        public static void ColorLogLevel(string str) {
            if (logColorEnabled) {
                foreach (KeyValuePair<string, ConsoleColor> entry in logLevels) {
                    if (str.Contains(entry.Key)) // What is the performance impact of this vs StartsWith?
                    {
                        Console.ForegroundColor = entry.Value;
                        return;
                    }
                }
                Console.ForegroundColor = DefaultConsoleColor;
            }
        }

        #endregion Color

        #region Input

        public static void ClearInputLine() {
            if (!updateInput || !enableInputPrompt)
                return;

            lock (inputLock) { // IMPROVE: Should this be a try instead?
                if (Console.CursorTop == Console.WindowHeight - 1) {
                    var cursorLeft = Console.CursorLeft;
                    Console.CursorLeft = 0;
                    Console.Write(new string(' ', Console.WindowWidth - 1));
                    Console.CursorLeft = cursorLeft;
                }
            }
        }

        public static void UpdateInputLine() {
            if (!updateInput || !enableInputPrompt || !Monitor.TryEnter(inputLock))
                return;

            string input = ConsoleInput.GetCurrentInput();

            if (!ConsoleInput.EmptyBuffer) {
                if (input != "" && ConsoleInput.DisableCarridgeReturn) {
                    Console.WriteLine(input);
                } else { // IMPROVE: Can this throw if WindowHeight changes rapidly?
                    int last = Console.CursorTop;
                    var cursorLeft = Console.CursorLeft;

                    Console.CursorTop = Console.WindowHeight - 1;
                    Console.CursorLeft = 0;

                    Console.Write(input + new string(' ', Console.WindowWidth - 1 - input.Length));
                    Console.CursorTop = last;
                    Console.CursorLeft = cursorLeft;
                }
            }

            Monitor.Exit(inputLock);
        }

        internal static void SendMsg(string message) {
            if (inputDataType == DataType.Ascii) {
                Console.WriteLine($"Sending {(expandInput ? "Expanded" : "")} String: {message}");
                if (expandInput) {
                    message = Regex.Unescape(message);
                }
                SerialClient.SendString(message);
            } else {
                string[] messages = message.Trim().Split(' ');
                foreach (var msgStr in messages) {
                    byte[] msgArr = SerialType.GetByteArray(inputDataType, msgStr);
                    if (msgArr == null)
                        return;
                    Console.WriteLine($"Sending Bytes: {string.Join(",", msgArr)}");
                    SerialClient.SendBytes(msgArr);
                }
            }
        }

        public static void EnableInput(DataType inputDataType, bool expandInput, bool enableInputPrompt) {
            Term.expandInput = expandInput;
            Term.inputDataType = inputDataType;
            Term.enableInputPrompt = enableInputPrompt;
            updateInput = inputDataType != DataType.None && inputDataType != DataType.Mapped;

            if (updateInput)
                ConsoleInput.Start();
        }

        #endregion Input

        #region Output

        public static void ExceptionPrint(Exception ex) {
            if (colorEnabled)
                Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            if (colorEnabled)
                Console.ForegroundColor = DefaultConsoleColor;
        }

        public static void Write(string str, bool Log = false) {
            WriteInternal(str, false, Log);
        }

        public static void WriteLine(string str, bool Log = false) {
            WriteInternal(str, true, Log);
        }

        private static void WriteInternal(string str, bool newline, bool Log) {
            ClearInputLine();
            if (str.Length > 0) {
                ColorLogLevel(str.ToUpper()); // TODO: trim to last bracket to reduce text that is searched
                if (newline) {
                    Console.WriteLine(str);
                    if (Log)
                        FileLog.LogLine(str);
                } else {
                    Console.Write(str);
                    if (Log)
                        FileLog.Log(str);
                }
                ColorConsole();
            }
            UpdateInputLine();
        }

        #endregion Output
    }
}
