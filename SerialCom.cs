﻿/* This AX-Fast Serial Library - Modified
   Developer: Ahmed Mubarak - RoofMan

   https://roofman.me/2012/09/13/fast-serial-communication-for-c-real-time-applications/
 
   This Library Provide The Fastest & Efficient Serial Communication
   Over The Standard C# Serial Component
*/

using System;
using System.IO.Ports;
using System.Threading;
using System.Linq;

namespace SerialCom
{
    public class DataStreamEventArgs : EventArgs
    {
        public byte[] Data { get; set; }

    }

    public class SerialClient : IDisposable
    {
        #region Defines
        private string _port;
        private int _baudRate;
        private Parity _parity = Parity.None;
        private int _databits = 8;
        private StopBits _stopbits = StopBits.None;

        private SerialPort _serialPort;
        private Thread serThread;
        private double _PacketsRate;
        private DateTime _lastReceive;
        /*The Critical Frequency of Communication to Avoid Any Lag*/
        private int freqCriticalLimit = 20;
        #endregion

        #region Custom Events
        public event EventHandler<DataStreamEventArgs> SerialDataReceived;
        #endregion

        #region Constructors
        public SerialClient(string port)
        {
            _port = port;
            _baudRate = 9600;
        }
        public SerialClient(string Port, int baudRate) : this(Port)
        {
            _baudRate = baudRate;
        }       
        public SerialClient(string Port, int baudRate, Parity parity, int dataBits, StopBits stopBits, int frequency) : this(Port, baudRate)
        {
            _parity = parity;
            _databits = dataBits;
            _stopbits = stopBits;
            freqCriticalLimit = Math.Max(1, frequency);
        }
        #endregion

        #region Methods
        #region Port Control
        public bool OpenConn()
        {
            try
            {

                if (_serialPort == null)
                    _serialPort = new SerialPort(_port, _baudRate, _parity, _databits, _stopbits);

                if (!PortAvailable())
                    throw new SerialException(string.Format("Port is not available: {0}", _port));

                if (!_serialPort.IsOpen)
                {
                    _serialPort.ReadTimeout = -1;
                    _serialPort.WriteTimeout = -1;

                    _serialPort.Open();

                    if (!_serialPort.IsOpen)
                        throw new SerialException(string.Format("Could not open serial port: {0}", _port));

                    _PacketsRate = 0;
                    _lastReceive = DateTime.MinValue;

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
        public bool OpenConn(string port, int baudRate)
        {
            _port = port;
            _baudRate = baudRate;

            return OpenConn();
        }
        public void CloseConn()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                serThread.Abort();

                if (serThread.ThreadState == ThreadState.Aborted)
                    _serialPort.Close();
            }
        }
        public bool ResetConn()
        {
            CloseConn();
            return OpenConn();
        }
        public bool PortAvailable()
        {
            return SerialPort.GetPortNames().Contains(_port);
        }
        public bool IsAlive()
        {
            return _serialPort.IsOpen;
        }
        #endregion
        #region Transmit/Receive
        public void Transmit(byte[] packet)
        {
            _serialPort.Write(packet, 0, packet.Length);
        }
        #endregion
        #region IDisposable Methods
        public void Dispose()
        {
            CloseConn();

            if (_serialPort != null)
            {

                _serialPort.Dispose();
                _serialPort = null;
            }
        }
        #endregion
        #endregion

        #region Threading Loops

        public bool _addFreq(int freq)
        {
            if (freqCriticalLimit == 1)
                return false;
            freqCriticalLimit = Math.Max(freqCriticalLimit+freq, 1);
            return true;
        }

        private async void SerialReceiving()
        {
            int count;
            while (true)
            {
                try
                {
                    count = _serialPort.BytesToRead;

                    /*Get Sleep Inteval*/
                    TimeSpan tmpInterval = (DateTime.Now - _lastReceive);

                    /*Form The Packet in The Buffer*/
                    byte[] buf = new byte[count];
                    int readBytes = 0;
                    if (count > 0)
                        readBytes = await _serialPort.BaseStream.ReadAsync(buf, 0, count);

                    OnSerialReceiving(buf); // Handlers must deal with zero byte case

                    #region Frequency Control
                    _PacketsRate = ((_PacketsRate + readBytes) / 2);
                    _lastReceive = DateTime.Now;

                    if (tmpInterval.Milliseconds > 0 && (double)(readBytes + _serialPort.BytesToRead) / 2 <= _PacketsRate)
                        Thread.Sleep(tmpInterval.Milliseconds > freqCriticalLimit ? freqCriticalLimit : tmpInterval.Milliseconds);
                    #endregion
                }
                catch (Exception)
                {
                    break;
                }
            }

        }
        #endregion

        #region Custom Events Invoke Functions
        private void OnSerialReceiving(byte[] res)
        {
            if (SerialDataReceived != null)
            {
                SerialDataReceived(this, new DataStreamEventArgs() { Data = res });
            }
        }
        #endregion

    }
}