using ScottPlot.Plottable;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Data;

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
        public SignalPlot SignalPlot { get; internal set; }
        public string Name { get; }

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

        internal readonly Color Color;
        internal double[] Data = new double[InitHeap];

        private int _Range;
        private int nextDataIndex = 1;
        private readonly PlotSeriesManager Manager;

        internal PlotSeries(string Name, int Range, bool Growing, PlotSeriesManager Manager, Color Color)
        {
            this.Name = Name;
            this.Range = Range;
            this.Growing = Growing;
            this.Manager = Manager;
            this.Color = Color;
        }

        internal void Clear()
        {
            nextDataIndex = 1;
            SignalPlot.MinRenderIndex = 0;
            SignalPlot.MaxRenderIndex = 0;
        }

        private static int NextSize(int CurrentSize)
        {
            CurrentSize += (int)Math.Log(CurrentSize) * CurrentSize / 4;
            return CurrentSize - (CurrentSize % InitHeap);
        }

        private void IncreaseBuffer()
        {
            double[] NewData = SignalPlot.MinRenderIndex != 0 && Data.Length > _Range * 2 ? Data : new double[NextSize(Data.Length)];
            Span<double> d = Data.AsSpan().Slice(SignalPlot.MinRenderIndex);
            d.CopyTo(NewData);
            nextDataIndex = d.Length;
            Data = NewData;

            Manager.Reload(this);
        }

        public void Update(double value)
        {
            if (nextDataIndex >= Data.Length)
            {
                IncreaseBuffer();
            }

            Data[nextDataIndex] = Growing ? Data[nextDataIndex - 1] + value : value;

            Status = $"{Counter} : {Math.Round(LastY, 3)}";

            if (_Range != 0)
            {
                SignalPlot.MinRenderIndex = Math.Max(nextDataIndex - _Range, 0);
            }
            SignalPlot.MaxRenderIndex = LastX;

            Counter++;

            nextDataIndex += 1;
        }
    }
}
