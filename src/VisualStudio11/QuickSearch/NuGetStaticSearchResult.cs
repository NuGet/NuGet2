using System;
using System.Globalization;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGet.VisualStudio11
{
    public class NuGetStaticSearchResult : IVsSearchItemResult
    {
        private const string TabProvider = ";Online";
        private readonly string _searchText;
        private readonly DTE _dte;

        public NuGetStaticSearchResult(DTE dte, string searchText, NuGetSearchProvider provider)
        {
            _dte = dte;
            _searchText = searchText;

            DisplayText = String.Format(CultureInfo.CurrentCulture, Resources.NuGetStaticResult_DisplayText, searchText);
            PersistenceData = searchText;
            SearchProvider = provider;
        }

        public string Description
        {
            get { return null; }
        }

        public string DisplayText
        {
            get;
            private set;
        }

        public IVsUIObject Icon
        {
            get 
            { 
                // TODO: Supply nuget icon here
                return null; 
            }
        }

        public void InvokeAction()
        {
            _dte.ExecuteCommand("Project.ManageNuGetPackages", _searchText + TabProvider);
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
            get { return null; } 
        }
    }
}