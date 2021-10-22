using System.Windows;
using ComPlotter.Plot;
using MahApps.Metro.Controls;

namespace ComPlotter
{
    public partial class MainWindow : MetroWindow
    {
        public PlotManager PlotManager { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            PlotManager = new(MainPlot, SeriesListBox);

            DataContext = this;

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

        private void LaunchGitHubSite(object sender, RoutedEventArgs e)
        {
        }

        private void DeployCupCakes(object sender, RoutedEventArgs e)
        {
        }
    }
}
