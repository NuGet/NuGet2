using System;
using System.Windows.Data;

namespace NuGet.Dialog.PackageManagerUI
{
    public class DescriptionLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var releaseNotes = (string)value;
            return String.IsNullOrEmpty(releaseNotes) ?
                Resources.Dialog_DescriptionLabel :
                Resources.Dialog_ReleaseNotesLabel;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}