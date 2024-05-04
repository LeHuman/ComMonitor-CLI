using ComPlotter.Plot;
using ComPlotter.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ComPlotter {

    internal class Test {
        private readonly Toaster Toaster;
        private readonly Random rand = new(0);
        private readonly PlotManager PlotManager;
        private readonly List<PlotGroup> DeletePlotList = new();
        private readonly List<PlotSeries> DeleteSeriesList = new();

        public Test(PlotManager PlotManager, Toaster Toaster) {
            this.PlotManager = PlotManager;
            this.Toaster = Toaster;

            _ = Task.Run(UpdateTestData);
        }

        private static void NOP(double durationSeconds) {
            var durationTicks = Math.Round(durationSeconds * Stopwatch.Frequency);
            var sw = Stopwatch.StartNew();

            while (sw.ElapsedTicks < durationTicks) {
            }
        }

        private async Task UpdateTestData() {
            Stopwatch Stopwatch = Stopwatch.StartNew();
            while (true) {
                foreach (PlotGroup pm in PlotManager.Groups) {
                    foreach (PlotSeries ps in pm.SeriesList) {
                        if (rand.Next(100000) > 99998) {
                            DeleteSeriesList.Add(ps);
                        }
                    }

                    foreach (PlotSeries ps in DeleteSeriesList) {
                        pm.RemoveSeries(ps);
                    }

                    DeleteSeriesList.Clear();

                    if (rand.Next(100) > 85) { pm.Update(rand.Next(5).ToString(), rand.Next(1001) * Math.Sin(Stopwatch.ElapsedMilliseconds)); }

                    if (rand.Next(1000001) == 1000000) {
                        DeletePlotList.Add(pm);
                    }
                }

                foreach (PlotGroup pm in DeletePlotList) {
                    Toaster.WarnToast($"Removed Port : {pm.Name}");
                    PlotManager.RemoveGroup(pm);
                }
                DeletePlotList.Clear();

                if (rand.Next(1000001) >= 999999) {
                    PlotGroup g = PlotManager.NewGroup(rand.Next(999).ToString());
                    Toaster.Toast($"New Port : {g.Name}");
                }
                if (PlotManager.Groups.Count != 0)
                    NOP(0.0001);
                await Task.Delay(0);
            }
        }
    }
}
