using ScottPlot;
using ScottPlot.Plottable;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows;

namespace ComPlotter
{
    public class PlotSeriesManager : INotifyPropertyChanged
    {
        // public bool Sync { get; set; } = true; // TODO: Implement "Syncing" of data

        public PlotSeries SelectedPlot { get; set; }
        public List<PlotSeries> Series { get; } = new();

        internal readonly WpfPlot WpfPlot;
        internal ScatterPlot HighlightedPoint;

        private int ip = -1;

        private string _HighlightedPointStatus;
        private PlotControl plotControl;

        public string HighlightedPointStatus
        {
            get { return _HighlightedPointStatus; }
            set
            {
                _HighlightedPointStatus = value;
                OnPropertyChanged("HighlightedPointStatus");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private Color NextColor()
        {
            ip++;
            ip %= WpfPlot.Plot.Palette.Count();
            return WpfPlot.Plot.Palette.GetColor(ip);
        }

        internal void SetRange(int value)
        {
            foreach (PlotSeries s in Series)
            {
                s.Range = value;
            }
        }

        internal void Clear()
        {
            foreach (PlotSeries s in Series)
            {
                s.Clear();
            }
        }

        internal void Reload(PlotSeries Reloadee)
        {
            WpfPlot.Plot.Remove(Reloadee.SignalPlot);
            bool vis = Reloadee.SignalPlot == null || Reloadee.SignalPlot.IsVisible;
            Reloadee.SignalPlot = WpfPlot.Plot.AddSignal(Reloadee.Data);
            Reloadee.SignalPlot.IsVisible = vis;
            Reloadee.SignalPlot.MaxRenderIndex = 0;
            Reloadee.SignalPlot.Color = Reloadee._Color;
        }

        public PlotSeries Create(string Name, int Range = 512, bool Growing = false)
        {
            PlotSeries ps = new(Name, Range, Growing, this, NextColor());
            Reload(ps);
            Series.Add(ps);

            if (plotControl.SetLowQ = Series.Count > WpfPlot.Plot.Palette.Count())
            {
                ps.IsVisible = false;
            }

            return ps;
        }

        public void SetHighlight(double mouseCoordX)
        {
            if (SelectedPlot == null || !SelectedPlot.SignalPlot.IsVisible)
            {
                HighlightedPoint.IsVisible = false;
                return;
            }
            (double pointX, double pointY, int _) = SelectedPlot.SignalPlot.GetPointNearestX(mouseCoordX);
            HighlightedPoint.Xs[0] = pointX;
            HighlightedPoint.Ys[0] = pointY;
            HighlightedPoint.IsVisible = true;
            HighlightedPointStatus = $"Highlight: {Math.Round(pointY, 8)}";
        }

        public PlotSeriesManager(PlotControl plotControl, WpfPlot WpfPlot)
        {
            this.plotControl = plotControl;
            this.WpfPlot = WpfPlot;

            HighlightedPoint = this.WpfPlot.Plot.AddPoint(0, 0);
            HighlightedPoint.Color = Color.White;
            HighlightedPoint.MarkerSize = 10;
            HighlightedPoint.MarkerShape = MarkerShape.openCircle;
            HighlightedPoint.IsVisible = false;
        }
    }
}
