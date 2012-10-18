using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace Nuget.Tools
{
    public sealed class NugetSearchTask : VsSearchTask
    {
        private readonly DTE _dte;
        private readonly NugetSearchProvider _provider;

        public NugetSearchTask(DTE dte, NugetSearchProvider provider, uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchProviderCallback pSearchCallback)
            : base(dwCookie, pSearchQuery, pSearchCallback)
        {
            _provider = provider;
            _dte = dte;
        }

        protected override void OnStartSearch()
        {
            try
            {
                var result = new NugetStaticSearchResult(_dte, SearchQuery.SearchString, _provider);
                var searchProviderCallback = (IVsSearchProviderCallback)base.SearchCallback;

                searchProviderCallback.ReportResult(this, result);
                searchProviderCallback.ReportComplete(this, 1);
            }
            catch (Exception)
            {
            }
        }
    }
}