using ScottPlot;
using ScottPlot.Plottable;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Controls;

namespace ComPlotter.Plot
{
    public class PlotGroup
    {
        public string Name { get; }
        public List<PlotSeries> SeriesList { get; } = new();

        internal readonly WpfPlot WpfPlot;
        internal readonly PlotControl PlotController;

        private bool Invalid;
        private int ip = -1, ipb;
        private readonly ListBox SeriesListBox;
        private readonly Random rand = new(420);

        public PlotGroup(string Name, PlotControl PlotController, ListBox SeriesListBox = null)
        {
            WpfPlot = PlotController.WpfPlot;

            this.Name = Name;
            this.SeriesListBox = SeriesListBox;
            this.PlotController = PlotController;
        }

        internal void Delete()
        {
            if (Invalid)
            {
                return;
            }

            Invalid = true;

            foreach (PlotSeries s in SeriesList.ToArray())
            {
                RemoveSeries(s);
            }
        }

        internal void ReloadColors()
        {
            if (Invalid)
            {
                return;
            }

            ip = -1; ipb = 0;
            foreach (PlotSeries s in SeriesList)
            {
                s._Color = NextColor();
                s.SignalPlot.Color = s._Color;
            }
        }

        internal void SetRange(int value)
        {
            if (Invalid)
            {
                return;
            }

            foreach (PlotSeries s in SeriesList)
            {
                s.Range = value;
            }
        }

        internal void Clear()
        {
            if (Invalid)
            {
                return;
            }

            foreach (PlotSeries s in SeriesList)
            {
                s.Clear();
            }
        }

        internal void Update()
        {
            if (Invalid)
            {
                return;
            }

            foreach (PlotSeries s in SeriesList)
            {
                s.Update();
            }
        }

        internal void RemovePlot(IPlottable plottable)
        {
            PlotController.RunOnUIThread(() => { WpfPlot.Plot.Remove(plottable); });
        }

        internal void RemoveSeries(PlotSeries plotSeries)
        {
            plotSeries.Invalid = true;
            plotSeries.Clear();
            RemovePlot(plotSeries.SignalPlot);
            PlotController.RunOnUIThread(() => { _ = SeriesList.Remove(plotSeries); SeriesListBox?.Items.Remove(plotSeries); });
        }

        internal SignalPlot NewPlot(double[] DataPointer)
        {
            SignalPlot signalPlot = null;
            PlotController.RunOnUIThread(() => { signalPlot = WpfPlot.Plot.AddSignal(DataPointer); });
            return signalPlot;
        }

        internal PlotSeries CreateSeries(string Name, int Range = 0, bool Growing = false)
        {
            if (Invalid)
            {
                return null;
            }

            PlotSeries ps = null;
            if (Range <= 1)
            {
                Range = PlotController._Range;
            }

            Name = $"{this.Name} : {Name}";

            PlotController.RunOnUIThread(() => { ps = new(this, Name, NextColor(), Range, Growing); SeriesList.Add(ps); SeriesListBox?.Items.Add(ps); });

            if (PlotController.SlowMode) // TODO: Show warning that new plots are being hidden
            {
                ps.IsVisible = false;
            }

            return ps;
        }

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
    }
}
