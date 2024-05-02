using ScottPlot;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ComPlotter.Plot {

    public class PlotManager {
        public PlotControl Control => _Control;
        public List<PlotGroup> Groups { get; } = new();

        internal readonly PlotControl _Control;

        private int avgMS = 5;
        private bool RunRender;
        private readonly WpfPlot WpfPlot;
        private readonly ListBox ListBox;
        private readonly Thread RenderThread;
        private readonly Stopwatch sw = new();
        private readonly Dictionary<string, PlotGroup> GroupMap = new();
        private readonly ScottPlot.Plottable.Annotation SlowModeAnnotation;

        public PlotManager(WpfPlot WpfPlot, ListBox ListBox = null) {
            this.WpfPlot = WpfPlot;
            this.ListBox = ListBox;
            ScottPlot.Plot plt = WpfPlot.Plot;
            plt.Palette = Palette.OneHalfDark;
            plt.Title(null);
            plt.Style(Style.Gray1);
            plt.Style(figureBackground: Color.Transparent, dataBackground: Color.Transparent);
            plt.XAxis.Grid(false);

            SlowModeAnnotation = WpfPlot.Plot.AddAnnotation("Slow", Alignment.LowerLeft);
            SlowModeAnnotation.IsVisible = false;
            SlowModeAnnotation.Font.Size = 14;
            SlowModeAnnotation.Font.Bold = true;
            SlowModeAnnotation.Font.Color = Color.White;
            SlowModeAnnotation.Shadow = false;
            SlowModeAnnotation.Border = false;
            SlowModeAnnotation.BackgroundColor = Color.Red;
            SlowModeAnnotation.Background = false;

            _Control = new(WpfPlot, Groups, ListBox);

            RenderThread = new(RenderRunner)
            {
                Name = "Render Thread"
            };
        }

        public void Start() {
            RunRender = true;
            RenderThread.Start();
        }

        public void Stop() {
            RunRender = false;
        }

        public void RemoveGroup(PlotGroup Group) {
            Group.Delete();
            GroupMap.Remove(Group.Name);
            _Control.RunOnUIThread(() => { _ = Groups.Remove(Group); });
        }

        public void RemoveGroup(string GroupName) {
            GroupMap.TryGetValue(GroupName, out PlotGroup pg);
            if (pg != null)
                RemoveGroup(pg);
        }

        public void FocusSeries(PlotSeries PlotSeries) {// TODO: Select Per List
            foreach (PlotGroup SeriesGroup in Groups) {
                SeriesGroup.Focus(PlotSeries);
            }
        }

        public PlotGroup NewGroup(string Name) {// TODO: Give each group their own list
            GroupMap.TryGetValue(Name, out PlotGroup pm);

            if (pm == null) {
                pm = new(Name, _Control, ListBox);
                GroupMap.Add(Name, pm);
            }

            _Control.RunOnUIThread(() => { Groups.Add(pm); });
            return pm;
        }

        public void UpdateHighlight() {
            (double mouseCoordX, _) = WpfPlot.GetMouseCoordinates();
            _Control.SetHighlight(mouseCoordX);
        }

        private void RenderRunner() {
            Thread.Sleep(500); // Wait for half a second just to settle
            try {
                while (RunRender) {
                    if (!_Control._DisableSlowMode) {
                        sw.Restart();
                        _Control.RunOnUIThread(Render);
                        sw.Stop();
                        avgMS = (avgMS + (int)sw.ElapsedMilliseconds) / 2;
                        _Control.LowQualityRender = avgMS > 10;
                        _Control.SlowMode = avgMS > 30;
                        SlowModeAnnotation.IsVisible = false;
                        SlowModeAnnotation.Background = false;
                    } else {
                        _Control.RunOnUIThread(Render);
                        avgMS = 5;
                    }
                    SlowModeAnnotation.IsVisible = _Control.LowQualityRender;
                    SlowModeAnnotation.Background = _Control.SlowMode;
                    Thread.Sleep(avgMS);
                }
            } catch (TaskCanceledException _) {

            }
        }

        private static double Avg(double a, double b, double mult = 32) {
            return (a * mult + b) / (mult + 1);
        }

        private void Render() {
            foreach (PlotGroup SeriesManager in Groups) {
                SeriesManager.Update(); // FIXME: System.InvalidOperationException: 'Collection was modified; enumeration operation may not execute.'
            }

            if (_Control._AutoRange) {
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
