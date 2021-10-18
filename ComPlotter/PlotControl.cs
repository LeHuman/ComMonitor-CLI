using ScottPlot;
using System;
using System.Windows.Input;
using System.Windows.Threading;

namespace ComPlotter
{
    public class PlotControl
    {
        public PlotSeriesManager SeriesManager { get; }

        public ICommand ClearCommand { get; private set; }
        private int _Range = PlotSeries.InitHeap;

        public int Range // TODO: enable range for individual series
        {
            get => _Range; set { _Range = value; SeriesManager.SetRange(value); }
        }

        private bool _AutoRange = true;
        public bool AutoRange { get => _AutoRange; set { _AutoRange = value; if (value) { WpfPlot.Plot.AxisAuto(0.1, 0.5); } } }

        private readonly WpfPlot WpfPlot;
        private readonly DispatcherTimer RenderTimer = new();
        //private readonly DispatcherTimer ZoomTimer = new();

        private void SetupCommands()
        {
            ClearCommand = new CommandBinding(Clear);
        }

        public PlotControl(WpfPlot WpfPlot)
        {
            SeriesManager = new PlotSeriesManager(WpfPlot);

            this.WpfPlot = WpfPlot;
            Plot plt = WpfPlot.Plot;
            plt.Palette = Palette.OneHalfDark;
            plt.Title(null);
            plt.Style(Style.Gray1);
            plt.Style(figureBackground: System.Drawing.Color.Transparent, dataBackground: System.Drawing.Color.Transparent);
            plt.XAxis.Grid(false);
            plt.XAxis.Ticks(false);

            SetupCommands();

            RenderTimer.Interval = TimeSpan.FromMilliseconds(5);
            RenderTimer.Tick += Render;
            RenderTimer.Start();

            //ZoomTimer.Interval = TimeSpan.FromMilliseconds(20);
            //ZoomTimer.Tick += Zoom;
            //ZoomTimer.Start();
        }

        private double avg(double a, double b, double mult = 32)
        {
            return (a * mult + b) / (mult + 1);
        }

        //private void Zoom(object sender = null, EventArgs e = null)
        //{
        //    AxisLimits alb = WpfPlot.Plot.GetAxisLimits();
        //    WpfPlot.Plot.AxisAuto(0.5, 0.5);
        //    AxisLimits ala = WpfPlot.Plot.GetAxisLimits();

        //    WpfPlot.Plot.SetAxisLimits(avg(alb.XMin, ala.XMin), avg(alb.XMax, ala.XMax), avg(alb.YMin, ala.YMin, 32), avg(alb.YMax, ala.YMax, 32));
        //    WpfPlot.Plot.AxisAutoX(0.5);
        //    WpfPlot.Refresh(true);
        //}

        private void Render(object sender = null, EventArgs e = null)
        {
            if (_AutoRange)
            {
                //SeriesManager.Update();
                WpfPlot.Plot.AxisAutoX(0.1);
                AxisLimits alb = WpfPlot.Plot.GetAxisLimits();
                WpfPlot.Plot.AxisAutoY(0.5);
                AxisLimits ala = WpfPlot.Plot.GetAxisLimits();
                WpfPlot.Plot.SetAxisLimitsY(ala.YMin < alb.YMin ? ala.YMin : avg(alb.YMin, ala.YMin), ala.YMax > alb.YMax ? ala.YMax : avg(alb.YMax, ala.YMax));
                //double mny = SeriesManager.MinY, mxy = SeriesManager.MaxY;
                //WpfPlot.Plot.SetAxisLimitsY(mny < alb.YMin ? mny : avg(mny, alb.YMin), mxy > alb.YMax ? mxy : avg(mxy, alb.YMax));
                //WpfPlot.Plot.SetAxisLimitsY(SeriesManager.MinY - 1, SeriesManager.MaxY + 1);
                //WpfPlot.Plot.AxisAuto(0.1, 0.1);
            }
            WpfPlot.Refresh(true);
        }

        public void Clear()
        {
            SeriesManager.Clear();
        }

        public void Stop()
        {
            RenderTimer?.Stop();
        }
    }
}
