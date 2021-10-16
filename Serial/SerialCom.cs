﻿/* This AX-Fast Serial Library - Modified
   Developer: Ahmed Mubarak - RoofMan

   https://roofman.me/2012/09/13/fast-serial-communication-for-c-real-time-applications/

   This Library Provide The Fastest & Efficient Serial Communication
   Over The Standard C# Serial Component
*/

using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace Serial
{
    public class DataStreamEventArgs : EventArgs
    {
        public byte[] Data { get; set; }
    }

    public static class SerialClient
    {
        #region Defines

        public static string portName { get; private set; }
        public static int baudRate { get; private set; } = 9600;
        public static Parity parity { get; private set; } = Parity.None;
        public static int dataBits { get; private set; } = 8;
        public static StopBits stopBits { get; private set; } = StopBits.None;
        public static int freqCriticalLimit { get; private set; } = 20; // The Critical Frequency of Communication to Avoid Any Lag

        private static SerialPort serialPort;
        private static int writeTimeout = -1;
        private static Thread serThread;
        private static double packetsRate;
        private static DateTime lastReceive;

        #endregion Defines

        #region Custom Events

        public static event EventHandler<DataStreamEventArgs> SerialDataReceived;

        #endregion Custom Events

        #region Setups

        public static void Setup(string port)
        {
            portName = port;
        }

        public static void Setup(string Port, int baudRate)
        {
            Setup(Port);
            SerialClient.baudRate = baudRate;
        }

        public static void Setup(string Port, int baudRate, Parity parity, int dataBits, StopBits stopBits, int freqCriticalLimit)
        {
            Setup(Port, baudRate);
            SerialClient.parity = parity;
            SerialClient.dataBits = dataBits;
            SerialClient.stopBits = stopBits;
            SerialClient.freqCriticalLimit = Math.Max(1, freqCriticalLimit);
        }

        public static void SetWriteTimeout(int timeout)
        {
            writeTimeout = timeout;
        }

        #endregion Setups

        #region Methods

        #region Port Control

        public static bool OpenConn()
        {
            try
            {
                if (serialPort == null)
                    serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);

                if (!PortAvailable())
                    throw new SerialException(string.Format("Port is not available: {0}", portName));

                if (!serialPort.IsOpen)
                {
                    serialPort.ReadTimeout = -1;
                    serialPort.WriteTimeout = writeTimeout;

                    serialPort.Open();

                    if (!serialPort.IsOpen)
                        throw new SerialException(string.Format("Could not open serial port: {0}", portName));

                    packetsRate = 0;
                    lastReceive = DateTime.MinValue;

                    serThread = new Thread(new ThreadStart(SerialReceiving))
                    {
                        Priority = ThreadPriority.AboveNormal
                    };
                    serThread.Name = "SerialHandle" + serThread.ManagedThreadId;
                    serThread.Start(); /*Start The Communication Thread*/
                }
            }
            catch (SerialException e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static bool OpenConn(string port, int baudRate)
        {
            portName = port;
            SerialClient.baudRate = baudRate;

            return OpenConn();
        }

        public static void CloseConn()
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                serThread.Abort();

                if (serThread.ThreadState == ThreadState.Aborted)
                    serialPort.Close();
            }
        }

        public static bool ResetConn()
        {
            CloseConn();
            return OpenConn();
        }

        public static bool PortAvailable()
        {
            try
            {
                return SerialPort.GetPortNames().Contains(portName);
            }
            catch (Win32Exception)
            {
                return false;
            }
        }

        public static bool IsAlive()
        {
            return serialPort.IsOpen;
        }

        #endregion Port Control

        #region Transmit/Receive

        public static void Transmit(byte[] packet)
        {
            serialPort.Write(packet, 0, packet.Length);
        }

        #endregion Transmit/Receive

        #region IDisposable Methods

        public static void Dispose()
        {
            CloseConn();

            if (serialPort != null)
            {
                serialPort.Dispose();
                serialPort = null;
            }
        }

        #endregion IDisposable Methods

        #endregion Methods

        #region Threading Loops

        public static bool AddFreq(int freq)
        {
            if (freqCriticalLimit == 1)
                return false;
            freqCriticalLimit = Math.Max(freqCriticalLimit + freq, 1);
            return true;
        }

        public static void SendString(string msg) // TODO: Async Writes
        {
            try
            {
                if (serialPort != null)
                    serialPort.Write(msg);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Serial: Error Sending Data, {e.Message}");
            }
        }

        public static void SendBytes(byte[] msg)
        {
            try
            {
                if (serialPort != null)
                    serialPort.Write(msg, 0, msg.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Serial: Error Sending Data, {e.Message}");
            }
        }

        private static async void SerialReceiving()
        {
            int count;
            while (true)
            {
                try
                {
                    count = serialPort.BytesToRead;

                    /*Get Sleep Inteval*/
                    TimeSpan tmpInterval = (DateTime.Now - lastReceive);

                    /*Form The Packet in The Buffer*/
                    byte[] buf = new byte[count];
                    int readBytes = 0;
                    if (count > 0)
                    {
                        readBytes = await serialPort.BaseStream.ReadAsync(buf, 0, count);
                        OnSerialReceiving(buf);
                    }

                    #region Frequency Control

                    packetsRate = ((packetsRate + readBytes) / 2);
                    lastReceive = DateTime.Now;

                    if (tmpInterval.Milliseconds > 0 && (double)(readBytes + serialPort.BytesToRead) / 2 <= packetsRate)
                        Thread.Sleep(tmpInterval.Milliseconds > freqCriticalLimit ? freqCriticalLimit : tmpInterval.Milliseconds);

                    #endregion Frequency Control
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Serial: Error Receiving Data, {e.Message}");
                    break;
                }
            }
        }

        #endregion Threading Loops

        #region Custom Events Invoke Functions

        private static void OnSerialReceiving(byte[] res)
        {
            SerialDataReceived?.Invoke(null, new DataStreamEventArgs() { Data = res });
        }

        #endregion Custom Events Invoke Functions
    }
}
