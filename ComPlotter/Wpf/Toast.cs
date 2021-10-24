using System.ComponentModel;
using System.Windows.Media;
using Color = System.Drawing.Color;

namespace ComPlotter.Wpf
{
    public class Toast : INotifyPropertyChanged
    {
        public string Text { get; }
        public SolidColorBrush Brush { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public double Opacity { get => _Opacity; internal set { _Opacity = value; OnPropertyChanged("Opacity"); } }

        private double _Opacity;

        internal Toast(string Text, SolidColorBrush Brush)
        {
            this.Text = Text;
            this.Brush = Brush;
        }

        internal Toast(string Text, Color BrushColor)
        {
            this.Text = Text;
            Brush = new(CheckBox.ToMediaColor(BrushColor));
        }

        internal virtual void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
