using EnvDTE;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace Nuget.Tools
{
    /// <summary>
    ///  Search Provider for Nuget Packages
    /// </summary>
    [Guid("042C2B4B-C7F7-49DB-B7A2-402EB8DC7891")]
    internal class NugetSearchProvider : IVsSearchProvider
    {
        private DTE _dte;
        private DTE DTE
        {
            get
            {
                if (null == _dte)
                {
                    _dte = (DTE)(Package.GetGlobalService(typeof(SDTE)));
                }
                return _dte;
            }
        }
        public Guid Category
        {
            get { return GetType().GUID; }
        }

        public IVsSearchTask CreateSearch(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchProviderCallback pSearchCallback)
        {
            if (dwCookie == VSConstants.VSCOOKIE_NIL)
            {
                return null;
            }

            return new NugetSearchTask(DTE, this, dwCookie, pSearchQuery, pSearchCallback);
        }

        public IVsSearchItemResult CreateItemResult(string lpszPersistenceData)
        {
            return new NugetStaticSearchResult(DTE, lpszPersistenceData, this);
        }

        public string DisplayText
        {
            get { return Resources.NuGetSearchProvider_DisplayText; }
        }

        public string Description
        {
            get
            {
                return Resources.NuGetSearchProvider_Description;
            }
        }

        public void ProvideSearchSettings(IVsUIDataSource pSearchOptions)
        {
        }

        public string Shortcut
        {
            get
            {
                return Resources.NuGetSearchProvider_CategoryShortcut;
            }
        }

        public string Tooltip
        {
            get { return null; } //no additional tooltip
        }

        public IVsUIObject Icon
        {
            get
            {
                var image = BitmapFrame.Create(new Uri("pack://application:,,,/NugetExtensions;component/Resources/nuget.png", UriKind.RelativeOrAbsolute));
                return WpfPropertyValue.CreateIconObject(image);
            }
        }
    }
}
