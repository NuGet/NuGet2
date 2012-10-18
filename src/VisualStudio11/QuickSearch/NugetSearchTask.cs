using System;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGet.VisualStudio11
{
    public sealed class NuGetSearchTask : IVsSearchTask
    {
        private readonly DTE _dte;
        private readonly NuGetSearchProvider _provider;
        private readonly IVsSearchProviderCallback _searchCallback;

        public NuGetSearchTask(DTE dte, NuGetSearchProvider provider, uint cookie, IVsSearchQuery searchQuery, IVsSearchProviderCallback searchCallback)
        {
            _provider = provider;
            _dte = dte;
            _searchCallback = searchCallback;

            SearchQuery = searchQuery;
            Id = cookie;
            ErrorCode = 0;

            SetStatus(VsSearchTaskStatus.Created);
        }

        public int ErrorCode
        {
            get;
            private set;
        }

        public uint Id
        {
            get;
            private set;
        }

        public IVsSearchQuery SearchQuery
        {
            get;
            private set;
        }

        public uint Status
        {
            get;
            private set;
        }

        public void Start()
        {
            SetStatus(VsSearchTaskStatus.Started);

            SetStatus(VsSearchTaskStatus.Completed);
            if (!String.IsNullOrEmpty(SearchQuery.SearchString))
            {
                var result = new NuGetStaticSearchResult(_dte, SearchQuery.SearchString, _provider);
                _searchCallback.ReportResult(this, result);
                _searchCallback.ReportComplete(this, 1);
            }
            else
            {
                _searchCallback.ReportComplete(this, 0);
            }
        }

        public void Stop()
        {
            SetStatus(VsSearchTaskStatus.Stopped);
        }

        private void SetStatus(VsSearchTaskStatus taskStatus)
        {
            Status = (uint)taskStatus;
        }

        private enum VsSearchTaskStatus : uint
        {
            Completed = 2,
            Created = 0,
            Error = 4,
            Started = 1,
            Stopped = 3
        }
    }
}