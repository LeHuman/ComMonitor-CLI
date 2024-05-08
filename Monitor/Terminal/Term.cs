using ComMonitor.Log;
using ComMonitor.Serial;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ComMonitor.Terminal {

    public static class Term {

        #region Defines

        public static readonly ConsoleColor DefaultConsoleColor = ConsoleColor.White;

        private static bool updateInput = false;
        private static bool enableInputPrompt = false;
        private static bool expandInput;
        private static DataType inputDataType = DataType.None;

        private static bool colorEnabled = false;
        private static bool logColorEnabled = false;
        private static readonly object inputLock = new();

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

        private static async Task<Tuple<int, ConsoleColor>[]> GetColorSplice(ConsoleColor color, string key_str, string str) {
            //string[] strings = str.Split(new[] { key_str }, StringSplitOptions.RemoveEmptyEntries);
            //if (str.Contains(key_str)) {
            //    Console.ForegroundColor = color;
            //}

            str = str.ToUpper();
            List<Tuple<int, ConsoleColor>> result = new(4);

            await Task.Run(() => {
                int ix = -1;
                while (true) {
                    ix = str.IndexOf(key_str, ix + 1);
                    if (ix == -1)
                        break;
                    result.Add(new Tuple<int, ConsoleColor>(ix, color));
                }
            });

            return [.. result];
        }

        private static Task<Tuple<int, ConsoleColor>[]>[] ColorLevelTasks = new Task<Tuple<int, ConsoleColor>[]>[logLevels.Count + 1];

        public static Tuple<string[], ConsoleColor[]> ColorLogLevel(string str) {
            int c = 0;
            foreach (KeyValuePair<string, ConsoleColor> entry in logLevels) {
                ColorLevelTasks[c++] = GetColorSplice(entry.Value, entry.Key, str);
                //    if (str.Contains(entry.Key)) // What is the performance impact of this vs StartsWith?
                //    {
                //        Console.ForegroundColor = entry.Value;
                //        return;
                //    }
            }

            ColorLevelTasks[c] = Task.Run(() => {
                List<Tuple<int, ConsoleColor>> result = new(4);

                int ix = -1;
                while (true) {
                    ix = str.IndexOf('\n', ix + 1);
                    if (ix == -1)
                        break;
                    result.Add(new Tuple<int, ConsoleColor>(ix, DefaultConsoleColor));
                }

                return result.ToArray();
            });

            List<Tuple<int, ConsoleColor>> splices = new(10);
            foreach (Task<Tuple<int, ConsoleColor>[]> task in ColorLevelTasks) {
                splices.AddRange(task.Result);
            }
            splices.Sort((x, y) => x.Item1.CompareTo(y.Item1));
            string[] strings = new string[splices.Count + 1];
            ConsoleColor[] colors = new ConsoleColor[splices.Count + 1];
            int last = 0;
            c = 0;

            // We have a string cutoff at the start
            //int offset = 0;
            //if (splices[0].Item1 != 0) {
            //    offset = 1;
            //    colors[0] = DefaultConsoleColor;
            //}

            colors[0] = DefaultConsoleColor;

            foreach (Tuple<int, ConsoleColor> splice in splices) {
                strings[c] = str[last..splice.Item1];
                colors[c + 1] = splice.Item2;
                c++;
                last = splice.Item1;
            }

            strings[c] = str[last..];
            return new Tuple<string[], ConsoleColor[]>(strings, colors);
            //Console.ForegroundColor = DefaultConsoleColor;
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

        private static readonly ConcurrentQueue<Tuple<string, bool, bool>> TerminalData = new();
        private static readonly SemaphoreSlim TerminalSemaphore = new(1);

        public static async void WriteInternal(string str, bool newline, bool Log) {
            TerminalData.Enqueue(new Tuple<string, bool, bool>(str, newline, Log));
            await Task.Run(async () => {
                if (!await TerminalSemaphore.WaitAsync(0))
                    return;

                try {
                    while (TerminalData.TryDequeue(out Tuple<string, bool, bool> packet)) {
                        // We may be overrun with data, attempt to concatenate alongside dequeuing
                        if (TerminalData.Count > 1000) {
                            Task writeTask = Task.Run(() => _WriteInternal(packet.Item1, packet.Item2, packet.Item3));
                            TerminalData.TryDequeue(out var merged); // NOTE: Guaranteed to return an item
                            do {
                                if (TerminalData.TryDequeue(out var newMerged)) {
                                    merged = new Tuple<string, bool, bool>(merged.Item1 + (merged.Item2 ? '\n' : null) + newMerged.Item1, newMerged.Item2, merged.Item3 || newMerged.Item3);
                                }
                            } while (!writeTask.IsCompleted);
                            TerminalData.Prepend(merged);
                        } else {
                            _WriteInternal(packet.Item1, packet.Item2, packet.Item3);
                        }
                    }
                } finally {
                    TerminalSemaphore.Release();
                }
            });
        }

        // FIXME: Log coloring is broken, sometimes fails to color after a default string
        public static void _WriteInternal(string str, bool newline, bool Log) {
            ClearInputLine();

            if (logColorEnabled) {
                Tuple<string[], ConsoleColor[]> lines = ColorLogLevel(str);
                for (int i = 0; i < lines.Item1.Length; i++) {
                    str = lines.Item1[i];
                    if (str.Length > 0) {
                        Console.ForegroundColor = lines.Item2[i];
                        if (newline) {
                            Console.WriteLine(str);
                            if (Log)
                                FileLog.LogLine(str);
                        } else {
                            Console.Write(str);
                            if (Log)
                                FileLog.Log(str);
                        }
                    }
                    Console.ForegroundColor = DefaultConsoleColor;
                }
            } else if (str.Length > 0) {
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
