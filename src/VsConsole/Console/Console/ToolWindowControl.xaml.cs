using System;
using System.Windows;
using System.Windows.Controls;
using NuGet.VisualStudio;

namespace NuGetConsole.Implementation.Console {
    /// <summary>
    /// Interaction logic for ToolWindowControl.xaml
    /// </summary>
    public partial class ToolWindowControl : UserControl {

        private readonly IHost _host;
        private readonly IWpfConsole _console;
        private readonly IHostSettings _hostSettings;
        private ProgressBar _progressBar;
        private TextBlock _progressText;

        public ToolWindowControl(FrameworkElement editor, IHost host, IWpfConsole console) {
            InitializeComponent();

            if (host == null) {
                throw new ArgumentNullException("host");
            }

            if (console == null) {
                throw new ArgumentNullException("console");
            }

            if (editor == null) {
                throw new ArgumentNullException("editor");
            }

            _host = host;
            _hostSettings = host.Settings;
            _console = console;

            // add editor to the second row of the grid
            Grid.SetRow(editor, 1);
            Root.Children.Add(editor);
            
            if (host.IsCommandEnabled) {
                SetupBindings();
            }
        }

        private void SetupBindings() {
            PackageSources.ItemsSource = _hostSettings.PackageSources;
            PackageSources.SelectedItem = _hostSettings.ActivePackageSource;

            DefaultProjects.ItemsSource = _hostSettings.AvailableProjects;
            DefaultProjects.SelectedItem = _hostSettings.DefaultProject;

            _hostSettings.PropertyChanged += OnHostSettingsPropertyChanged;
        }

        private void OnClearConsoleButtonClicked(object sender, RoutedEventArgs e) {
            _console.Dispatcher.ClearConsole();
        }

        private void OnStopCommandButtonClicked(object sender, RoutedEventArgs e) {
            _host.Abort();
        }

        private void OpenOptionsPage(object sender, RoutedEventArgs e) {
            var optionsDialogOpener = ServiceLocator.GetInstance<IOptionsDialogOpener>();
            optionsDialogOpener.OpenOptionsDialog(NuGetOptionsPage.PackageSources);
        }

        internal void DisableToolBar() {
            MainToolBar.IsEnabled = false;
        }

        internal void SetExecutionMode(bool executing) {
            // if command is executing, we want to hide all elements of the toolbar,
            // except for the Stop Command button, which we want to show.
            var isVisible = executing ? Visibility.Collapsed : Visibility.Visible;
            var stopButtonVisible = executing ? Visibility.Visible : Visibility.Collapsed;

            foreach (FrameworkElement element in MainToolBar.Items) {
                element.Visibility = (element == StopButton) ? stopButtonVisible : isVisible;
            }

            // hide the progress bar if execution completes.
            if (!executing && _progressBar != null) {
                _progressBar.Visibility = Visibility.Collapsed;
                _progressText.Visibility = Visibility.Collapsed;
            }
        }

        internal void ShowProgress(ProgressData data) {
            if (_progressBar == null) {
                // delay creating the ProgressBar control. Most of the time we don't need it.
                _progressBar = CreateProgressBarControl();
                MainToolBar.Items.Add(_progressBar);

                _progressText = CreateProgressTextControl();
                MainToolBar.Items.Add(_progressText);
            }

            if (!String.IsNullOrEmpty(data.CurrentOperation)) {
                _progressText.Text = data.CurrentOperation;
                _progressText.Visibility = Visibility.Visible;
            }

            _progressBar.Value = data.PercentComplete;
            _progressBar.Visibility = Visibility.Visible;
        }

        private ProgressBar CreateProgressBarControl() {
            var control = new ProgressBar {
                Minimum = 0,
                Maximum = 100,
                Width = 250,
                Margin = new Thickness(5,2,0,2)
            };

            return control;
        }

        private TextBlock CreateProgressTextControl() {
            var textBlock = new TextBlock() {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10,2,0,2)
            };
            return textBlock;
        }

        private void PackageSourceSelected(object sender, SelectionChangedEventArgs e) {
            if (PackageSources.SelectedItem != null) {
                _hostSettings.ActivePackageSource = (string)PackageSources.SelectedItem;
            }
        }

        private void DefaultProjectSelected(object sender, SelectionChangedEventArgs e) {
            if (DefaultProjects.SelectedItem != null) {
                _hostSettings.DefaultProject = (string)DefaultProjects.SelectedItem;
            }
        }

        private void OnHostSettingsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            string name = e.PropertyName;
            if (name == "ActivePackageSource") {
                PackageSources.SelectedItem = _hostSettings.ActivePackageSource;
            }
            else if (name == "DefaultProject") {
                DefaultProjects.SelectedItem = _hostSettings.DefaultProject;
            }
        }
    }
}