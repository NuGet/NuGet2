using System;
using System.Globalization;
using System.Windows.Data;

namespace NuGet.Dialog.PackageManagerUI
{
    /// <summary>
    /// Converts a DateTimeOffset to the local time zone.
    /// </summary>
    public class DateTimeOffsetToLocalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTimeOffset? offset = value as DateTimeOffset?;

            if (offset.HasValue)
            {
                return offset.Value.ToLocalTime();
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}