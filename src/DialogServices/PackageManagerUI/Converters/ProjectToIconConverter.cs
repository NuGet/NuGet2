using System;
using System.Collections.Generic;
using System.Windows.Data;
using System.Windows.Media;
using EnvDTE;

namespace NuGet.Dialog.PackageManagerUI
{
    public class ProjectToIconConverter : IValueConverter
    {
        private static readonly Dictionary<string, ImageSource> _imageCache = new Dictionary<string, ImageSource>();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            Project project = (Project)value;
            string kind = project.Kind;

            ImageSource imageSource;
            if (!_imageCache.TryGetValue(kind, out imageSource))
            {
                imageSource = ProjectUtilities.GetImage(project);
                _imageCache.Add(kind, imageSource);
            }

            return imageSource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}