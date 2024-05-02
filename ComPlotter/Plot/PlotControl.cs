using Microsoft.Win32;
using ScottPlot;
using ScottPlot.Plottable;
using ScottPlot.Styles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CommandBinding = ComPlotter.Wpf.CommandBinding;

namespace ComPlotter.Plot {

    public class PlotControl : INotifyPropertyChanged {
        public PlotSeries SelectedPlot { get; set; }
        public ICommand ClearCommand { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public string HighlightedPointStatus { get; private set; } = "No Point Highlighted";
        public bool StaleSeries { get => _StaleSeries; set => _StaleSeries = value; }
        public bool VisibleDefault { get => _VisibleDefault; set => _VisibleDefault = value; }
        public int Range { get => _Range; set { _Range = value; SetRangeAll(value); } } // TODO: enable range for individual series
        public bool AutoRange { get => _AutoRange; set { _AutoRange = value; EnableInput(!value); if (value) { RunOnUIThread(() => { WpfPlot.Plot.AxisAuto(0.1, 0.5); }); } } }

        internal WpfPlot WpfPlot;
        internal ListBox SeriesListBox;
        internal bool _AutoRange = true;
        internal bool _VisibleDefault = true;
        internal bool LowQualityRender = true;
        internal MarkerPlot HighlightedPoint;
        internal int _Range = PlotSeries.InitHeap;
        internal bool SlowMode, _DisableSlowMode, _StaleSeries;
        private readonly List<PlotGroup> PlotGroups;

        internal PlotControl(WpfPlot WpfPlot, List<PlotGroup> PlotGroups, ListBox listBox) {
            this.WpfPlot = WpfPlot;
            this.PlotGroups = PlotGroups;
            SeriesListBox = listBox; // TODO: Programmaticlly setup the listbox

            WpfPlot.Configuration.MiddleClickAutoAxis = false;
            WpfPlot.Configuration.DoubleClickBenchmark = false;
            WpfPlot.Configuration.WarnIfRenderNotCalledManually = false;
            WpfPlot.Configuration.AxesChangedEventEnabled = false;
            WpfPlot.Configuration.QualityConfiguration.AutoAxis = RenderType.ProcessMouseEventsOnly;
            WpfPlot.Configuration.QualityConfiguration.BenchmarkToggle = RenderType.ProcessMouseEventsOnly;
            WpfPlot.Configuration.QualityConfiguration.MouseInteractiveDropped = RenderType.ProcessMouseEventsOnly;
            WpfPlot.Configuration.QualityConfiguration.MouseWheelScrolled = RenderType.ProcessMouseEventsOnly;
            WpfPlot.Configuration.QualityConfiguration.MouseInteractiveDragged = RenderType.ProcessMouseEventsOnly;
            WpfPlot.RightClicked -= WpfPlot.DefaultRightClickEvent;
            WpfPlot.RightClicked += RightClick_Menu_Options;

            EnableInput(false);

            SetupCommands();

            HighlightedPoint = WpfPlot.Plot.AddPoint(0, 0);
            HighlightedPoint.Color = Color.White;
            HighlightedPoint.MarkerSize = 10;
            HighlightedPoint.MarkerShape = MarkerShape.openCircle;
            HighlightedPoint.IsVisible = false;
        }

        public void EnableBenchmark(bool Enabled) {
            WpfPlot.Plot.Benchmark(Enabled);
        }

        public void DisableSlowMode(bool Disable) {
            _DisableSlowMode = Disable;
            if (Disable) {
                LowQualityRender = false;
                SlowMode = false;
            }
        }

        public void ClearAll() {
            foreach (PlotGroup SeriesManager in PlotGroups) {
                SeriesManager.Clear();
            }
        }

        public void SetRangeAll(int value) {
            foreach (PlotGroup SeriesManager in PlotGroups) {
                SeriesManager.SetRange(value);
            }
        }

        public void SetHighlight(double mouseCoordX) {
            if (SelectedPlot == null || !SelectedPlot.IsVisible) {
                HighlightedPoint.IsVisible = false;
                return;
            }
            (double pointX, double pointY, int _) = SelectedPlot.GetPointNearestX(mouseCoordX);
            HighlightedPoint.X = pointX;
            HighlightedPoint.Y = pointY;
            HighlightedPoint.IsVisible = true;
            HighlightedPointStatus = $"Highlight: {Math.Round(pointY, 8)}";
            OnPropertyChanged("HighlightedPointStatus");
        }

        internal virtual void OnPropertyChanged(string property) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        internal void RunOnUIThread(Action action) {
            _ = WpfPlot.Dispatcher.InvokeAsync(action).Wait();  // TODO: actually use async
        }

        private void SetupCommands() {
            ClearCommand = new CommandBinding(ClearAll);
        }

        private void EnableInput(bool enable) {
            WpfPlot.Configuration.UseRenderQueue = !enable;
            WpfPlot.Configuration.LeftClickDragPan = enable;
            WpfPlot.Configuration.MiddleClickDragZoom = enable;
            WpfPlot.Configuration.RightClickDragZoom = enable;
            WpfPlot.Configuration.ScrollWheelZoom = enable;
        }

        private void RightClick_Menu_Options(object sender, EventArgs e) {
            MenuItem addSaveImageMenuItem = new() { Header = "Save Image" };
            addSaveImageMenuItem.Click += (_, _) => Task.Run(SaveGraph);
            //MenuItem addCopyImageMenuItem = new() { Header = "Copy Image" };
            //addCopyImageMenuItem.Click += (_, _) => Task.Run(CopyGraph);

            ContextMenu rightClickMenu = new();
            rightClickMenu.Items.Add(addSaveImageMenuItem);
            //rightClickMenu.Items.Add(addCopyImageMenuItem);
            rightClickMenu.IsOpen = true;
        }

        //private void CopyGraph() {
        //    Bitmap Graph_Image = null;

        //    RunOnUIThread(() => { Graph_Image = WpfPlot.Plot.Render(); });

        //    MemoryStream Image_Memory = new();
        //    Graph_Image.Save(Image_Memory, System.Drawing.Imaging.ImageFormat.Png);

        //    BitmapImage Graph_Bitmap = new();
        //    Graph_Bitmap.BeginInit();
        //    Graph_Bitmap.StreamSource = new MemoryStream(Image_Memory.ToArray());
        //    Graph_Bitmap.EndInit();

        //    Clipboard.SetImage(Graph_Bitmap);

        //    Graph_Image.Dispose();
        //    Image_Memory.Dispose();
        //    Graph_Bitmap.Freeze();
        //}

        private void SaveGraph() {
            try {
                var Save_Image_Window = new SaveFileDialog
                {
                    FileName = "Graph Plot_" + DateTime.Now.ToString("yyyy-MM-dd h-mm-ss tt") + ".png",
                    Filter = "PNG Files (*.png)|*.png;*.png" +
                      "|JPG Files (*.jpg, *.jpeg)|*.jpg;*.jpeg" +
                      "|BMP Files (*.bmp)|*.bmp;*.bmp" +
                      "|All files (*.*)|*.*"
                };

                if (Save_Image_Window.ShowDialog() is true) {
                    RunOnUIThread(() => { WpfPlot.Plot.SaveFig(Save_Image_Window.FileName); });
                }
            } catch (Exception) {
            }
        }
    }
}
