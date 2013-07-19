using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace NuGet.Dialog.PackageManagerUI
{
    public class MultiNullToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isAllNullOrEmpty = values.All(v => v == null || v == DependencyProperty.UnsetValue || String.Empty.Equals(v));
            return isAllNullOrEmpty ? Visibility.Collapsed : Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}