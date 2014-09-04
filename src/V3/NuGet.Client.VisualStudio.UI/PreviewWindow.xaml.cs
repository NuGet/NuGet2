using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NuGet.Client.VisualStudio.UI
{
    /// <summary>
    /// Interaction logic for InstallPreviewWindow.xaml
    /// </summary>
    public partial class PreviewWindow : Window
    {
        IEnumerable<PackageName> _unchanged;
        IEnumerable<PackageName> _deleted;
        IEnumerable<PackageName> _added;

        public PreviewWindow(
            IEnumerable<PackageName> unchanged,
            IEnumerable<PackageName> deleted,
            IEnumerable<PackageName> added)
        {
            InitializeComponent();

            _unchanged = unchanged;
            _deleted = deleted;
            _added = added;

            _list.Children.Add(new TextBlock()
                {
                    Text = "Unchanged:",
                    FontWeight = FontWeights.Bold
                });
            foreach (var p in _unchanged)
            {
                var tb = new TextBlock();
                tb.Text = p.ToString();
                tb.Margin = new Thickness(10, 0, 0, 0);
                tb.Foreground = new SolidColorBrush(Colors.Gray);
                _list.Children.Add(tb);
            }

            _list.Children.Add(new TextBlock()
            {
                Text = "Added:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 5, 0, 0)
            });
            foreach (var p in _added)
            {
                var tb = new TextBlock();
                tb.Text = p.ToString();
                tb.Margin = new Thickness(10, 0, 0, 0);
                _list.Children.Add(tb);
            }

            _list.Children.Add(new TextBlock()
            {
                Text = "Deleted:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 5, 0, 0)
            });
            foreach (var p in _deleted)
            {
                var tb = new TextBlock();
                tb.Text = p.ToString();
                tb.Margin = new Thickness(10, 0, 0, 0);
                tb.TextDecorations = TextDecorations.Strikethrough;
                _list.Children.Add(tb);
            }
        }
    }
}
