using System;
using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Media;

namespace NuGet.WebMatrix
{
    internal class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Debug.Assert(value != null, "Value must not be null");

            Brush brush = null;
            if (value is Color)
            {
                brush = new SolidColorBrush((Color)value);
            }
            else if (value is string)
            {
                try
                {
                    BrushConverter converter = new BrushConverter();
                    brush = converter.ConvertFromString((string)value) as SolidColorBrush;
                }
                catch
                {
                    // We want to eat the exception
                }
            }

            if (brush == null)
            {
                Debug.Fail(string.Format("Unable to convert {0} to a Brush. Using a Colors.Black as a default.", value ?? "NULL"));
                brush = new SolidColorBrush(Colors.Black);
            }

            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
