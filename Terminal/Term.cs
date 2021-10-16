using Log;
using Serial;
using System;
using System.Collections.Generic;

namespace Terminal
{
    internal static class Term
    {
        #region Defines

        public static readonly ConsoleColor DefaultConsoleColor = ConsoleColor.White;

        private static DataType inputDataType = DataType.None;
        private static bool enableInputPrompt = false;
        private static bool checkInput = false;

        private static bool colorEnabled = false;
        private static bool logColorEnabled = false;

        private static readonly Dictionary<string, ConsoleColor> logLevels = new Dictionary<string, ConsoleColor>{
            { "[DEBUG]", ConsoleColor.Magenta },
            { "[FATAL]", ConsoleColor.DarkRed },
            { "[ERROR]", ConsoleColor.Red },
            { "[WARN]", ConsoleColor.Yellow },
            { "[INFO]", ConsoleColor.Cyan }
        };

        #endregion Defines

        #region Color

        internal static void ColorEnable(bool enabled)
        {
            colorEnabled = enabled;
            Console.CancelKeyPress += delegate { Console.ResetColor(); };
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = DefaultConsoleColor;
        }

        internal static void EnableLogColor(bool enabled)
        {
            logColorEnabled = colorEnabled && enabled;
        }

        public static void ColorSingle(ConsoleColor color, string line)
        {
            ColorConsole(color);
            Console.WriteLine(line);
            ColorConsole();
        }

        public static void ColorConsole(ConsoleColor color)
        {
            if (colorEnabled)
                Console.ForegroundColor = color;
        }

        public static void ColorConsole()
        {
            ColorConsole(DefaultConsoleColor);
        }

        public static void ColorLogLevel(string str)
        {
            if (logColorEnabled)
            {
                foreach (KeyValuePair<string, ConsoleColor> entry in logLevels)
                {
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

        public static void EnableInput(DataType inputDataType, bool enableInputPrompt)
        {
            Term.inputDataType = inputDataType;
            Term.enableInputPrompt = enableInputPrompt;
            checkInput = inputDataType != DataType.None && inputDataType != DataType.Mapped;

            if (checkInput)
                ConsoleInput.Start();
        }

        private static bool alreadyChecking = false;

        public static void CheckInputLine()
        {
            if (!checkInput || !enableInputPrompt || alreadyChecking)
                return;
            alreadyChecking = true;
            string input = ConsoleInput.GetCurrentInput();

            if (input != "")
            {
                Console.WriteLine();
                Console.Write(input);
                Console.CursorTop--;
            }
            alreadyChecking = false;
        }

        internal static void SendMsg(string msg)
        {
            if (inputDataType == DataType.Ascii)
            {
                Console.WriteLine($"Sending String: {msg}");
                SerialClient.SendString(msg);
            }
            else
            {
                string[] messages = msg.Trim().Split(' ');
                foreach (var msgStr in messages)
                {
                    byte[] msgArr = SerialType.GetByteArray(inputDataType, msgStr);
                    if (msgArr == null)
                        return;
                    Console.WriteLine($"Sending Bytes: {string.Join(",", msgArr)}");
                    SerialClient.SendBytes(msgArr);
                }
            }
        }

        #endregion Input

        #region Output

        public static void Write(string str, bool Log = false)
        {
            WriteInternal(str, false, Log);
        }

        public static void WriteLine(string str, bool Log = false)
        {
            WriteInternal(str, true, Log);
        }

        private static void WriteInternal(string str, bool newline, bool Log)
        {
            CheckInputLine();
            if (str.Length > 0)
            {
                ColorLogLevel(str.ToUpper()); // TODO: trim to last bracket to reduce text that is searched
                if (newline)
                {
                    Console.WriteLine(str);
                    if (Log)
                        FileLog.LogLine(str);
                }
                else
                {
                    Console.Write(str);
                    if (Log)
                        FileLog.Log(str);
                }
                ColorConsole();
            }
        }

        public static void ExceptionPrint(Exception ex)
        {
            if (colorEnabled)
                Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            if (colorEnabled)
                Console.ForegroundColor = DefaultConsoleColor;
        }

        #endregion Output
    }
}
