using System;
using System.Runtime.InteropServices;

// Yoinked from https://stackoverflow.com/questions/13656846/how-to-programmatic-disable-c-sharp-console-applications-quick-edit-mode

namespace ComMonitor.Terminal {

    internal static class ConsoleMode {
        public const uint ENABLE_QUICK_EDIT = 0x0040;
        public const uint ENABLE_LINE_INPUT = 0x0002;
        public const uint ENABLE_MOUSE_INPUT = 0x0010;

        // STD_INPUT_HANDLE (DWORD): -10 is the standard input device.
        private const int STD_INPUT_HANDLE = -10;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        internal static bool Set(uint option, bool enable) {
            try {
                IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);

                if (!GetConsoleMode(consoleHandle, out uint consoleMode)) {
                    return false;
                }

                if (enable)
                    consoleMode |= option;
                else
                    consoleMode &= ~option;

                if (!SetConsoleMode(consoleHandle, consoleMode)) {
                    return false;
                }
            } catch (Exception) {
                return false;
            }

            return true;
        }
    }
}
