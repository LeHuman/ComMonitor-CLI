using System.Windows.Controls;
using WCheckBox = ComPlotter.Wpf.CheckBox;

namespace ComPlotter.Util
{
    public class SettingsPanel
    {
        private readonly ListBox ListBox;

        public SettingsPanel(ListBox listBox)
        {
            ListBox = listBox;
        }

        public WCheckBox AddCheckBox(string Name)
        {
            WCheckBox CheckBox = new(Name);
            ListBox.Items.Add(CheckBox);
            return CheckBox;
        }
    }
}
