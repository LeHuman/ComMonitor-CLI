using System.Windows.Controls;

namespace ComPlotter.Util
{
    public class SettingsPanel
    {
        private readonly ListBox ListBox;

        public SettingsPanel(ListBox listBox)
        {
            ListBox = listBox;
            CheckBox c = new CheckBox("TestBox");
            ListBox.Items.Add(c);
        }
    }
}
