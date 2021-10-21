using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
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

            PlotManager = new(MainPlot);
            PlotManager.SetListBox(SeriesListBox);

            DataContext = this;

            TestTask();
        }

        private Timer DataTimer;
        private readonly List<PlotSeries> DeleteSeriesList = new();
        private readonly List<PlotSeries> SeriesList = new();
        private readonly Random rand = new(0);
        private readonly Stopwatch Stopwatch = Stopwatch.StartNew();

        private void TestTask()
        {
            SeriesList.Add(PlotManager.CreateSeries("First"));

            Closed += (sender, args) =>
            {
                PlotManager.Stop();
                DataTimer.Stop();
            };

            DataTimer = new();
            DataTimer.AutoReset = false;
            DataTimer.Interval = 10;
            DataTimer.Elapsed += UpdateTestData;
            DataTimer.Start();
        }

        private void UpdateTestData(object sender, EventArgs e)
        {
            double s = Math.Sin(Stopwatch.Elapsed.TotalSeconds) + rand.NextDouble() / 2;
            double r = Math.Round(rand.NextDouble() - .5, 3);

            foreach (PlotSeries ps in SeriesList)
                if (rand.Next(100) > rand.Next(100))
                { ps.Update(Math.Sin(Stopwatch.Elapsed.TotalSeconds)); }
                else if (rand.Next(100) > rand.Next(100))
                { ps.Update(r); }
                else if (rand.Next(100) > rand.Next(100))
                { ps.Update(s + r); }
                else if (rand.Next(100) > rand.Next(100))
                { ps.Update((rand.NextDouble() - 0.5) * rand.NextDouble()); }
                else if (rand.Next(100) > rand.Next(100))
                { ps.Update(s); }
                else if (rand.Next(100) > 95)
                { DeleteSeriesList.Add(ps); }

            foreach (PlotSeries ps in DeleteSeriesList)
            {
                PlotManager.RemoveSeries(ps);
                SeriesList.Remove(ps);
            }

            DeleteSeriesList.Clear();

            if (rand.Next(100) > 95)
                SeriesList.Add(PlotManager.CreateSeries(rand.Next().ToString()));
            DataTimer.Start();
        }

        private void UpdateSelectedPlotSeries(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && !((PlotSeries)e.AddedItems[0]).Invalid)
                PlotManager.Control.SelectedPlot = (PlotSeries)e.AddedItems[0];
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
