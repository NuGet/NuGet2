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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NuGet.Client.VisualStudio.UI
{
    public class DropdownButtonClickEventArgs: EventArgs
    {
        public DropdownButtonClickEventArgs(string buttonText)
        {
            ButtonText = buttonText;
        }

        public string ButtonText
        {
            get;
            private set;
        }
    }

    /// <summary>
    /// Interaction logic for DropdownButton.xaml
    /// </summary>
    public partial class DropdownButton : UserControl
    {
        ContextMenu _contextMenu;

        public event EventHandler<DropdownButtonClickEventArgs> Clicked;

        bool _dontOpenContextMenu;

        public DropdownButton()
        {
            InitializeComponent();

            _contextMenu = new ContextMenu();
            _contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            _contextMenu.PlacementTarget = _mainButton;
        }

        public void SetItems(IEnumerable<string> items)
        {
            _contextMenu.Items.Clear();
            foreach (var item in items)
            {
                var menuItem = new MenuItem() { Header = item };
                menuItem.Click += menuItem_Click;
                _contextMenu.Items.Add(menuItem);
            }

            _mainButton.Content = items.First();
        }

        void menuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
            {
                return;
            }

            _mainButton.Content = menuItem.Header;

            if (Clicked != null)
            {
                Clicked(this, new DropdownButtonClickEventArgs(_mainButton.Content as string));
            }
        }

        private void _mainButton_Click(object sender, RoutedEventArgs e)
        {
            if (Clicked != null)
            {
                Clicked(this, new DropdownButtonClickEventArgs(_mainButton.Content as string));
            }
        }

        private void _dropdownButton_Click(object sender, RoutedEventArgs e)
        {
            if (_dontOpenContextMenu)
            {
                _dontOpenContextMenu = false;
                return;
            }

            _contextMenu.IsOpen = true;
            Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(_contextMenu, PreviewMouseDownHandler);
        }

        private void PreviewMouseDownHandler(Object sender, MouseButtonEventArgs e)
        {
            var result = VisualTreeHelper.HitTest(_grid, e.GetPosition(_grid));
            if (result != null && result.VisualHit == _downArrow &&
                e.LeftButton == MouseButtonState.Pressed)
            {
                _dontOpenContextMenu = true;
            }

            Mouse.RemovePreviewMouseDownOutsideCapturedElementHandler(_contextMenu, PreviewMouseDownHandler);
        }
    }
}
