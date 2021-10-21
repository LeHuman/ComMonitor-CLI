using ScottPlot;
using System.Drawing;
using ScottPlot.Plottable;
using System.Collections.Generic;
using System.Windows;

namespace ComPlotter.Plot
{
    public class PlotSeriesManager
    {
        internal readonly WpfPlot WpfPlot;
        internal readonly PlotControl PlotController;
        internal readonly List<PlotSeries> SeriesList = new();

        private int ip = -1;

        private Color NextColor() // TODO: more colors!
        {
            ip++;
            ip %= WpfPlot.Plot.Palette.Count();
            return WpfPlot.Plot.Palette.GetColor(ip);
        }

        internal void SetRange(int value)
        {
            foreach (PlotSeries s in SeriesList)
            {
                s.Range = value;
            }
        }

        internal void Clear()
        {
            foreach (PlotSeries s in SeriesList)
            {
                s.Clear();
            }
        }

        internal void Update()
        {
            foreach (PlotSeries s in SeriesList)
            {
                s.Update();
            }
        }

        internal void RemovePlot(IPlottable plottable)
        {
            _ = WpfPlot.Dispatcher.InvokeAsync(() => { WpfPlot.Plot.Remove(plottable); }).Wait();
        }

        internal void RemoveSeries(PlotSeries plotSeries)
        {
            plotSeries.Invalid = true;
            plotSeries.Clear();
            RemovePlot(plotSeries.SignalPlot);
            _ = WpfPlot.Dispatcher.InvokeAsync(() => { _ = SeriesList.Remove(plotSeries); PlotController.SeriesListBox.Items.Remove(plotSeries); }).Wait();
        }

        internal SignalPlot NewPlot(double[] DataPointer)
        {
            SignalPlot signalPlot = null;
            _ = WpfPlot.Dispatcher.InvokeAsync(() => { signalPlot = WpfPlot.Plot.AddSignal(DataPointer); }).Wait();
            return signalPlot;
        }

        internal PlotSeries CreateSeries(string Name, int Range, bool Growing)
        {
            PlotSeries ps = null;

            _ = WpfPlot.Dispatcher.InvokeAsync(() => { ps = new(this, Name, NextColor(), Range, Growing); SeriesList.Add(ps); PlotController.SeriesListBox.Items.Add(ps); }).Wait();

            if (PlotController.SlowMode) // TODO: Show warning that new plots are being hidden
            {
                ps.IsVisible = false;
            }

            return ps;
        }

        public PlotSeriesManager(PlotControl PlotController)
        {
            this.PlotController = PlotController;
            WpfPlot = PlotController.WpfPlot;
        }
    }
}
