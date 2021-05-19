using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ComMonitor
{
    public delegate void DelegateConsoleInput(string msg);

    public delegate void DelegateConsoleUpdate();

    internal class ConsoleInput
    {
        private static readonly Stream Input = Console.OpenStandardInput();
        private static readonly Thread Observer = new Thread(new ThreadStart(ObserveInput));

        private static DelegateConsoleInput Callback;
        private static DelegateConsoleUpdate UpdateCallback;
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

        public static void SetCallback(DelegateConsoleInput callback)
        {
            Callback = callback;
        }

        public static void SetUpdateCallback(DelegateConsoleUpdate updateCallback)
        {
            UpdateCallback = updateCallback;
        }

        public static string GetCurrentInput()
        {
            if (currentInput == "")
                return "";
            return $"Input: {currentInput}\r";
        }

        private static bool EnableInput = false;

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
                        UpdateCallback.Invoke();
                    }
                    continue;
                }
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Insert && Clipboard.ContainsText())
                {
                    string clip = Clipboard.GetText(TextDataFormat.Text);
                    if (clip != "")
                        Callback.Invoke(clip);
                }
                else if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (currentInput.Length > 0)
                    {
                        currentInput = currentInput.Substring(0, currentInput.Length - 1);
                        UpdateCallback.Invoke();
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
                            Callback.Invoke(currentInput);
                            currentInput = "";
                            UpdateCallback.Invoke();
                        }
                        else if (next != '\0')
                        {
                            currentInput += next;
                            UpdateCallback.Invoke();
                        }
                    }
                }
            }
        }
    }
}