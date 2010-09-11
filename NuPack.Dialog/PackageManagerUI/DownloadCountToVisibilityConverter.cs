using System;
using System.Windows;
using System.Windows.Data;

namespace NuPack.Dialog.PackageManagerUI {
    internal class DownloadCountToVisibilityConverter : IValueConverter {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (value == null)
                return DependencyProperty.UnsetValue;

            int downloadCount = System.Convert.ToInt32(value);
            if (downloadCount < 1) {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }

        #endregion
    }
}
