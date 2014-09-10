using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// <summary>
    /// Interaction logic for BusyControl.xaml
    /// </summary>
    public partial class BusyControl : UserControl
    {
        bool _cancellable;
        CancellationTokenSource _cts;

        public bool Cancellable
        {
            get
            {
                return _cancellable;
            }

            set
            {
                _cancellable = value;

                if (_cancellable)
                {
                    _cancelButton.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    _cancelButton.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }

        public BusyControl()
        {
            InitializeComponent();
        }

        public async Task<T> StartAsync<T>(Func<T> func, CancellationTokenSource cts)
        {
            _cts = cts;
            return await Task.Factory.StartNew(func);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_cts != null && _cts.IsCancellationRequested == false)
            {
                _cts.Cancel();
                _cancelButton.IsEnabled = false;
                _text.Text = "Cancelled";
            }
        }
    }
}
