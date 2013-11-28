using System;
using System.Reflection;
using System.Windows;
using Microsoft.VisualStudio.Shell;

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
                if (NuGet.VisualStudio.VsVersionHelper.IsVisualStudio2010)
                {
                    return VsBrushes.NewProjectBackgroundKey;
                }
                else
                {
                    if (_windowBackgroundBrushKey == null)
                    {
                        _windowBackgroundBrushKey = GetObject(
                            "Microsoft.VisualStudio.ExtensionsExplorer.UI.ColorResources, Microsoft.VisualStudio.ExtensionsExplorer.UI", // Class Name, Assembly Name
                            "BackgroundBrushKey", // Property Name
                            VsBrushes.NewProjectBackgroundKey);  
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
                if (NuGet.VisualStudio.VsVersionHelper.IsVisualStudio2010)
                {
                    return VsBrushes.WindowKey;
                }
                else
                {
                    if (_contentBackgroundBrushKey == null)
                    {
                        _contentBackgroundBrushKey = GetObject(
                            "Microsoft.VisualStudio.ExtensionsExplorer.UI.ColorResources, Microsoft.VisualStudio.ExtensionsExplorer.UI", // Class Name, Assembly Name
                            "WonderbarBrushKey",    // Property Name
                            VsBrushes.WindowKey);   
                    }

                    return _contentBackgroundBrushKey;
                }
            }
        }

        private static object GetObject(string fullClassName, string property, object defaultValue)
        {
            Type type = Type.GetType(fullClassName);
            if (type == null)
            {
                return defaultValue;
            }

            PropertyInfo propertyInfo = type.GetProperty(property, BindingFlags.Public | BindingFlags.Static);
            if (propertyInfo == null)
            {
                return defaultValue;
            }
                
            return propertyInfo.GetValue(null, null) ?? defaultValue;
        }
    }
}