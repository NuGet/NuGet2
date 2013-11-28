using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using NuGet.VisualStudio;

namespace NuGet.Dialog
{
    public partial class SolutionExplorer : VsDialogWindow
    {
        public SolutionExplorer()
        {
            InitializeComponent();
        }

        private void OnOKButtonClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void OnCancelButtonClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OnSolutionTreeViewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Space)
            {
                return;
            }

            var node = SolutionTreeView.SelectedItem as ProjectNodeBase;
            if (node == null)
            {
                return;
            }
            
            if (node.IsSelected == null)
            {
                node.IsSelected = false;
            }
            else
            {
                node.IsSelected = !node.IsSelected;
            }
        }
    }
}