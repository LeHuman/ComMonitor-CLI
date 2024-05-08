/* This AX-Fast Serial Library - Modified
   Developer: Ahmed Mubarak - RoofMan

   https://roofman.me/2012/09/13/fast-serial-communication-for-c-real-time-applications/

   This Library Provide The Fastest & Efficient Serial Communication
   Over The Standard C# Serial Component
*/

// Ignore Spelling: Dtr

using CommandLine;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;

namespace ComMonitor.Serial {
    //public class DataStreamEventArgs : EventArgs {
    //    public byte[] Data { get; set; }
    //}

    public static class SerialClient {

        #region Defines

        public static string PortName { get; private set; }
        public static int BaudRate { get; private set; } = 9600;
        public static Parity Parity { get; private set; } = Parity.None;
        public static int DataBits { get; private set; } = 8;
        public static StopBits StopBits { get; private set; } = StopBits.One;
        public static bool Dtr { get; private set; } = true;

        private static Thread receivingThread;
        private static int writeTimeout = -1;
        private static SerialPort serialPort;
        public static readonly AutoResetEvent DataReceived = new(false);
        private static readonly ConcurrentQueue<byte[]> IncomingData = new();
        private static readonly ConcurrentQueue<byte[]> OutgoingData = new();
        private static readonly SemaphoreSlim OutgoingSemaphore = new(1);
        private static bool stopThreads = false;

        #endregion Defines

        #region Setups

        public static void Setup(string port) {
            PortName = port;
        }

        public static void Setup(string Port, int baudRate) {
            Setup(Port);
            SerialClient.BaudRate = baudRate;
        }

        public static void Setup(string Port, int baudRate, Parity parity, int dataBits, StopBits stopBits, bool enableDtr) {
            Setup(Port, baudRate);
            SerialClient.Parity = parity;
            SerialClient.DataBits = dataBits;
            SerialClient.StopBits = stopBits;
            SerialClient.Dtr = enableDtr;
        }

        public static void SetWriteTimeout(int timeout) {
            writeTimeout = timeout;
        }

        #endregion Setups

        #region Methods

        public static void ClearLock() => DataReceived.Set();

        #region Port Control

        public static bool IsAlive => serialPort?.IsOpen ?? false;

        public static bool ResetConn() {
            CloseConn();
            return OpenConn();
        }

        public static bool PortListed() {
            try {
                return SerialPort.GetPortNames().Contains(PortName); // NOTE: GetPortNames is wonky, at least on windows, does not update correctly
            } catch (Win32Exception) {
                return false;
            }
        }

        public static void CloseConn() {
            stopThreads = true;
            OutgoingSemaphore.Wait();
            receivingThread?.Join();
            serialPort?.Close();
            // Wait up to a second for consumers to finish, if needed
            if (!IncomingData.IsEmpty) {
                DataReceived.Set();
                Stopwatch sw = Stopwatch.StartNew();
                while (!IncomingData.IsEmpty && sw.ElapsedMilliseconds < 1000) {
                    Thread.Sleep(10);
                }
            }
            DataReceived.Reset();
            IncomingData.Clear();
            OutgoingData.Clear();
            OutgoingSemaphore.Release(); // IMPROVE: Does this need a tryf?
        }

        public static bool OpenConn(string port, int baudRate) {
            PortName = port;
            SerialClient.BaudRate = baudRate;

            return OpenConn();
        }

        public static bool OpenConn(bool suppressMessage = false) {
            serialPort ??= new SerialPort(PortName, BaudRate, Parity, DataBits, StopBits);

            if (!PortListed()) {
                if (!suppressMessage)
                    Console.WriteLine(string.Format("Port is not available: {0}", PortName));
                return false;
            }

            if (!serialPort.IsOpen) {
                CloseConn();
                serialPort.ReadTimeout = -1;
                serialPort.WriteTimeout = writeTimeout;
                serialPort.DtrEnable = Dtr;

                try {
                    serialPort.Open();
                } catch (UnauthorizedAccessException) {
                    if (!suppressMessage)
                        Console.WriteLine(string.Format("Serial port opened by another application: {0}", PortName));
                    return false;
                } catch (System.IO.FileNotFoundException) {
                    if (!suppressMessage)
                        Console.WriteLine(string.Format("Serial port not found: {0}", PortName));
                    return false;
                } catch {
                }

                if (!serialPort.IsOpen) {
                    if (!suppressMessage)
                        Console.WriteLine(string.Format("Could not open serial port: {0}", PortName));
                    return false;
                }

                receivingThread = new Thread(new ThreadStart(SerialReceiving))
                {
                    Priority = ThreadPriority.AboveNormal
                };
                receivingThread.Name = "SerialHandle" + receivingThread.ManagedThreadId;
                stopThreads = false;
                receivingThread.Start(); // Start The Communication Thread
            }

            return true;
        }

        #endregion Port Control

        #region Transmit/Receive

        public static bool Receive(out byte[] data) {
            return IncomingData.TryDequeue(out data);
        }

        private static async void Transmit() {
            if (!await OutgoingSemaphore.WaitAsync(0))
                return;

            try {
                while (OutgoingData.TryDequeue(out byte[] data)) {
#if NETCOREAPP2_1_OR_GREATER
                    await serialPort.BaseStream.WriteAsync(data);
#else
                    await serialPort.BaseStream.WriteAsync(data, 0, data.Length);
#endif
                }
            } finally {
                OutgoingSemaphore.Release();
            }
        }

        #endregion Transmit/Receive

        #region IDisposable Methods

        public static void Dispose() {
            CloseConn();

            serialPort?.Dispose();
            serialPort = null;
        }

        #endregion IDisposable Methods

        #endregion Methods

        #region Threading Loops

        public static void SendString(string msg) {
            SendBytes(Encoding.ASCII.GetBytes(msg));
        }

        public static void SendBytes(byte[] msg) {
            try {
                OutgoingData.Enqueue(msg);
                Transmit();
            } catch (Exception e) {
                Console.WriteLine($"Serial: Error Sending Data, {e.Message}");
            }
        }

        private const int BufferSize = 4096;
        private static readonly byte[] Buffer = new byte[BufferSize]; // TODO: Ensure this can be allocated
        private static ReadOnlySpan<byte> Buf_s => Buffer;

        private static void SerialReceiving() {
            Stopwatch sw = Stopwatch.StartNew();
            while (!stopThreads) {
                try {
                    int count = serialPort.BaseStream.ReadAsync(Buffer, 0, BufferSize).Result;
                    // Wait up to 2ms for more data if received data is small, reduces the number of queued items
                    if (count < 8) {
                        sw.Restart();
                        while (serialPort.BytesToRead < 0 && sw.ElapsedMilliseconds < 2) { }
                        count += serialPort.BaseStream.ReadAsync(Buffer, count, BufferSize - count).Result;
                    }
                    IncomingData.Enqueue(Buf_s[..count].ToArray());
                    DataReceived.Set();
                } catch (Exception e) {
                    Console.WriteLine($"Serial: Error Receiving Data, {e.Message}");
                    break;
                }
            }
        }

        #endregion Threading Loops
    }
}
