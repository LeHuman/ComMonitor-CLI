using System;
using System.IO.Ports;
using System.Threading;

namespace ComMonitor.Main {
    internal class PortSelector {
        private int selectedIndex = 0;
        private string[] ports;
        private string selectedPort;
        private Timer timer;
        private bool stopRequested = false; // Flag to signal stop request
        private bool screenUpdateNeeded = true; // Flag to indicate if screen update is needed
        private readonly object lockObject = new object(); // Object for synchronization

        public void Start() {
            // Get the names of all available serial ports
            ports = SerialPort.GetPortNames();

            stopRequested = false;

            // Timer for periodic updates
            timer = new Timer(RefreshSerialPorts, null, TimeSpan.Zero, TimeSpan.FromSeconds(2)); // Refresh every 5 seconds

            // Start a separate thread for handling user input
            Thread userInputThread = new Thread(UserInputLoop);
            userInputThread.IsBackground = true;
            userInputThread.Start();

            // Main thread updates the screen
            UpdateScreenLoop();
            // Gracefully close other threads
            timer?.Dispose();
            stopRequested = true;
            userInputThread.Join();
        }

        private void UpdateScreenLoop() {
            while (!stopRequested) {
                // Check if screen update is needed
                if (screenUpdateNeeded) {
                    // Clear the console and print the list of ports
                    Console.Clear();
                    PrintSerialPorts();

                    // Reset the flag
                    screenUpdateNeeded = false;
                }

                // Wait for a short duration before checking again
                Thread.Sleep(50);
            }
        }

        private void UserInputLoop() {
            ConsoleKeyInfo keyInfo;
            do {
                // Read key input
                keyInfo = Console.ReadKey(true);

                // Process key input
                switch (keyInfo.Key) {
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.W:
                        lock (lockObject) {
                            selectedIndex = Math.Max(0, selectedIndex - 1);
                        }
                        screenUpdateNeeded = true; // Set flag to update screen
                        break;

                    case ConsoleKey.DownArrow:
                    case ConsoleKey.S:
                        lock (lockObject) {
                            selectedIndex = Math.Min(ports.Length - 1, selectedIndex + 1);
                        }
                        screenUpdateNeeded = true; // Set flag to update screen
                        break;

                    case ConsoleKey.Enter:
                        lock (lockObject) {
                            if (ports.Length > 0) {
                                selectedPort = ports[selectedIndex];
                                Console.Clear();
                                Console.WriteLine($"Selected port: {selectedPort}");
                            } else {
                                Console.Clear();
                                Console.WriteLine("No serial ports available.");
                                selectedPort = null;
                            }
                        }
                        stopRequested = true;
                        break;

                    case ConsoleKey.Escape:
                        stopRequested = true; // Set flag to request stop
                        break;
                }
            } while (!stopRequested);
        }

        private void RefreshSerialPorts(object state) {
            // Get the names of all available serial ports
            lock (lockObject) {
                string[] newPorts = SerialPort.GetPortNames();
                if (!ArrayEquals(ports, newPorts)) {
                    ports = newPorts;
                    screenUpdateNeeded = true; // Set flag to update screen
                    selectedIndex = Math.Max(0, Math.Min(selectedIndex, ports.Length - 1));
                }
            }
        }

        private void PrintSerialPorts() {
            Console.WriteLine("Available serial ports:");
            lock (lockObject) {
                for (int i = 0; i < ports.Length; i++) {
                    if (i == selectedIndex) {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("> ");
                    } else {
                        Console.Write("  ");
                    }
                    Console.WriteLine(ports[i]);
                    Console.ResetColor();
                }
            }
        }

        private bool ArrayEquals(string[] arr1, string[] arr2) {
            if (arr1.Length != arr2.Length) {
                return false;
            }
            for (int i = 0; i < arr1.Length; i++) {
                if (arr1[i] != arr2[i]) {
                    return false;
                }
            }
            return true;
        }

        public string Stop() {
            lock (lockObject) {
                string temp = selectedPort;
                selectedPort = null;
                return temp;
            }
        }
    }
}
