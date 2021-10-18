using ScottPlot.Plottable;
using System;
using System.Drawing;

namespace ComPlotter
{
    public class PlotSeries
    {
        public const int InitHeap = 512;

        public int LastX { get => nextDataIndex - 1; }
        public double LastY { get => Data[LastX]; }
        public int Range { get => _Range; set => _Range = Math.Max(value, 0); }

        public bool Growing { get; set; }

        internal long Counter;
        public SignalPlot SignalPlot { get; internal set; }
        public string Name { get; }
        internal readonly Color Color;
        internal double[] Data = new double[InitHeap];

        private string Status;
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

        //private bool increasing = false;

        private void IncreaseBuffer()
        {
            //increasing = true;
            double[] NewData = SignalPlot.MinRenderIndex != 0 && Data.Length > _Range * 2 ? Data : new double[NextSize(Data.Length)];
            Span<double> d = Data.AsSpan().Slice(SignalPlot.MinRenderIndex);
            d.CopyTo(NewData);
            nextDataIndex = d.Length;
            Data = NewData;

            Manager.Reload(this);
            //increasing = false;
        }

        public void Update(double value)
        {
            //bool up = false;
            if (nextDataIndex >= Data.Length)
            {
                IncreaseBuffer();
                //up = true;
            }

            Data[nextDataIndex] = Growing ? Data[nextDataIndex - 1] + value : value;

            //if (increasing)
            //    return;

            Status = $"{Counter} : {LastY}";

            if (_Range != 0)
            {
                SignalPlot.MinRenderIndex = Math.Max(nextDataIndex - _Range, 0);
            }
            SignalPlot.MaxRenderIndex = LastX;

            //Manager.GiveValue(this, Data[nextDataIndex]);

            Counter++;

            nextDataIndex += 1;

            //if (up)
            //{
            //    Update();
            //}
        }

        //internal void Update()
        //{
        //    if (increasing)
        //        return;

        //    Status = $"{Counter} : {LastY}";

        //    if (_Range != 0)
        //    {
        //        sp.MinRenderIndex = Math.Max(nextDataIndex - _Range, 0);
        //    }
        //    sp.MaxRenderIndex = LastX;
        //}
    }
}
