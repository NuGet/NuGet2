using System.Windows;
using Microsoft.VisualStudio.PlatformUI;

namespace NuGet.Dialog {
    public partial class SolutionExplorer : DialogWindow {
        public SolutionExplorer() {
            InitializeComponent();
        }

        private void OnOKButtonClicked(object sender, RoutedEventArgs e) {
            DialogResult = true;
        }

        private void OnCancelButtonClicked(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }
    }
}