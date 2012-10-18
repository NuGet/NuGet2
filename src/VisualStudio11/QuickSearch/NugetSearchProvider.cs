using System;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGet.VisualStudio11
{
    [Guid("042C2B4B-C7F7-49DB-B7A2-402EB8DC7892")]
    public class NuGetSearchProvider : IVsSearchProvider
    {
        private readonly DTE _dte;

        public NuGetSearchProvider(DTE dte)
        {
            if (dte == null)
            {
                throw new ArgumentNullException("dte");
            }

            _dte = dte;
        }

        public Guid Category
        {
            get { return GetType().GUID; }
        }

        public IVsSearchTask CreateSearch(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchProviderCallback pSearchCallback)
        {
            if (dwCookie == 0)
            {
                return null;
            }

            return new NuGetSearchTask(_dte, this, dwCookie, pSearchQuery, pSearchCallback);
        }

        public IVsSearchItemResult CreateItemResult(string lpszPersistenceData)
        {
            return new NuGetStaticSearchResult(_dte, lpszPersistenceData, this);
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
            get { return null; } 
        }
    }
}
