using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NuGet.WebMatrix
{
    /// <summary>
    /// Interaction logic for SearchPageHeader.xaml
    /// </summary>
    internal partial class SearchPageHeader : UserControl, INotifyPropertyChanged
    {
        private string _searchString;

        public SearchPageHeader()
        {
            InitializeComponent();
            UpdateButtonImage();
        }

        private static ImageSource GetSearchImageSource()
        {
            try
            {
                return Extensions.ConvertToImageSource(NuGet.WebMatrix.Resources.MagGlass);
            }
            catch (Exception e)
            {
                Debug.Fail(@"MagGlass Image could not be loaded: ", e.ToString());
                return null;
            }
        }

        private static ImageSource GetDeleteImageSource()
        {
            try
            {
                return Extensions.ConvertToImageSource(NuGet.WebMatrix.Resources.ClearSearch);
            }
            catch (Exception e)
            {
                Debug.Fail(@"ClearSearch Image could not be loaded: ", e.ToString());
                return null;
            }
        }

        public bool IsSearchTextBoxVisible
        {
            get
            {
                return (_searchStackPanel.Visibility == System.Windows.Visibility.Visible);
            }

            set
            {
                if (value)
                {
                    _searchStackPanel.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    _searchStackPanel.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }

        public void SetFocusOnSearchBox()
        {
            _searchTextBox.Focus();
        }

        public string SearchString
        {
            get
            {
                return _searchString;
            }

            set
            {
                _searchString = value;
                OnPropertyChanged("SearchString");
                UpdateButtonImage();
            }
        }

        private void UpdateButtonImage()
        {
            if (string.IsNullOrEmpty(_searchString))
            {
                _buttonImage.Source = GetSearchImageSource();
            }
            else
            {
                _buttonImage.Source = GetDeleteImageSource();
            }
        }

        private void OnPropertyChanged(string prop)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(prop));
            }
        }

        public void SetHeaderTextContent(FrameworkElement headerControl)
        {
            _headerTextBlockContentControl.Content = headerControl;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            ClearSearchString();
        }

        private void ClearSearchString()
        {
            SearchString = string.Empty;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // not sure if a textbox can be keyboard focused and not focused (seems like it shouldn't be possible)
            // but let's be safe anyway
            if (_searchTextBox.IsFocused ||
                _searchTextBox.IsKeyboardFocused)
            {
                if (e.Key == Key.Enter)
                {
                    // no op since this should "execute" the search but we search as we type
                    e.Handled = true;
                    return;
                }
                else if (e.Key == Key.Escape)
                {
                    if (!string.IsNullOrEmpty(SearchString))
                    {
                        // clear search text
                        ClearSearchString();
                        e.Handled = true;
                        return;
                    }
                }
            }

            base.OnKeyDown(e);
        }
    }
}
