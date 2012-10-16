using System;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using Microsoft.Internal.VisualStudio.PlatformUI;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Globalization;

namespace Nuget.Tools
{
    internal class NugetStaticSearchResult : IVsSearchItemResult
    {
        private const string TabProvider = ";Online";
        private readonly string _searchText;

        private DTE _dte;
        public NugetStaticSearchResult(DTE dte, string searchText, NugetSearchProvider provider)
        {
            _searchText = searchText;
            DisplayText = String.Format(CultureInfo.CurrentCulture, Resources.NuGetStaticResult_DisplayText, searchText);
            PersistenceData = searchText;
            SearchProvider = provider;
            Icon = provider.Icon;
            _dte = dte;
        }

        public string Description
        {
            get;
            private set;
        }

        public string DisplayText
        {
            get;
            private set;
        }

        public IVsUIObject Icon
        {
            get;
            private set;
        }

        public void InvokeAction()
        {
            _dte.ExecuteCommand("Project.ManageNuGetPackages", String.Format("{0}{1}", _searchText, TabProvider));
        }

        public string PersistenceData
        {
            get;
            private set;
        }

        public IVsSearchProvider SearchProvider
        {
            get;
            private set;
        }

        public string Tooltip
        {
            get;
            private set;
        }
    }
}
