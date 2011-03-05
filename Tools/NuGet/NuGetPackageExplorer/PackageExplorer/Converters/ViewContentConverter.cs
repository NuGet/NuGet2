using System;
using System.Windows.Data;
using PackageExplorerViewModel;

namespace PackageExplorer {
    public class ViewContentConverter : IValueConverter {

        public ViewContentConverter() {
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return value is PackageFile;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
