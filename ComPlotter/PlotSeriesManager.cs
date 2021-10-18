using ScottPlot;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace ComPlotter
{
    public class PlotSeriesManager
    {
        // public bool Sync { get; set; } = true; // TODO: Implement "Syncing" of data

        internal readonly WpfPlot Plot;

        private int ip = -1;

        //private readonly Crosshair ch;
        public List<PlotSeries> Series { get; } = new();

        private Color NextColor()
        {
            ip++;
            ip %= Plot.Plot.Palette.Count();
            return Plot.Plot.Palette.GetColor(ip);
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
            Plot.Plot.Remove(Reloadee.SignalPlot);
            Reloadee.SignalPlot = Plot.Plot.AddSignal(Reloadee.Data);
            Reloadee.SignalPlot.MaxRenderIndex = 0;
            Reloadee.SignalPlot.Color = Reloadee.Color;
            //ch.X = Reloadee.LastX;
            //ch.Y = Reloadee.LastY;
        }

        //internal PlotSeries psMx, psMn;
        //internal long psMxC, psMnC;
        //internal double MaxY = double.MinValue, MinY = double.MaxValue;
        //internal double MinX, MaxX;

        //internal void GiveValue(PlotSeries ps, double val)
        //{
        //    if (val < MinY)
        //    {
        //        psMn = ps;
        //        psMnC = ps.Counter;
        //        MinY = val;
        //    }
        //    else if (val > MaxY)
        //    {
        //        psMx = ps;
        //        psMxC = ps.Counter;
        //        MaxY = val;
        //    }
        //    else if (ps == psMn && ps.Counter - psMnC >= ps.Range)
        //    {
        //        psMnC++;
        //        MinY = ps.Data[ps.sp.MinRenderIndex];
        //    }
        //    else if (ps == psMx && ps.Counter - psMxC >= ps.Range)
        //    {
        //        psMxC++;
        //        MaxY = ps.Data[ps.sp.MinRenderIndex];
        //    }
        //}

        //internal void Update()
        //{
        //    foreach (PlotSeries ps in Series)
        //    {
        //        ps.Update();
        //    }
        //}

        public PlotSeries Create(string Name, int Range = 512, bool Growing = false)
        {
            PlotSeries ps = new(Name, Range, Growing, this, NextColor());
            Reload(ps);
            Series.Add(ps);
            return ps;
        }

        public PlotSeriesManager(WpfPlot Plot)
        {
            this.Plot = Plot;
            //ch = Plot.Plot.AddCrosshair(0, 0);
            //ch.IsVisible = false;
        }
    }
}
