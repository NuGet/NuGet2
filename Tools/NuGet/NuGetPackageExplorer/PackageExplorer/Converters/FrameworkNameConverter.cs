using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using NuGet;
using PackageExplorerViewModel;
using System.IO;
using System.Globalization;

namespace PackageExplorer {
    public class FrameworkNameConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            var folder = (PackageFolder)value;

            string[] parts = folder.Path.Split('\\');
            if (parts.Length == 2 && parts[0].Equals("LIB", StringComparison.OrdinalIgnoreCase)) {
                string frameworkProfile = VersionUtility.ParseFrameworkName(folder.Name).ToString();
                return String.Format(CultureInfo.CurrentCulture, "{0}  ({1})", folder.Name, frameworkProfile);
            }
            else {
                return folder.Name;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
