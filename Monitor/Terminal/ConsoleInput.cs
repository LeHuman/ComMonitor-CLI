using System;
using System.IO;
using System.Threading;
using TextCopy;

namespace ComMonitor.Terminal {

    public delegate void DelegateConsoleInput(string msg);

    public delegate void DelegateConsoleUpdate();

    public static class ConsoleInput {
        public static bool Enable { get; set; }
        public static bool DisableCarridgeReturn { get; set; }
        public static bool EmptyBuffer { get; set; }
        private static string currentInput = "";
        private static readonly Stream Input = Console.OpenStandardInput();
        private static readonly Thread Observer = new(new ThreadStart(ObserveInput));

        public static string GetCurrentInput() {
            if (currentInput == "")
                return "";

            return $"Input: {currentInput}";
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
                if (!Enable) {
                    if (currentInput != "") {
                        currentInput = "";
                        Term.UpdateInputLine();
                        EmptyBuffer = true;
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
                        Term.UpdateInputLine();
                    } else if (!EmptyBuffer) {
                        Term.UpdateInputLine();
                        EmptyBuffer = true;
                    }
                } else {
                    char next = keyInfo.KeyChar;
                    if (next != -1) {
                        if (next == '\r') { // IMPROVE: Do we also check for '\n'?
                            if (currentInput == "") {
                                if (DisableCarridgeReturn)
                                    Console.WriteLine("Input Buffer is empty");
                                else
                                    Console.Write("\rInput Buffer is empty\r");
                                continue;
                            }
                            Term.SendMsg(currentInput);
                            currentInput = "";
                            Term.UpdateInputLine();
                            EmptyBuffer = true;
                        } else if (next != '\0') {
                            currentInput += next;
                            EmptyBuffer = false;
                            Term.UpdateInputLine();
                        }
                    }
                }
            }
        }
    }
}
