using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ComPlotter.Wpf {

    public class Toaster {
        private readonly SolidColorBrush DefaultBrush, ErrorBrush, WarningBrush, DebugBrush;

        public bool Enable { get; set; } = true;

        public ObservableCollection<Toast> Messages { get; } = new();

        public Toaster(SolidColorBrush DefaultBrush, SolidColorBrush ErrorBrush, SolidColorBrush WarningBrush, SolidColorBrush DebugBrush) {
            this.DefaultBrush = DefaultBrush;
            this.ErrorBrush = ErrorBrush;
            this.WarningBrush = WarningBrush;
            this.DebugBrush = DebugBrush;
        }

        public void Toast(string message, int DurationMilliseconds = 2000) {
            NewToast(new(message, DefaultBrush), DurationMilliseconds);
        }

        public void ErrorToast(string message, int DurationMilliseconds = 2000) {
            NewToast(new(message, ErrorBrush), DurationMilliseconds);
        }

        public void WarnToast(string message, int DurationMilliseconds = 2000) {
            NewToast(new(message, WarningBrush), DurationMilliseconds);
        }

        public void DebugToast(string message, int DurationMilliseconds = 2000) {
            NewToast(new(message, DebugBrush), DurationMilliseconds);
        }

        private void NewToast(Toast Toast, int DurationMilliseconds) {
            if (!Enable) {
                return;
            }
            Application.Current.Dispatcher.Invoke(() => { Messages.Add(Toast); });
            DelayedRemoval(Toast, DurationMilliseconds);
        }

        private async void DelayedRemoval(Toast Toast, int DurationMilliseconds) // TODO: Use animation timeline instead?
        {
            while (Toast.Opacity < 1) {
                Toast.Opacity += 0.1;
                await Task.Delay(DurationMilliseconds / 500);
            }
            Toast.Opacity = 1;

            await Task.Delay(DurationMilliseconds);

            while (Toast.Opacity > 0) {
                Toast.Opacity -= 0.1;
                await Task.Delay(DurationMilliseconds / 500);
            }
            Toast.Opacity = 0;

            Application.Current.Dispatcher.Invoke(() => { _ = Messages.Remove(Toast); });
        }
    }
}
