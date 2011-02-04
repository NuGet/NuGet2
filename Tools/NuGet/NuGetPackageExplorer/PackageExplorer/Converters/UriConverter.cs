using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;

namespace PackageExplorer {
    public class UriConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            Uri uri = (Uri)value;
            return uri == null ? null : uri.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            string stringValue = (string)value;
            Uri uri;
            if (!Uri.TryCreate(stringValue, UriKind.Absolute, out uri)) {
                return uri;
            }
            else {
                return DependencyProperty.UnsetValue;
            }
        }
    }
}
