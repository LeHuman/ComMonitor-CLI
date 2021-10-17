using MahApps.Metro.Controls;
using RealTimeGraphX.DataPoints;
using RealTimeGraphX.WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ComPlotter
{
    public partial class MainWindow : MetroWindow
    {
        public WpfGraphController<TimeSpanDataPoint, DoubleDataPoint> MultiController { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            MultiController = new WpfGraphController<TimeSpanDataPoint, DoubleDataPoint>();
            MultiController.Range.MinimumY = 0;
            MultiController.Range.MaximumY = 1080;
            MultiController.Range.MaximumX = TimeSpan.FromSeconds(10);
            MultiController.Range.AutoY = true;

            MultiController.DataSeriesCollection.Add(new WpfGraphDataSeries()
            {
                Name = "Series 1",
                Stroke = Colors.Red,
            });

            MultiController.DataSeriesCollection.Add(new WpfGraphDataSeries()
            {
                Name = "Series 2",
                Stroke = Colors.Green,
            });

            MultiController.DataSeriesCollection.Add(new WpfGraphDataSeries()
            {
                Name = "Series 3",
                Stroke = Colors.Blue,
            });

            MultiController.DataSeriesCollection.Add(new WpfGraphDataSeries()
            {
                Name = "Series 4",
                Stroke = Colors.Yellow,
            });

            MultiController.DataSeriesCollection.Add(new WpfGraphDataSeries()
            {
                Name = "Series 5",
                Stroke = Colors.Gray,
            });

            Application.Current.MainWindow.ContentRendered += (_, __) => Start();

            DataContext = this;
        }

        private void TestTask()
        {
            Stopwatch watch = new();
            watch.Start();

            Random r = new();

            while (true)
            {
                int y = r.Next();

                List<DoubleDataPoint> yy = new()
                {
                    y,
                    y + 20,
                    y + 40,
                    y + 60,
                    y + 80,
                };

                TimeSpan x = watch.Elapsed;

                List<TimeSpanDataPoint> xx = new()
                {
                    x,
                    x,
                    x,
                    x,
                    x
                };
                MultiController.PushData(xx, yy);

                //Thread.Sleep(30);
            }
        }

        private void Start()
        {
            _ = Task.Factory.StartNew(TestTask, TaskCreationOptions.LongRunning);
        }

        private void LaunchGitHubSite(object sender, RoutedEventArgs e)
        {
        }

        private void DeployCupCakes(object sender, RoutedEventArgs e)
        {
        }
    }
}
