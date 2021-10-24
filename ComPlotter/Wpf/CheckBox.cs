using System.ComponentModel;
using System.Windows.Media;
using DColor = System.Drawing.Color;
using MColor = System.Windows.Media.Color;

namespace ComPlotter.Wpf
{
    public class CheckBox : INotifyPropertyChanged
    {
        public delegate void CheckBoxCallback(bool IsChecked);

        public string Name { get; }
        public bool IsChecked { get => _IsChecked; set { _IsChecked = value; Callback?.Invoke(value); } }
        public SolidColorBrush Brush { get => _Brush; private set { _Brush = value; OnPropertyChanged("Brush"); } }
        public string Status { get => _Status; internal set { _Status = value; OnPropertyChanged("Status"); } }

        public event PropertyChangedEventHandler PropertyChanged;

        private bool _IsChecked;
        private string _Status;
        private SolidColorBrush _Brush;
        private CheckBoxCallback Callback;

        public CheckBox(string Name, SolidColorBrush Brush = null)
        {
            this.Name = Name;
            this.Brush = Brush;
        }

        public CheckBox(string Name, DColor BrushColor)
        {
            this.Name = Name;
            Brush = new(ToMediaColor(BrushColor));
        }

        internal void SetCallback(CheckBoxCallback Callback)
        {
            this.Callback = Callback;
        }

        internal void ChangeBrush(SolidColorBrush Brush)
        {
            this.Brush = Brush;
        }

        internal void ChangeBrush(DColor BrushColor)
        {
            Brush = new(ToMediaColor(BrushColor));
        }

        internal virtual void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private static MColor ToMediaColor(DColor color)
        {
            return MColor.FromArgb(color.A, color.R, color.G, color.B);
        }
    }
}
