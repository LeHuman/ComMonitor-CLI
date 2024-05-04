using ComPlotter.Plot;
using ComPlotter.Util;
using ComPlotter.Wpf;
using MahApps.Metro.Controls;
using MaterialDesignColors;
using Pipe;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using static Pipe.PipeDataServer;

namespace ComPlotter {

    public partial class MainWindow : MetroWindow {
        public Toaster Toaster { get; }
        public SettingsPanel Settings { get; }
        public PlotManager PlotManager { get; private set; }

        public AssemblyInformation AssemblyInformation { get; private set; }

        private bool AutoPurge;
        private PipeDataServer SerialPipe;
        private readonly CheckBox VisibleCheck;
        private readonly Storyboard ToggleSettings, ToggleAbout, ToggleList;
        private static readonly string MessagePattern = @"([ \w\(\)\[\],:@#$%^&*!~\/\\\-\+]+)[ \t]([+-]?(?>[0-9]+(?>[.][0-9]*)?|[.][0-9]+))[\r\n]+";

        public MainWindow() {
            CheckForInstance();

            InitializeComponent();

            PlotManager = new(MainPlot, SeriesListBox);
            MainPlot.Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;

            DataContext = this;

            ToggleSettings = MainGrid.Resources["SettingsPanelToggle"] as Storyboard;
            ToggleAbout = MainGrid.Resources["AboutPanelToggle"] as Storyboard;
            ToggleList = MainGrid.Resources["ListPanelToggle"] as Storyboard;

            AssemblyInformation = new(Assembly.GetExecutingAssembly())
            {
                RepoLink = new("Github", new("https://github.com/LeHuman/ComMonitor-CLI"))
            };
            //AssemblyInformation.Image = (BitmapImage)FindResource("COM_Plotter_Icon256.png");
            Settings = new SettingsPanel(SettingsCheckList);

            CheckBox SlowModeCheck = Settings.AddCheckBox("Disable Slow Mode");
            SlowModeCheck.Status = "Slow mode helps prevent freezing on high loads";
            SlowModeCheck.SetCallback(IsChecked => { PlotManager.Control.DisableSlowMode(IsChecked); });

            CheckBox BenchmarkCheck = Settings.AddCheckBox("Enable Benchmark");
            BenchmarkCheck.Status = "Show the render time for the last rendered frame";
            BenchmarkCheck.SetCallback(IsChecked => { PlotManager.Control.EnableBenchmark(IsChecked); });

            CheckBox StaleCheck = Settings.AddCheckBox("Remove Stale Series");
            StaleCheck.Status = "Automaticlly purge the data of series that have not sent data in a while";
            StaleCheck.SetCallback(IsChecked => PlotManager.Control.StaleSeries = IsChecked);

            CheckBox PurgeCheck = Settings.AddCheckBox("Auto Purge");
            PurgeCheck.Status = "Automaticlly purge the data of ports that have been disconnected";
            PurgeCheck.SetCallback(IsChecked => AutoPurge = IsChecked);

            CheckBox NotifyCheck = Settings.AddCheckBox("Enable Notifications", true);
            NotifyCheck.Status = "Notifications temporarily show info on the bottom left of the application";
            NotifyCheck.SetCallback(IsChecked => { Toaster.Enable = IsChecked; });

            VisibleCheck = Settings.AddCheckBox("New series are visible", true);
            VisibleCheck.Status = "When a new series is added, automaticlly show it";
            VisibleCheck.SetCallback(IsChecked => { PlotManager.Control.VisibleDefault = IsChecked; });

            Toaster = new(
                FindResource("MahApps.Brushes.AccentBase") as SolidColorBrush,
                new(SwatchHelper.Lookup[MaterialDesignColor.Green600]),
                new(SwatchHelper.Lookup[MaterialDesignColor.Red600]),
                new(SwatchHelper.Lookup[MaterialDesignColor.Amber600]),
                new(SwatchHelper.Lookup[MaterialDesignColor.Purple600]
            ));

            SetupPipes();

            Loaded += (_, _) => PlotManager.Start();
        }

        private static void CheckForInstance() {
            if (Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location)).Length > 1) {
                Debug.WriteLine("Instance of ComPlotter already exists, use the same instead instance");
                Process.GetCurrentProcess().Kill();
            }

            PingPipe SingleInstanceCheck = new("ComPlotter");
            SingleInstanceCheck.SetCallback(Process.GetCurrentProcess().Kill);
            if (!SingleInstanceCheck.Ping())
                SingleInstanceCheck.ListenForPing();
        }

        private static void ReceiveByteString(PlotGroup pg, byte[] Data) {
            string msg = Encoding.UTF8.GetString(Data);
            RegexOptions options = RegexOptions.Multiline;
            foreach (Match m in Regex.Matches(msg, MessagePattern, options).Cast<Match>()) {
                if (m.Groups.Count == 3)
                    pg.Update(m.Groups[1].Value, double.Parse(m.Groups[2].Value));
            }
        }

        private DelegateSerialData ReceiveInfo(string PipeName, int MaxBytes, string MetaData) {
            Toaster.WarnToast($"Pipe Info Received {PipeName}");
            PlotGroup pg = PlotManager.NewGroup(PipeName);
            return Data => { ReceiveByteString(pg, Data); };
        }

        private void PrintPipeStatus(string PipeName, bool IsConnected) {
            if (IsConnected) {
                Toaster.SuccessToast($"Pipe Opened {PipeName}");
            } else {
                Toaster.ErrorToast($"Pipe Closed {PipeName}");
                if (AutoPurge) {
                    PlotManager.RemoveGroup(PipeName);
                }
            }
        }

        private void SetupPipes() {
            SerialPipe = new PipeDataServer(ReceiveInfo);
            SerialPipe.SetStatusListener(PrintPipeStatus);
            if (!SerialPipe.Start()) {
                Toaster.ErrorToast("Unable to wait for open system pipe for serial data");
            } else {
                Toaster.Toast("Waiting for pipe", 1000);
            }
        }

        private void Dispatcher_ShutdownStarted(object sender, EventArgs e) {
            SerialPipe?.Stop();
            PlotManager?.Stop();
        }

        private void UpdateSelectedPlotSeries(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            if (e.AddedItems.Count > 0 && !((PlotSeries)e.AddedItems[0]).Invalid) {
                PlotManager.Control.SelectedPlot = (PlotSeries)e.AddedItems[0];
            }
        }

        private void UpdateSampleSize(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (PlotManager != null) {
                PlotManager.Control.Range = 2 << (int)e.NewValue;
            }
        }

        private void UpdateMouseSelection(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            PlotManager.UpdateHighlight();
        }

        private void ListBtn(object sender, RoutedEventArgs e) {
            ToggleList.Begin();
        }

        private void AboutBtn(object sender, RoutedEventArgs e) {
            ToggleAbout.Begin();
        }

        private void SettingsBtn(object sender, RoutedEventArgs e) {
            ToggleSettings.Begin();
        }

        private void HyperlinkClick(object sender, RequestNavigateEventArgs e) {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void ListView_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e) {
            ((System.Windows.Controls.ScrollViewer)e.OriginalSource).ScrollToBottom();
        }

        private void SeriesDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            PlotSeries ps = null;
            try {
                if (sender is System.Windows.Controls.ListBox box) {
                    ps = (PlotSeries)box.SelectedItem;
                } else if (sender is System.Windows.Controls.CheckBox box1) {
                    SeriesListBox.UnselectAll();
                    ps = (PlotSeries)box1.DataContext;
                }
            } catch (InvalidCastException) {
            }
            if (ps != null) {
                PlotManager.FocusSeries(ps);
                VisibleCheck.SetChecked(false);
                e.Handled = true;
            }
        }
    }
}
