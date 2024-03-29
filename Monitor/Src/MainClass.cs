﻿using CommandLine;
using ComMonitor.Log;
using ComMonitor.MsgMap;
using Pipe;
using ComMonitor.Serial;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ComMonitor.Terminal;

namespace ComMonitor.Main {

    public static class MainClass {

        #region defines

        private static int MAX_RETRY = 500; // We should give it a limit, just in case
        private static int retries = MAX_RETRY;
        private static bool reconnect = false;
        private static bool initalWait = false;

        private static string PriorityPipeName = "ComMonitor";
        private static PingPipe priorityPipe;

        private static PipeDataClient SerialPipe;

        private static string connectStr, waitStr, retryStr;
        private static readonly int[] waitAnimTime = { 80, 40, 30, 30, 20, 20, 10, 20, 20, 30, 30, 40 };
        private static readonly string[] waitAnim = { "        ", "-       ", "--      ", "---     ", "----    ", " ----   ", "  ----  ", "   ---- ", "    ----", "     ---", "      --", "       -" };

        #endregion defines

        #region Methods

        #region Runtime Methods

        private static void RetryWait(bool firstWait = false) {
            if (SerialClient.PortAvailable())
                return;
            Thread.Sleep(100);
            Term.ColorConsole(ConsoleColor.Blue);
            FileLog.Flush();
            string waitMessage = firstWait ? waitStr : retryStr;
            do {
                for (int i = 0; i < waitAnimTime.Length; i++) {
                    Console.Write($"\r{waitMessage}{waitAnim[i]}");
                    Thread.Sleep(waitAnimTime[i]);
                    if (SerialClient.PortAvailable())
                        break;
                }
            } while (!SerialClient.PortAvailable());
            Console.Write("\r");
            Console.Write(string.Concat(Enumerable.Repeat(" ", waitMessage.Length + waitAnim[0].Length)));
            Console.Write("\r");
            retries--;
            if (retries == 0) {
                throw new Exception("Max number of retries reached");
            }
        }

        public static void Run() {
            Term.ColorSingle(ConsoleColor.Yellow, connectStr);

            if (!SerialClient.PortAvailable()) {
                if (initalWait)
                    RetryWait(true);
                else
                    throw new SerialException($"Unable to find port: {SerialClient.PortName}");
            }

            while (true) {
                try {
                    if (SerialClient.OpenConn()) {
                        retries = MAX_RETRY; // Reset retry counter
                        Term.ColorSingle(ConsoleColor.Green, "\r------[Connect]-------");
                        ConsoleInput.Enable(true);
                        while (SerialClient.IsAlive()) {
                            Thread.Sleep(400);
                        }
                        ConsoleInput.Enable(false);
                        Term.ColorSingle(ConsoleColor.Red, "-----[Disconnect]-----");
                    }
                } catch (SerialException e) {
                    Term.WriteLine(e.Message);
                    SerialClient.Dispose();
                    return;
                } finally {
                    SerialClient.Dispose();
                }
                if (!reconnect)
                    break;
                ConsoleInput.Enable(false);
                RetryWait();
            }
        }

        #endregion Runtime Methods

        #region Exception Handlers

        private static void PriorityStop() {
            try {
                FileLog.Flush();
            } catch (Exception) {
            }
            throw new SerialException($"Another instance has taken priority over the current port: {SerialClient.PortName}");
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e) {
            Term.ExceptionPrint((Exception)e.ExceptionObject);
            //ConsoleMode.Set(ConsoleMode.ENABLE_QUICK_EDIT, true);
            //Thread.Sleep(10000);
            Environment.Exit(1);
        }

        #endregion Exception Handlers

        #endregion Methods

        private static void SetOptions(Options options) {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

            retries = MAX_RETRY;
            initalWait = options.Wait;
            reconnect = options.Reconnect;
            MAX_RETRY = options.MaxRetries;

            DataType dataType = options.SetDataType == DataType.None ? DataType.Ascii : options.SetDataType;

            Term.ColorEnable(!options.SetColor);
            Term.EnableInput(options.EnableInput, options.DisableInputPrompt);

            JSONMap.LoadJSONMap(options.JsonPath, options.JsonBlock, SerialParser.MaxBytes);

            if (dataType == DataType.Mapped && !JSONMap.Loaded)
                throw new ArgumentException("Mapped Mode selected, but no map was loaded");

            Term.EnableLogColor(dataType == DataType.Ascii || (JSONMap.Loaded && dataType == DataType.Mapped));

            #region Setup SerialClient

            SerialClient.Setup(options.PortName.ToUpper(), options.BaudRate, options.SetParity, options.SetDataBits, options.SetStopBits, options.Frequency);
            SerialClient.SetWriteTimeout(options.WaitTimeout);
            SerialClient.SerialDataReceived += SerialParser.LoadParser(dataType, options.SetMaxBytes);

            #endregion Setup SerialClient

            #region File Logging

            if (options.Logging != null) {
                FileLog.SetFile(options.Logging, options.SingleLogging);
                FileLog.TimestampEnabled = options.LogTime;
                if (dataType == DataType.Mapped)
                    JSONMap.LogMap();
            }

            #endregion File Logging

            #region Priority Queue Setup

            PriorityPipeName += SerialClient.PortName;
            priorityPipe = new PingPipe(PriorityPipeName);
            if (options.Priority) {
                if (priorityPipe.Ping()) {
                    Term.WriteLine("Notified priority to another ComMonitor");
                } else {
                    Term.WriteLine("No ComMonitor to take priority over");
                }
            }
            if (!priorityPipe.ListenForPing(options.Priority ? 10 : 5)) {
                Console.WriteLine("Warning: Unable to open priority notifier.");
                Console.WriteLine("Is another ComMonitor open?");
            }

            priorityPipe.SetCallback(PriorityStop);

            #endregion Priority Queue Setup

            #region Serial Data Pipe

            if (options.EnableSerialPipe || options.PlotData) {
                SerialPipe = new PipeDataClient(SerialClient.PortName, options.SetMaxBytes, dataType.ToString());
                // SerialClient.SerialDataReceived += (sender, e) => { SerialPipe.SendData(e.Data); }; // For piping raw data
                if (!SerialPipe.Start()) {
                    Term.ColorSingle(ConsoleColor.Yellow, "Unable to wait for open system pipe for serial data");
                    Term.ColorSingle(ConsoleColor.Yellow, "Is another ComMonitor open?");
                } else {
                    SerialParser.SetParsedDataListener(SerialPipe.SendData);
                    if (options.PlotData) {
                        Process[] processes = Process.GetProcessesByName("ComPlotter"); // TODO: Only start if specifically asked to, otherwise just wait
                        if (processes.Length == 0) {
                            try {
                                Process.Start(options.PlotterPath);
                            } catch (SystemException) {
                                Term.ColorSingle(ConsoleColor.Red, "Failed to launch Plotter, will connect when available");
                            }
                        } else {
                            Term.ColorSingle(ConsoleColor.Blue, "Plotter already opened");
                        } // TODO: add option to wait for plotter
                        //Term.ColorSingle(ConsoleColor.Yellow, "Waiting for system pipe for serial data");
                        //while (!SerialPipe.IsConnected) { }
                    }
                }
            }

            #endregion Serial Data Pipe

            waitStr = $"Waiting for connection to {SerialClient.PortName} ";
            retryStr = $"Retrying to connect to {SerialClient.PortName} ";
            connectStr = $"Connecting to {SerialClient.PortName} @ {SerialClient.BaudRate}\np:{SerialClient.Parity} d:{SerialClient.DataBits} s:{SerialClient.StopBits} cf:{SerialClient.FreqCriticalLimit} {(options.SetMaxBytes > 0 ? "j:" + options.SetMaxBytes : "")}\n";
        }

        private static void Main(string[] args) {
            //if (!ConsoleMode.Set(ConsoleMode.ENABLE_QUICK_EDIT, false))
            //    Console.WriteLine("Warning: Failed to disable console Quick Edit");

            #region Argument Parser

            Parser parser = new(with => { with.CaseInsensitiveEnumValues = true; with.AutoHelp = true; with.AutoVersion = true; with.HelpWriter = Console.Out; });
            ParserResult<Options> result = parser.ParseArguments<Options>(args);
            _ = result.WithParsed(SetOptions);

            if (result.Tag == ParserResultType.NotParsed)
                return;

            #endregion Argument Parser

            Run();

            //ConsoleMode.Set(ConsoleMode.ENABLE_QUICK_EDIT, true);
        }
    }
}
