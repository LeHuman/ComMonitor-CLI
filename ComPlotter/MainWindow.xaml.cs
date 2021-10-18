using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using MahApps.Metro.Controls;

namespace ComPlotter
{
    public partial class MainWindow : MetroWindow
    {
        public PlotControl PlotController { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            PlotController = new(MainPlot);

            DataContext = this;

            TestTask();
        }

        private readonly Random rand = new(0);

        private PlotSeries signalPlot, signalPlot2, signalPlot3;

        private DispatcherTimer _updateDataTimer;

        private void TestTask()
        {
            // plot the data array only once
            signalPlot = PlotController.SeriesManager.Create("First");
            signalPlot2 = PlotController.SeriesManager.Create("Seconddvzx44vdx4v56z🙄");
            signalPlot3 = PlotController.SeriesManager.Create("Amogs");
            PlotController.SeriesManager.Create("Amo4fsgs");
            PlotController.SeriesManager.Create("Amoag3ag4fsgs");
            PlotController.SeriesManager.Create("Amoag3aggfgfsdgdg4fsgs");
            PlotController.SeriesManager.Create("Amoag3aggfgfsdgdg465fds464fsgs");
            PlotController.SeriesManager.Create("e");
            PlotController.SeriesManager.Create("");
            PlotController.SeriesManager.Create("fdsgf6");

            // create a timer to modify the data
            _updateDataTimer = new DispatcherTimer();
            _updateDataTimer.Interval = TimeSpan.FromMilliseconds(1);
            _updateDataTimer.Tick += UpdateTestData;
            _updateDataTimer.Start();

            Closed += (sender, args) =>
            {
                PlotController?.Stop();
                _updateDataTimer?.Stop();
            };
        }

        private readonly Stopwatch Stopwatch = Stopwatch.StartNew();

        private void UpdateTestData(object sender, EventArgs e)
        {
            double s = Math.Sin(Stopwatch.Elapsed.TotalSeconds) + rand.NextDouble() / 2;
            double r = Math.Round(rand.NextDouble() - .5, 3);

            foreach (PlotSeries ps in PlotController.SeriesManager.Series)
                if (rand.Next(100) > rand.Next(100))
                    ps.Update((rand.NextDouble() - 0.5) * rand.NextDouble());

            if (rand.Next(100) > rand.Next(100))
                signalPlot.Update(s);
            if (rand.Next(100) > rand.Next(100))
                signalPlot2.Update(r);
            if (rand.Next(100) > 90)
                signalPlot3.Update(s + r);
        }

        private void DisableAutoAxis(object sender, RoutedEventArgs e)
        {
            MainPlot.Plot.AxisAuto(verticalMargin: .5);
            ScottPlot.AxisLimits oldLimits = MainPlot.Plot.GetAxisLimits();
            MainPlot.Plot.SetAxisLimits(xMax: oldLimits.XMax + 1000);
        }

        private void UpdateSelectedPlotSeries(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            PlotController.SeriesManager.SelectedPlot = (PlotSeries)e.AddedItems[0];
        }

        private void LaunchGitHubSite(object sender, RoutedEventArgs e)
        {
        }

        private void DeployCupCakes(object sender, RoutedEventArgs e)
        {
        }
    }
}
