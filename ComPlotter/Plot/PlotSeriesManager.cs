using ScottPlot;
using System.Drawing;
using ScottPlot.Plottable;
using System.Collections.Generic;
using System;

namespace ComPlotter.Plot
{
    public class PlotSeriesManager
    {
        internal readonly WpfPlot WpfPlot;
        internal readonly PlotControl PlotController;
        internal readonly List<PlotSeries> SeriesList = new();

        private int ip = -1, ipb = 0;
        private readonly Random rand = new(420);

        private Color NextColor()
        {
            int max = WpfPlot.Plot.Palette.Count();
            Color[] colors = WpfPlot.Plot.Palette.GetColors(max);

            ip++;

            if (ip == max)
            {
                ip = 0;
                ipb++;
            }

            Color color = colors[ip];

            for (int i = 0; i < ipb; i++)
            {
                color = color.Blend(colors[rand.Next(0, max)], 0.5 + (rand.NextDouble() / 2));
            }

            return color;
        }

        internal void ReloadColors()
        {
            ip = -1; ipb = 0;
            foreach (PlotSeries s in SeriesList)
            {
                s._Color = NextColor();
                s.SignalPlot.Color = s._Color;
            }
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
