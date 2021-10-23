using System.Windows;
using System.Windows.Media.Animation;
using ComPlotter.Plot;
using MahApps.Metro.Controls;

namespace ComPlotter
{
    public partial class MainWindow : MetroWindow
    {
        public PlotManager PlotManager { get; private set; }

        private readonly Storyboard ToggleSettings, ToggleAbout;

        public MainWindow()
        {
            InitializeComponent();

            PlotManager = new(MainPlot, SeriesListBox);

            DataContext = this;

            ToggleSettings = MainGrid.Resources["SettingsPanelToggle"] as Storyboard;
            ToggleAbout = MainGrid.Resources["AboutPanelToggle"] as Storyboard;

            //new Test(PlotManager);
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

        private void AboutBtn(object sender, RoutedEventArgs e)
        {
            ToggleAbout.Begin();
        }
    }
}
