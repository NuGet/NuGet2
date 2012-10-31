using System;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGet.VisualStudio11
{
    public sealed class NuGetSearchTask : IVsSearchTask
    {
        private readonly NuGetSearchProvider _provider;
        private readonly IVsSearchProviderCallback _searchCallback;
        private readonly OleMenuCommand _managePackageDialogCommand;
        private readonly OleMenuCommand _managePackageForSolutionDialogCommand;

        public NuGetSearchTask(NuGetSearchProvider provider, uint cookie, IVsSearchQuery searchQuery, IVsSearchProviderCallback searchCallback, OleMenuCommand managePackageDialogCommand, OleMenuCommand managePackageForSolutionDialogCommand)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }
            if (searchQuery == null)
            {
                throw new ArgumentNullException("searchQuery");
            }
            if (searchCallback == null)
            {
                throw new ArgumentNullException("searchCallback");
            }
            if (managePackageDialogCommand == null)
            {
                throw new ArgumentNullException("managePackageDialogCommand");
            }
            if (managePackageForSolutionDialogCommand == null)
            {
                throw new ArgumentNullException("managePackageForSolutionDialogCommand");
            }
            _provider = provider;
            _searchCallback = searchCallback;
            _managePackageDialogCommand = managePackageDialogCommand;
            _managePackageForSolutionDialogCommand = managePackageForSolutionDialogCommand;

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
            OleMenuCommand supportedManagePackageCommand = GetSupportedManagePackageCommand();

            if (!String.IsNullOrEmpty(SearchQuery.SearchString) && null != supportedManagePackageCommand)
            {
                var result = new NuGetStaticSearchResult(SearchQuery.SearchString, _provider, supportedManagePackageCommand);
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

        private OleMenuCommand GetSupportedManagePackageCommand()
        {
            if (_managePackageDialogCommand.Enabled && _managePackageDialogCommand.Visible && _managePackageDialogCommand.Supported)
            {
                return _managePackageDialogCommand;
            }

            if (_managePackageForSolutionDialogCommand.Enabled && _managePackageForSolutionDialogCommand.Visible && _managePackageForSolutionDialogCommand.Supported)
            {
                return _managePackageForSolutionDialogCommand;
            }

            return null;
        }
    }
}