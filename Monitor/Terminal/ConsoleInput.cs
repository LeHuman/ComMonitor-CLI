using System;
using System.IO;
using System.Threading;
using TextCopy;

namespace ComMonitor.Terminal {

    public delegate void DelegateConsoleInput(string msg);

    public delegate void DelegateConsoleUpdate();

    public static class ConsoleInput {
        private static bool EnableInput = false;
        private static string currentInput = "";
        private static readonly Stream Input = Console.OpenStandardInput();
        private static readonly Thread Observer = new(new ThreadStart(ObserveInput));

        public static string GetCurrentInput() {
            if (currentInput == "")
                return "";
            return $"Input: {currentInput}\r";
        }

        public static void Enable(bool enable) {
            EnableInput = enable;
        }

        public static void Start() {
            if (Input.CanRead) {
                Observer.Start();
            } else {
                Console.WriteLine("Console does not support reading");
            }
        }

        public static void ObserveInput() {
            while (true) {
                if (!EnableInput) {
                    if (currentInput != "") {
                        currentInput = "";
                        Term.CheckInputLine();
                    }
                    continue;
                }
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Insert) {
                    string clip = ClipboardService.GetText();
                    if (clip.Length > 0)
                        Term.SendMsg(clip);
                } else if (keyInfo.Key == ConsoleKey.Backspace) {
                    if (currentInput.Length > 0) {
                        currentInput = currentInput[0..^1];
                        Term.CheckInputLine();
                    }
                } else {
                    char next = keyInfo.KeyChar;
                    if (next != -1) {
                        if (next == '\r') {
                            if (currentInput == "") {
                                Console.Write("\rInput Buffer is empty\r");
                                continue;
                            }
                            Term.SendMsg(currentInput);
                            currentInput = "";
                            Term.CheckInputLine();
                        } else if (next != '\0') {
                            currentInput += next;
                            Term.CheckInputLine();
                        }
                    }
                }
            }
        }
    }
}
