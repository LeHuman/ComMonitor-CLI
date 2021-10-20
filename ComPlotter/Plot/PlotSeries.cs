using ScottPlot.Plottable;
using System;
using System.ComponentModel;
using System.Drawing;

namespace ComPlotter
{
    public class PlotSeries : INotifyPropertyChanged
    {
        public const int InitHeap = 512;

        public int LastX { get => nextDataIndex - 1; }
        public double LastY { get => Data[LastX]; }
        public int Range { get => _Range; set => _Range = Math.Max(value, 0); }

        public bool Growing { get; set; }

        internal long Counter;
        internal SignalPlot SignalPlot;
        public string Name { get; }

        public bool IsVisible { get => SignalPlot.IsVisible; set => SignalPlot.IsVisible = value; }

        private string _Status;

        public string Status
        {
            get { return _Status; }
            set
            {
                _Status = value;
                OnPropertyChanged("Status");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        internal readonly Color _Color;
        public System.Windows.Media.SolidColorBrush Brush { get; }

        internal double[] Data = new double[InitHeap];

        private int _Range;
        private int nextDataIndex = 1;
        private readonly PlotSeriesManager Manager;

        public static System.Windows.Media.Color ToMediaColor(Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        internal PlotSeries(string Name, int Range, bool Growing, PlotSeriesManager Manager, Color Color)
        {
            this.Name = Name;
            this.Range = Range;
            this.Growing = Growing;
            this.Manager = Manager;
            _Color = Color;
            Brush = new(ToMediaColor(_Color));
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

        private static int NextSize(int CurrentSize)
        {
            CurrentSize += (int)Math.Log(CurrentSize) * CurrentSize / 4;
            return CurrentSize - (CurrentSize % InitHeap);
        }

        private int MinRender, MaxRender;
        private double OffsetX;
        //private static readonly Semaphore DataArray = new(1, 1);

        private void IncreaseBuffer()
        {
            double[] NewData = MinRender != 0 && Data.Length > _Range * 2 ? Data : new double[NextSize(Data.Length)];
            Span<double> d = Data.AsSpan()[MinRender..];
            d.CopyTo(NewData);
            Data = NewData;
            Manager.Reload(this);
            nextDataIndex = d.Length;
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

            OffsetX++;
            Counter++;

            nextDataIndex += 1;
        }

        internal void Update()
        {
            Status = $"{Counter} : {Math.Round(LastY, 3)}";
            SignalPlot.MinRenderIndex = MinRender;
            SignalPlot.MaxRenderIndex = MaxRender;
            SignalPlot.OffsetX = OffsetX;
        }
    }
}
