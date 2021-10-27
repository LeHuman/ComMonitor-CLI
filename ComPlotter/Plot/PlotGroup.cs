using ScottPlot;
using ScottPlot.Drawing.Colormaps;
using ScottPlot.Plottable;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Controls;

namespace ComPlotter.Plot {

    public class PlotGroup {
        public string Name { get; }
        public List<PlotSeries> SeriesList { get; } = new();

        internal readonly WpfPlot WpfPlot;
        internal readonly PlotControl PlotController;

        private bool Invalid;
        private int ip = -1, ipb;
        private readonly ListBox SeriesListBox;
        private readonly Random rand = new(420);

        //private readonly List<PlotSeries> DeleteSeriesList = new();
        private Dictionary<string, PlotSeries> ItemMemory = new();

        public PlotGroup(string Name, PlotControl PlotController, ListBox SeriesListBox = null) {
            WpfPlot = PlotController.WpfPlot;

            this.Name = Name;
            this.SeriesListBox = SeriesListBox;
            this.PlotController = PlotController;
        }

        public void Update(string Name, double Value) {
            if (!ItemMemory.TryGetValue(Name, out PlotSeries plotSeries)) {
                plotSeries = CreateSeries(Name);
                ItemMemory.Add(Name, plotSeries);
            }
            plotSeries.Update(Value);
        }

        internal void Delete() {
            if (Invalid) {
                return;
            }

            Invalid = true;

            foreach (PlotSeries s in SeriesList.ToArray()) {
                RemoveSeries(s);
            }
        }

        internal void ReloadColors() {
            if (Invalid) {
                return;
            }

            ip = -1;
            ipb = 0;
            foreach (PlotSeries s in SeriesList) {
                s._Color = NextColor();
                s.SignalPlot.Color = s._Color;
            }
        }

        internal void SetRange(int value) {
            if (Invalid) {
                return;
            }

            foreach (PlotSeries s in SeriesList) {
                s.Range = value;
            }
        }

        internal void Clear() {
            if (Invalid) {
                return;
            }

            foreach (PlotSeries s in SeriesList) {
                s.Clear();
            }
        }

        //private long AvgLastUpdate;

        internal void Update() { // TODO: Remove stale series
            if (Invalid) {
                return;
            }

            //long Now = DateTime.Now.Ticks;

            foreach (PlotSeries s in SeriesList) {
                s.Update();
                //AvgLastUpdate += Now - s.LastUpdate;
            }

            //AvgLastUpdate /= 1 + SeriesList.Count;

            //if (PlotController._StaleSeries) {
            //    DeleteSeriesList.Clear();

            //    foreach (PlotSeries s in SeriesList) {
            //        if (Now - s.LastUpdate > TimeSpan.TicksPerMillisecond * 1000) {
            //            DeleteSeriesList.Add(s);
            //        }
            //    }

            //    foreach (PlotSeries s in DeleteSeriesList) {
            //        RemoveSeries(s);
            //    }
            //}
        }

        internal void RemovePlot(IPlottable plottable) {
            PlotController.RunOnUIThread(() => { WpfPlot.Plot.Remove(plottable); });
        }

        internal SignalPlot NewPlot(double[] DataPointer) {
            SignalPlot signalPlot = null;
            PlotController.RunOnUIThread(() => { signalPlot = WpfPlot.Plot.AddSignal(DataPointer); });
            return signalPlot;
        }

        internal void RemoveSeries(PlotSeries plotSeries) {
            plotSeries.Invalid = true;
            plotSeries.Clear();
            RemovePlot(plotSeries.SignalPlot);
            PlotController.RunOnUIThread(() => { _ = SeriesList.Remove(plotSeries); SeriesListBox?.Items.Remove(plotSeries); });
        }

        internal PlotSeries CreateSeries(string Name, int Range = 0, bool Growing = false) {
            if (Invalid) {
                return null;
            }

            PlotSeries ps = null;
            if (Range <= 1) {
                Range = PlotController._Range;
            }

            Name = $"{this.Name} : {Name}";

            PlotController.RunOnUIThread(() => {
                ps = new(this, Name, NextColor(), Range, Growing);
                SeriesList.Add(ps);
                SeriesListBox?.Items.Add(ps);
            });

            if (PlotController.SlowMode) {
                ps.IsVisible = false;
            }

            return ps;
        }

        private Color NextColor() {
            int max = WpfPlot.Plot.Palette.Count();
            Color[] colors = WpfPlot.Plot.Palette.GetColors(max);

            ip++;

            if (ip == max) {
                ip = 0;
                ipb++;
            }

            Color color = colors[ip];

            for (int i = 0; i < ipb; i++) {
                color = color.Blend(colors[rand.Next(0, max)], 0.5 + (rand.NextDouble() / 2));
            }

            return color;
        }
    }
}
