using System;
using System.Collections.Generic;
using System.Windows.Data;

namespace NuGet.Dialog.PackageManagerUI
{
    public class StringCollectionsToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType == typeof(string))
            {
                string stringValue = value as string;
                if (stringValue != null)
                {
                    return stringValue;
                }
                else if (value == null)
                {
                    return String.Empty;
                }
                else
                {
                    IEnumerable<string> parts = (IEnumerable<string>)value;
                    return String.Join(", ", parts);
                }
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
