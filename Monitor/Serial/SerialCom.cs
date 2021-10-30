/* This AX-Fast Serial Library - Modified
   Developer: Ahmed Mubarak - RoofMan

   https://roofman.me/2012/09/13/fast-serial-communication-for-c-real-time-applications/

   This Library Provide The Fastest & Efficient Serial Communication
   Over The Standard C# Serial Component
*/

using RJCP.IO.Ports;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace ComMonitor.Serial {

    public class DataStreamEventArgs : EventArgs {
        public byte[] Data { get; set; }
    }

    public static class SerialClient {

        #region Defines

        public static string PortName { get; private set; }
        public static int BaudRate { get; private set; } = 9600;
        public static Parity Parity { get; private set; } = Parity.None;
        public static int DataBits { get; private set; } = 8;
        public static StopBits StopBits { get; private set; } = StopBits.One;
        public static int FreqCriticalLimit { get; private set; } = 20; // The Critical Frequency of Communication to Avoid Any Lag

        private static Thread serThread;
        private static double packetsRate;
        private static DateTime lastReceive;
        private static int writeTimeout = -1;
        private static SerialPortStream serialPort;

        #endregion Defines

        #region Custom Events

        public static event EventHandler<DataStreamEventArgs> SerialDataReceived;

        #endregion Custom Events

        #region Setups

        public static void Setup(string port) {
            PortName = port;
        }

        public static void SetWriteTimeout(int timeout) {
            writeTimeout = timeout;
        }

        public static void Setup(string Port, int baudRate) {
            Setup(Port);
            SerialClient.BaudRate = baudRate;
        }

        public static void Setup(string Port, int baudRate, Parity parity, int dataBits, StopBits stopBits, int freqCriticalLimit) {
            Setup(Port, baudRate);
            SerialClient.Parity = parity;
            SerialClient.DataBits = dataBits;
            SerialClient.StopBits = stopBits;
            SerialClient.FreqCriticalLimit = Math.Max(1, freqCriticalLimit);
        }

        #endregion Setups

        #region Methods

        #region Port Control

        public static bool IsAlive() {
            return serialPort.IsOpen;
        }

        public static bool ResetConn() {
            CloseConn();
            return OpenConn();
        }

        public static bool OpenConn(string port, int baudRate) {
            PortName = port;
            SerialClient.BaudRate = baudRate;

            return OpenConn();
        }

        public static bool PortAvailable() {
            try {
                return SerialPortStream.GetPortNames().Contains(PortName);
            } catch (Win32Exception) {
                return false;
            }
        }

        public static void CloseConn() {
            if (serialPort != null && serialPort.IsOpen) {// Stop thread here
                                                          // serThread.Interrupt();

                // if (serThread.ThreadState == ThreadState.Aborted)
                serialPort.Close();
            }
        }

        public static bool OpenConn() {
            try {
                if (serialPort == null)
                    serialPort = new SerialPortStream(PortName, BaudRate, DataBits, Parity, StopBits);

                if (!PortAvailable())
                    throw new SerialException(string.Format("Port is not available: {0}", PortName));

                if (!serialPort.IsOpen) {
                    serialPort.ReadTimeout = -1;
                    serialPort.WriteTimeout = writeTimeout;

                    serialPort.Open();

                    if (!serialPort.IsOpen)
                        throw new SerialException(string.Format("Could not open serial port: {0}", PortName));

                    packetsRate = 0;
                    lastReceive = DateTime.MinValue;

                    serThread = new Thread(new ThreadStart(SerialReceiving))
                    {
                        Priority = ThreadPriority.AboveNormal
                    };
                    serThread.Name = "SerialHandle" + serThread.ManagedThreadId;
                    serThread.Start(); /*Start The Communication Thread*/
                }
            } catch (SerialException e) {
                Console.WriteLine(e.Message);
                return false;
            } catch {
                return false;
            }

            return true;
        }

        #endregion Port Control

        #region Transmit/Receive

        public static void Transmit(byte[] packet) {
            serialPort.Write(packet, 0, packet.Length);
        }

        #endregion Transmit/Receive

        #region IDisposable Methods

        public static void Dispose() {
            CloseConn();

            if (serialPort != null) {
                serialPort.Dispose();
                serialPort = null;
            }
        }

        #endregion IDisposable Methods

        #endregion Methods

        #region Threading Loops

        public static bool AddFreq(int freq) {
            if (FreqCriticalLimit == 1)
                return false;
            FreqCriticalLimit = Math.Max(FreqCriticalLimit + freq, 1);
            return true;
        }

        public static void SendString(string msg) // TODO: Async Writes
        {
            try {
                if (serialPort != null)
                    serialPort.Write(msg);
            } catch (Exception e) {
                Console.WriteLine($"Serial: Error Sending Data, {e.Message}");
            }
        }

        public static void SendBytes(byte[] msg) {
            try {
                if (serialPort != null)
                    serialPort.Write(msg, 0, msg.Length);
            } catch (Exception e) {
                Console.WriteLine($"Serial: Error Sending Data, {e.Message}");
            }
        }

        private static async void SerialReceiving() {
            int count;
            while (true) {
                try {
                    count = serialPort.BytesToRead;

                    /*Get Sleep Inteval*/
                    TimeSpan tmpInterval = (DateTime.Now - lastReceive);

                    /*Form The Packet in The Buffer*/
                    byte[] buf = new byte[count];
                    int readBytes = 0;
                    if (count > 0) {
                        readBytes = await serialPort.ReadAsync(buf.AsMemory(0, count));
                        OnSerialReceiving(buf);
                    }

                    #region Frequency Control

                    packetsRate = (packetsRate + readBytes) / 2;
                    lastReceive = DateTime.Now;

                    if (tmpInterval.Milliseconds > 0 && (double)(readBytes + serialPort.BytesToRead) / 2 <= packetsRate)
                        Thread.Sleep(tmpInterval.Milliseconds > FreqCriticalLimit ? FreqCriticalLimit : tmpInterval.Milliseconds);

                    #endregion Frequency Control
                } catch (Exception e) {
                    Console.WriteLine($"Serial: Error Receiving Data, {e.Message}");
                    break;
                }
            }
        }

        #endregion Threading Loops

        #region Custom Events Invoke Functions

        private static void OnSerialReceiving(byte[] res) {
            SerialDataReceived?.Invoke(null, new DataStreamEventArgs() { Data = res });
        }

        #endregion Custom Events Invoke Functions
    }
}
