using ScottPlot;
using System.Diagnostics;
using System.Threading;
using System.Windows.Controls;

namespace ComPlotter.Plot
{
    public class PlotManager
    {
        public PlotControl Control => _Control;

        internal readonly PlotControl _Control;

        private int avgMS;
        private readonly WpfPlot WpfPlot;
        private readonly Thread RenderThread;
        private readonly Stopwatch sw = new();
        private readonly PlotSeriesManager SeriesManager; // TODO: SeriesManager for each pipe

        public PlotManager(WpfPlot WpfPlot)
        {
            this.WpfPlot = WpfPlot;
            ScottPlot.Plot plt = WpfPlot.Plot;
            plt.Palette = Palette.OneHalfDark;
            plt.Title(null);
            plt.Style(Style.Gray1);
            plt.Style(figureBackground: System.Drawing.Color.Transparent, dataBackground: System.Drawing.Color.Transparent);
            plt.XAxis.Grid(false);

            _Control = new(WpfPlot);

            SeriesManager = new PlotSeriesManager(_Control);

            _Control.Setup(SeriesManager);

            RenderThread = new(RenderRunner);
            RenderThread.Name = "Render Thread";
            RenderThread.Start();
        }

        public void Stop()
        {
            RenderThread.Interrupt();
        }

        public void RemoveSeries(PlotSeries plotSeries)
        {
            SeriesManager.RemoveSeries(plotSeries);
        }

        public void UpdateHighlight()
        {
            (double mouseCoordX, _) = WpfPlot.GetMouseCoordinates();
            _Control.SetHighlight(mouseCoordX);
        }

        public PlotSeries CreateSeries(string Name, bool Growing = false, int Range = 0)
        {
            if (Range <= 1)
            {
                Range = Control._Range;
            }
            return SeriesManager.CreateSeries(Name, Range, Growing);
        }

        private static double Avg(double a, double b, double mult = 32)
        {
            return (a * mult + b) / (mult + 1);
        }

        public void SetListBox(ListBox ListBox)
        {
            Control.SeriesListBox = ListBox;
        }

        private void Render()
        {
            SeriesManager.Update(); // FIXME: System.InvalidOperationException: 'Collection was modified; enumeration operation may not execute.'

            if (_Control._AutoRange)
            {
                AxisLimits alb = WpfPlot.Plot.GetAxisLimits();
                WpfPlot.Plot.AxisAuto(0.1, 0.5);
                AxisLimits ala = WpfPlot.Plot.GetAxisLimits();
                WpfPlot.Plot.SetAxisLimitsY(ala.YMin < alb.YMin ? ala.YMin : Avg(alb.YMin, ala.YMin), ala.YMax > alb.YMax ? ala.YMax : Avg(alb.YMax, ala.YMax));
                WpfPlot.Plot.SetAxisLimitsX(Avg(alb.XMin, ala.XMin, 1), Avg(alb.XMax, ala.XMax, 1));
            }

            WpfPlot.Refresh(_Control.LowQualityRender);
        }

        private void RenderRunner()
        {
            while (true)
            {
                sw.Restart();
                _ = WpfPlot.Dispatcher.InvokeAsync(Render).Wait();
                sw.Stop();
                avgMS = (avgMS + (int)sw.ElapsedMilliseconds) / 2;
                _Control.LowQualityRender = avgMS > 10;
                _Control.SlowMode = avgMS > 30;
                Thread.Sleep(avgMS);
            }
        }
    }
}
