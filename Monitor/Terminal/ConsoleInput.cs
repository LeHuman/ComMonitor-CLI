using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace Terminal
{
    public delegate void DelegateConsoleInput(string msg);

    public delegate void DelegateConsoleUpdate();

    internal static class ConsoleInput
    {
        private static readonly Thread Observer = new Thread(new ThreadStart(ObserveInput));
        private static readonly Stream Input = Console.OpenStandardInput();
        private static bool EnableInput = false;
        private static string currentInput = "";

        public static void Start()
        {
            if (Input.CanRead)
            {
                Observer.SetApartmentState(ApartmentState.STA);
                Observer.Start();
            }
            else
            {
                Console.WriteLine("Console does not support reading");
            }
        }

        public static string GetCurrentInput()
        {
            if (currentInput == "")
                return "";
            return $"Input: {currentInput}\r";
        }

        public static void Enable(bool enable)
        {
            EnableInput = enable;
        }

        public static void ObserveInput()
        {
            while (true)
            {
                if (!EnableInput)
                {
                    if (currentInput != "")
                    {
                        currentInput = "";
                        Term.CheckInputLine();
                    }
                    continue;
                }
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Insert && Clipboard.ContainsText())
                {
                    string clip = Clipboard.GetText(TextDataFormat.Text);
                    if (clip != "")
                        Term.SendMsg(clip);
                }
                else if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (currentInput.Length > 0)
                    {
                        currentInput = currentInput.Substring(0, currentInput.Length - 1);
                        Term.CheckInputLine();
                    }
                }
                else
                {
                    char next = keyInfo.KeyChar;
                    if (next != -1)
                    {
                        if (next == '\r')
                        {
                            if (currentInput == "")
                            {
                                Console.Write("\rInput Buffer is empty\r");
                                continue;
                            }
                            Term.SendMsg(currentInput);
                            currentInput = "";
                            Term.CheckInputLine();
                        }
                        else if (next != '\0')
                        {
                            currentInput += next;
                            Term.CheckInputLine();
                        }
                    }
                }
            }
        }
    }
}
