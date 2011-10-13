using System;
using System.Windows.Data;
using EnvDTE;
using NuGet.VisualStudio;

namespace NuGet.Dialog.PackageManagerUI
{
    public class ProjectToDisplayNameConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var project = (Project)value;
            return project.GetDisplayName();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
