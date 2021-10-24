using ComPlotter.Plot;
using ComPlotter.Util;
using ComPlotter.Wpf;
using MahApps.Metro.Controls;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

namespace ComPlotter
{
    public partial class MainWindow : MetroWindow
    {
        public SettingsPanel Settings { get; }
        public PlotManager PlotManager { get; private set; }

        public AssemblyInformation AssemblyInformation { get; private set; }

        private readonly Storyboard ToggleSettings, ToggleAbout;

        public MainWindow()
        {
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

            CheckBox NotifyCheck = Settings.AddCheckBox("Disable Notifications");

            new Test(PlotManager);
        }

        private void UpdateSelectedPlotSeries(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && !((PlotSeries)e.AddedItems[0]).Invalid)
            {
                PlotManager.Control.SelectedPlot = (PlotSeries)e.AddedItems[0];
            }
        }

        private void UpdateSampleSize(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PlotManager != null)
            {
                PlotManager.Control.Range = 2 << (int)e.NewValue;
            }
        }

        private void UpdateMouseSelection(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PlotManager.UpdateHighlight();
        }

        private void SettingsBtn(object sender, RoutedEventArgs e)
        {
            ToggleSettings.Begin();
        }

        private void HyperlinkClick(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void AboutBtn(object sender, RoutedEventArgs e)
        {
            ToggleAbout.Begin();
        }
    }
}
