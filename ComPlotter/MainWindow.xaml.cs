using ComPlotter.Plot;
using ComPlotter.Util;
using ComPlotter.Wpf;
using MahApps.Metro.Controls;
using MaterialDesignColors;
using Serial;
using System;
using Pipe;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using static Pipe.PipeDataServer;
using System.Text;
using ControlzEx.Standard;
using ScottPlot.Drawing.Colormaps;

namespace ComPlotter {

    public partial class MainWindow : MetroWindow {
        public Toaster Toaster { get; }
        public SettingsPanel Settings { get; }
        public PlotManager PlotManager { get; private set; }

        public AssemblyInformation AssemblyInformation { get; private set; }

        private PipeDataServer SerialPipe;
        private readonly Storyboard ToggleSettings, ToggleAbout;

        public MainWindow() {
            InitializeComponent();

            PlotManager = new(MainPlot, SeriesListBox);

            DataContext = this;

            ToggleSettings = MainGrid.Resources["SettingsPanelToggle"] as Storyboard;
            ToggleAbout = MainGrid.Resources["AboutPanelToggle"] as Storyboard;

            AssemblyInformation = new(Assembly.GetExecutingAssembly());
            AssemblyInformation.RepoLink = new("Github", new("https://github.com/LeHuman/ComMonitor-CLI"));

            Settings = new SettingsPanel(SettingsCheckList);

            CheckBox SlowModeCheck = Settings.AddCheckBox("Disable Slow Mode");
            SlowModeCheck.Status = "Slow mode helps prevent freezing on high loads";
            SlowModeCheck.SetCallback(IsChecked => { PlotManager.Control.DisableSlowMode(IsChecked); });

            CheckBox BenchmarkCheck = Settings.AddCheckBox("Enable Benchmark");
            BenchmarkCheck.Status = "Show the render time for the last rendered frame";
            BenchmarkCheck.SetCallback(IsChecked => { PlotManager.Control.EnableBenchmark(IsChecked); });

            // TODO: implement the following
            CheckBox PurgeCheck = Settings.AddCheckBox("Auto Purge");
            PurgeCheck.Status = "Automaticlly purge the data of ports that have been disconnected";

            CheckBox NotifyCheck = Settings.AddCheckBox("Enable Notifications", true);
            NotifyCheck.Status = "Notifications temporarily show info on the bottom left of the application";
            NotifyCheck.SetCallback(v => { Toaster.Enable = v; });

            Toaster = new(FindResource("MahApps.Brushes.AccentBase") as SolidColorBrush, new(SwatchHelper.Lookup[MaterialDesignColor.Red600]), new(SwatchHelper.Lookup[MaterialDesignColor.Amber600]), new(SwatchHelper.Lookup[MaterialDesignColor.Purple600]));

            SetupPipes();

            //new Test(PlotManager, Toaster);
        }

        private string PortName;
        private DataType dataType;

        private static readonly string MessagePattern = @"([ \w\(\)\[\],:@#$%^&*!~\/\\\-\+]+)[ \t]([+-]?(?>[0-9]+(?>[.][0-9]*)?|[.][0-9]+))[\r\n]+";

        private void ReceiveByteString(PlotGroup pg, byte[] Data) {
            string msg = Encoding.UTF8.GetString(Data);
            RegexOptions options = RegexOptions.Multiline;
            foreach (Match m in Regex.Matches(msg, MessagePattern, options)) {
                if (m.Groups.Count == 3)
                    pg.Update(m.Groups[1].Value, double.Parse(m.Groups[2].Value));
            }
        }

        private DelegateSerialData ReceiveInfo(string PipeName, int MaxBytes, string MetaData) {
            PortName = PipeName;
            dataType = Enum.Parse<DataType>(MetaData);
            Toaster.DebugToast($"PipeInfoReceived {PipeName}");
            PlotGroup pg = PlotManager.NewGroup(PipeName);
            //SerialParser parser = new(Msg => { PlotData(PipeName, Msg); }, dataType, MaxBytes);
            return Data => { ReceiveByteString(pg, Data); };
        }

        private void SetupPipes() {
            SerialPipe = new PipeDataServer(ReceiveInfo);
            if (!SerialPipe.Start()) {
                Toaster.ErrorToast("Unable to wait for open system pipe for serial data");
            } else {
                Toaster.Toast("Waiting for pipe");
            }
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
    }
}
