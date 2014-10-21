using System;
using System.Collections;
using System.Windows;
using System.Windows.Data;

namespace NuGet.Client.VisualStudio.UI
{
    /// <summary>
    /// If the value is an empty or null IEnumerable, returns Visibility.Visible.
    /// Otherwise, returns Visibility.Collapsed.
    /// </summary>
    public class EmptyEnumerableToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType == typeof(Visibility))
            {
                var list = value as IEnumerable;
                if (IsNullOrEmpty(list))
                {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static bool IsNullOrEmpty(IEnumerable list)
        {
            if (list == null)
            {
                return true;
            }

            var enumerator = list.GetEnumerator();
            return enumerator.MoveNext() == false;
        }
    }
}
