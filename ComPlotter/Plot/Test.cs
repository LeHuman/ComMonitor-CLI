using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;

namespace ComPlotter.Plot
{
    internal class Test
    {
        private readonly Timer DataTimer;
        private readonly Random rand = new(0);
        private readonly PlotManager PlotManager;
        private readonly List<PlotGroup> DeletePlotList = new();
        private readonly List<PlotSeries> DeleteSeriesList = new();
        private readonly Stopwatch Stopwatch = Stopwatch.StartNew();

        public Test(PlotManager PlotManager)
        {
            this.PlotManager = PlotManager;

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

            foreach (PlotGroup pm in PlotManager.Groups)
            {
                foreach (PlotSeries ps in pm.SeriesList)
                {
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
                    else if (rand.Next(100) > 98)
                    { DeleteSeriesList.Add(ps); }
                }

                if (rand.Next(100) > 98)
                { pm.CreateSeries(rand.Next(999).ToString()); }

                foreach (PlotSeries ps in DeleteSeriesList)
                {
                    pm.RemoveSeries(ps);
                }

                DeleteSeriesList.Clear();
                if (rand.Next(100) >= 99)
                {
                    DeletePlotList.Add(pm);
                }
            }

            foreach (PlotGroup pm in DeletePlotList)
            {
                PlotManager.RemoveGroup(pm);
            }
            DeletePlotList.Clear();

            if (rand.Next(100) > 95)
            { PlotManager.NewGroup(rand.Next(999).ToString()); }

            DataTimer.Start();
        }
    }
}
