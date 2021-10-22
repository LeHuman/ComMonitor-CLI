using ScottPlot;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Controls;

namespace ComPlotter.Plot
{
    public class PlotManager
    {
        public PlotControl Control => _Control;
        public List<PlotGroup> Groups { get; } = new();

        internal readonly PlotControl _Control;

        private int avgMS;
        private bool RunRender;
        private readonly WpfPlot WpfPlot;
        private readonly ListBox ListBox;
        private readonly Thread RenderThread;
        private readonly Stopwatch sw = new();

        public PlotManager(WpfPlot WpfPlot, ListBox ListBox = null)
        {
            this.WpfPlot = WpfPlot;
            this.ListBox = ListBox;
            ScottPlot.Plot plt = WpfPlot.Plot;
            plt.Palette = Palette.OneHalfDark;
            plt.Title(null);
            plt.Style(Style.Gray1);
            plt.Style(figureBackground: System.Drawing.Color.Transparent, dataBackground: System.Drawing.Color.Transparent);
            plt.XAxis.Grid(false);

            _Control = new(WpfPlot, Groups, ListBox);

            RenderThread = new(RenderRunner);
            RenderThread.Name = "Render Thread";
            Start();
        }

        public void Start()
        {
            RunRender = true;
            RenderThread.Start();
        }

        public void Stop()
        {
            RunRender = false;
        }

        public void RemoveGroup(PlotGroup Group)
        {
            Group.Delete();
            _Control.RunOnUIThread(() => { _ = Groups.Remove(Group); });
        }

        public PlotGroup NewGroup(string Name)
        {
            PlotGroup pm = new(Name, _Control, ListBox); // TODO: Give each group their own list
            _Control.RunOnUIThread(() => { Groups.Add(pm); });
            return pm;
        }

        public void UpdateHighlight()
        {
            (double mouseCoordX, _) = WpfPlot.GetMouseCoordinates();
            _Control.SetHighlight(mouseCoordX);
        }

        private void RenderRunner()
        {
            while (RunRender)
            {
                sw.Restart();
                _Control.RunOnUIThread(Render);
                sw.Stop();
                avgMS = (avgMS + (int)sw.ElapsedMilliseconds) / 2;
                _Control.LowQualityRender = avgMS > 10;
                _Control.SlowMode = avgMS > 30;
                Thread.Sleep(avgMS);
            }
        }

        private static double Avg(double a, double b, double mult = 32)
        {
            return (a * mult + b) / (mult + 1);
        }

        private void Render()
        {
            foreach (PlotGroup SeriesManager in Groups)
            {
                SeriesManager.Update(); // FIXME: System.InvalidOperationException: 'Collection was modified; enumeration operation may not execute.'
            }

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
    }
}
