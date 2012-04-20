using System;
using System.Reflection;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using NuGet.VisualStudio;

namespace NuGet.Dialog 
{
    public static class UIResources 
    {
        private static object _windowBackgroundBrushKey;
        private static object _contentBackgroundBrushKey;

        /// <summary>
        /// This key is used as the background for our dialogs.
        /// </summary>
        public static object WindowBackgroundBrushKey 
        {
            get
            {
                if (VsVersionHelper.IsVisualStudio2010)
                {

                    return VsBrushes.NewProjectBackgroundKey;
                }
                else
                {
                    if (_windowBackgroundBrushKey == null)
                    {
                        _windowBackgroundBrushKey = GetObject(
                            "Microsoft.VisualStudio.ExtensionsExplorer.UI.ColorResources, Microsoft.VisualStudio.ExtensionsExplorer.UI", // Class Name, Assembly Name
                            "BackgroundBrushKey");  // Property Name
                    }

                    return _windowBackgroundBrushKey;
                }
            }
        }

        /// <summary>
        /// This key is used for the background of the disclaimer text.
        /// </summary>
        public static object ContentBackgroundBrushKey
        {
            get
            {
                if (VsVersionHelper.IsVisualStudio2010)
                {
                    return VsBrushes.WindowKey;
                }
                else
                {
                    if (_contentBackgroundBrushKey == null)
                    {
                        _contentBackgroundBrushKey = GetObject(
                            "Microsoft.VisualStudio.ExtensionsExplorer.UI.ColorResources, Microsoft.VisualStudio.ExtensionsExplorer.UI", // Class Name, Assembly Name
                            "WonderbarBrushKey");   // Property Name
                    }

                    return _contentBackgroundBrushKey;
                }
            }
        }

        private static object GetObject(string fullClassName, string property)
        {
            Type type = Type.GetType(fullClassName);
            if (type == null)
            {
                return null;
            }
            PropertyInfo propertyInfo = type.GetProperty(property, BindingFlags.Public | BindingFlags.Static);
            return propertyInfo != null ? propertyInfo.GetValue(null, null) : null;
        }
    }
}
