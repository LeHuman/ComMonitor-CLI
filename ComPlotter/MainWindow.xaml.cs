using System.Windows;
using System.Windows.Media.Animation;
using ComPlotter.Plot;
using MahApps.Metro.Controls;

namespace ComPlotter
{
    public partial class MainWindow : MetroWindow
    {
        public PlotManager PlotManager { get; private set; }

        private bool SettingsToggle, AboutToggle;
        private readonly Storyboard CloseSettings, OpenSettings, CloseAbout, OpenAbout;

        public MainWindow()
        {
            InitializeComponent();

            PlotManager = new(MainPlot, SeriesListBox);

            DataContext = this;

            new Test(PlotManager);

            CloseSettings = MainGrid.Resources["SettingsPanelClose"] as Storyboard;
            OpenSettings = MainGrid.Resources["SettingsPanelOpen"] as Storyboard;
            CloseAbout = MainGrid.Resources["AboutPanelClose"] as Storyboard;
            OpenAbout = MainGrid.Resources["AboutPanelOpen"] as Storyboard;

            CloseAbout.Begin();
            CloseSettings.Begin();
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
            SettingsToggle = !SettingsToggle;
            if (SettingsToggle)
            {
                OpenSettings.Begin();
            }
            else
            {
                CloseSettings.Begin();
            }
        }

        private void AboutBtn(object sender, RoutedEventArgs e)
        {
            AboutToggle = !AboutToggle;
            if (AboutToggle)
            {
                OpenAbout.Begin();
            }
            else
            {
                CloseAbout.Begin();
            }
        }
    }
}
