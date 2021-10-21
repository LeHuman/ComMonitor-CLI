using System;
using ScottPlot;
using System.Drawing;
using ScottPlot.Plottable;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.Generic;

namespace ComPlotter.Plot
{
    public class PlotControl : INotifyPropertyChanged
    {
        public string HighlightedPointStatus;
        public PlotSeries SelectedPlot { get; set; }
        public ICommand ClearCommand { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public List<PlotSeries> SeriesList { get => SeriesManager.SeriesList; }

        public bool AutoRange { get => _AutoRange; set { _AutoRange = value; if (value) { _ = WpfPlot.Dispatcher.InvokeAsync(() => { WpfPlot.Plot.AxisAuto(0.1, 0.5); }).Wait(); } } }

        internal WpfPlot WpfPlot;
        internal bool _AutoRange = true;
        internal bool LowQualityRender = true;
        internal bool SlowMode = false;
        internal ScatterPlot HighlightedPoint;
        internal int _Range = PlotSeries.InitHeap;

        private PlotSeriesManager SeriesManager;

        internal PlotControl(WpfPlot WpfPlot)
        {
            this.WpfPlot = WpfPlot;
        }

        internal void Setup(PlotSeriesManager SeriesManager)
        {
            this.SeriesManager = SeriesManager;

            SetupCommands();

            HighlightedPoint = WpfPlot.Plot.AddPoint(0, 0);
            HighlightedPoint.Color = Color.White;
            HighlightedPoint.MarkerSize = 10;
            HighlightedPoint.MarkerShape = MarkerShape.openCircle;
            HighlightedPoint.IsVisible = false;
        }

        public int Range // TODO: enable range for individual series
        {
            get => _Range; set { _Range = value; SeriesManager.SetRange(value); }
        }

        public void SetHighlight(double mouseCoordX)
        {
            if (SelectedPlot == null || !SelectedPlot.IsVisible)
            {
                HighlightedPoint.IsVisible = false;
                return;
            }
            (double pointX, double pointY, int _) = SelectedPlot.GetPointNearestX(mouseCoordX);
            HighlightedPoint.Xs[0] = pointX;
            HighlightedPoint.Ys[0] = pointY;
            HighlightedPoint.IsVisible = true;
            HighlightedPointStatus = $"Highlight: {Math.Round(pointY, 8)}";
            OnPropertyChanged("HighlightedPointStatus");
        }

        internal virtual void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private void SetupCommands()
        {
            ClearCommand = new CommandBinding(SeriesManager.Clear);
        }
    }
}
