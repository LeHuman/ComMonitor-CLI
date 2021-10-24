using ComPlotter.Wpf;
using ScottPlot.Plottable;
using System;
using System.Drawing;

namespace ComPlotter.Plot
{
    public class PlotSeries
    {
        public string Name { get; }
        public const int InitHeap = 512;
        public bool Growing { get; set; }
        public CheckBox CheckBox { get; private set; }
        public bool Invalid { get; internal set; }
        public bool IsVisible { get => _IsVisible; set { CheckBox.IsChecked = value; _IsVisible = value; } }
        public double LastY { get => Data[LastX]; }
        public int LastX { get => nextDataIndex - 1; }
        public int Range { get => _Range; set => _Range = Math.Max(value, 0); }

        internal long Counter;
        internal bool _IsVisible;
        internal Color _Color;

        internal SignalPlot SignalPlot;
        internal double[] Data = new double[InitHeap];

        private int _Range;
        private double OffsetX;
        private int nextDataIndex = 1;
        private int MinRender, MaxRender;
        private readonly PlotGroup Manager;

        internal PlotSeries(PlotGroup Manager, string Name, Color Color, int Range, bool Growing)
        {
            this.Name = Name;
            this.Range = Range;
            this.Growing = Growing;
            this.Manager = Manager;
            _Color = Color;
            CheckBox = new(Name, Color);
            ReloadPlot();
            SignalPlot.Color = Color;
            IsVisible = true;
        }

        public (double x, double y, int index) GetPointNearestX(double X)
        {
            return SignalPlot.GetPointNearestX(X);
        }

        public void Update(double value)
        {
            if (nextDataIndex >= Data.Length)
            {
                IncreaseBuffer();
                OffsetX += MinRender;
            }

            Data[nextDataIndex] = Growing ? Data[nextDataIndex - 1] + value : value;

            if (_Range != 0)
            {
                MinRender = Math.Max(nextDataIndex - _Range, 0);
            }
            MaxRender = LastX;

            Counter++;

            nextDataIndex += 1;
        }

        internal void Clear()
        {
            MinRender = 0;
            MaxRender = 0;
            nextDataIndex = 1;
            SignalPlot.MinRenderIndex = 0;
            SignalPlot.MaxRenderIndex = 0;
            OffsetX = 0;
            SignalPlot.OffsetX = 0;
            Counter = 0;
        }

        internal void ReloadPlot()
        {
            SignalPlot NewPlot = Manager.NewPlot(Data);
            NewPlot.MaxRenderIndex = 0;

            if (SignalPlot != null)
            {
                Manager.RemovePlot(SignalPlot);
                NewPlot.Color = SignalPlot.Color;
                NewPlot.IsVisible = SignalPlot.IsVisible;
                NewPlot.OffsetX = SignalPlot.OffsetX;
            }

            SignalPlot = NewPlot;
        }

        internal void Update()
        {
            SignalPlot.MinRenderIndex = MinRender;
            SignalPlot.MaxRenderIndex = MaxRender;
            SignalPlot.OffsetX = OffsetX;
            SignalPlot.IsVisible = _IsVisible;
            CheckBox.Status = $"{Counter} : {Math.Round(LastY, 3)}";
        }

        private static int NextSize(int CurrentSize)
        {
            CurrentSize += (int)Math.Log(CurrentSize) * CurrentSize / 4;
            return CurrentSize - (CurrentSize % InitHeap);
        }

        private void IncreaseBuffer()
        {
            double[] NewData = MinRender != 0 && Data.Length > _Range * 2 ? Data : new double[NextSize(Data.Length)];
            Span<double> d = Data.AsSpan()[MinRender..];
            d.CopyTo(NewData);
            Data = NewData;
            ReloadPlot();
            nextDataIndex = d.Length;
        }
    }
}
