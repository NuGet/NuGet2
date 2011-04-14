using System.Windows;
using System.Windows.Controls;
using NuGet.VisualStudio;

namespace NuGetConsole {
    /// <summary>
    /// Interaction logic for ConsoleContainer.xaml
    /// </summary>
    public partial class ConsoleContainer : UserControl {

        public ConsoleContainer(IProductUpdateService productUpdateService) {
            InitializeComponent();

            RootLayout.Children.Add(new ProductUpdateBar(productUpdateService));            
        }

        public void AddConsoleEditor(UIElement content) {
            Grid.SetRow(content, 1);
            RootLayout.Children.Add(content);
        }

        public void NotifyInitializationCompleted() {
            RootLayout.Children.Remove(InitializeText);
        }
    }
}