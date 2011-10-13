using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace NuGet.Dialog.PackageManagerUI
{
    public class NormalizeTextConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string stringValue = (string)value;
            if (String.IsNullOrEmpty(stringValue))
            {
                return stringValue;
            }

            // replace a series of whitespaces with a single whitespace
            // REVIEW: Should we avoid regex and just do this manually?
            string normalizedText = Regex.Replace(stringValue, @"[\f\t\v\x85\p{Z}]+", " ");

            return WebUtility.HtmlDecode(normalizedText);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}