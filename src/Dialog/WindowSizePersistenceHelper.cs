using System;
using System.Windows;
using NuGet.VisualStudio;

namespace NuGet.Dialog {
    public static class WindowSizePersistenceHelper {
        private static Lazy<IWindowSettingsManager> WindowSettingsManager = new Lazy<IWindowSettingsManager>(ServiceLocator.GetInstance<IWindowSettingsManager>);

        public static string GetWindowToken(DependencyObject obj) {
            return (string)obj.GetValue(WindowTokenProperty);
        }

        public static void SetWindowToken(DependencyObject obj, string value) {
            obj.SetValue(WindowTokenProperty, value);
        }

        public static readonly DependencyProperty WindowTokenProperty =
            DependencyProperty.RegisterAttached(
                "WindowToken", 
                typeof(string), 
                typeof(WindowSizePersistenceHelper), 
                new UIPropertyMetadata(new PropertyChangedCallback(OnWindowTokenPropertyChange)));

        private static void OnWindowTokenPropertyChange(object sender, DependencyPropertyChangedEventArgs args) {
            Window window = sender as Window;
            if (window != null) {
                string windowToken = (string)args.NewValue;
                if (!String.IsNullOrEmpty(windowToken)) {
                    Size size = WindowSettingsManager.Value.GetWindowSize(windowToken);
                    if (!size.IsEmpty) {
                        SetWindowSize(window, size);
                    }
                }
                window.Closed += OnWindowClosed;
            }
        }

        private static void OnWindowClosed(object sender, EventArgs e) {
            Window window = (Window)sender;
            window.Closed -= OnWindowClosed;

            string windowToken = GetWindowToken(window);
            // save window size when it closes
            WindowSettingsManager.Value.SetWindowSize(windowToken, window.RenderSize);
        }

        private static void SetWindowSize(Window window, Size size) {
            window.Width = size.Width;
            window.Height = size.Height;
        }
    }
}