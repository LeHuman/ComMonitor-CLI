using System;
using ScottPlot;
using System.Drawing;
using ScottPlot.Plottable;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Controls;

namespace ComPlotter.Plot
{
    public class PlotControl : INotifyPropertyChanged
    {
        public PlotSeries SelectedPlot { get; set; }
        public ICommand ClearCommand { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public string HighlightedPointStatus { get; private set; }
        public int Range { get => _Range; set { _Range = value; SetRangeAll(value); } } // TODO: enable range for individual series
        public bool AutoRange { get => _AutoRange; set { _AutoRange = value; if (value) { RunOnUIThread(() => { WpfPlot.Plot.AxisAuto(0.1, 0.5); }); } } }

        internal WpfPlot WpfPlot;
        internal bool _AutoRange = true;
        internal bool LowQualityRender = true;
        internal bool SlowMode;
        internal ListBox SeriesListBox;
        internal ScatterPlot HighlightedPoint;
        internal int _Range = PlotSeries.InitHeap;

        private readonly List<PlotGroup> PlotGroups;

        internal PlotControl(WpfPlot WpfPlot, List<PlotGroup> PlotGroups, ListBox listBox)
        {
            this.WpfPlot = WpfPlot;
            this.PlotGroups = PlotGroups;
            SeriesListBox = listBox; // TODO: Programmaticlly setup the listbox

            SetupCommands();

            HighlightedPoint = WpfPlot.Plot.AddPoint(0, 0);
            HighlightedPoint.Color = Color.White;
            HighlightedPoint.MarkerSize = 10;
            HighlightedPoint.MarkerShape = MarkerShape.openCircle;
            HighlightedPoint.IsVisible = false;
        }

        public void ClearAll()
        {
            foreach (PlotGroup SeriesManager in PlotGroups)
            {
                SeriesManager.Clear();
            }
        }

        public void SetRangeAll(int value)
        {
            foreach (PlotGroup SeriesManager in PlotGroups)
            {
                SeriesManager.SetRange(value);
            }
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

        internal void RunOnUIThread(Action action)
        {
            _ = WpfPlot.Dispatcher.InvokeAsync(action).Wait();
        }

        private void SetupCommands()
        {
            ClearCommand = new CommandBinding(ClearAll);
        }
    }
}
