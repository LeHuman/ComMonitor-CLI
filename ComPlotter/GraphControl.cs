using RealTimeGraphX;
using System.Windows;
using System.Windows.Controls;

namespace ComPlotter
{
    public class GraphControl : Control
    {
        /// <summary>
        /// Gets or sets the graph controller.
        /// </summary>
        public IGraphController Controller
        {
            get => (IGraphController)GetValue(ControllerProperty);
            set => SetValue(ControllerProperty, value);
        }

        public static readonly DependencyProperty ControllerProperty =
            DependencyProperty.Register("Controller", typeof(IGraphController), typeof(GraphControl), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets a value indicating whether to display a tool tip with the current cursor value.
        /// </summary>
        public bool DisplayToolTip
        {
            get => (bool)GetValue(DisplayToolTipProperty);
            set => SetValue(DisplayToolTipProperty, value);
        }

        public static readonly DependencyProperty DisplayToolTipProperty =
            DependencyProperty.Register("DisplayToolTip", typeof(bool), typeof(GraphControl), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets the string format for the X Axis.
        /// </summary>
        public string StringFormatX
        {
            get => (string)GetValue(StringFormatXProperty);
            set => SetValue(StringFormatXProperty, value);
        }

        public static readonly DependencyProperty StringFormatXProperty =
            DependencyProperty.Register("StringFormatX", typeof(string), typeof(GraphControl), new PropertyMetadata("0.0"));

        /// <summary>
        /// Gets or sets the string format for the Y Axis.
        /// </summary>
        public string StringFormatY
        {
            get => (string)GetValue(StringFormatYProperty);
            set => SetValue(StringFormatYProperty, value);
        }

        public static readonly DependencyProperty StringFormatYProperty =
            DependencyProperty.Register("StringFormatY", typeof(string), typeof(GraphControl), new PropertyMetadata("hh\\:mm\\:ss"));

        /// <summary>
        /// Initializes the <see cref="GraphControl"/> class.
        /// </summary>
        static GraphControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GraphControl), new FrameworkPropertyMetadata(typeof(GraphControl)));
        }
    }
}
