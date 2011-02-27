using System;
using System.Windows.Data;

namespace PackageExplorer {
    public class NullToBoolConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (value is string) {
                return !String.IsNullOrEmpty((string)value);
            }

            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
