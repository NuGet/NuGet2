using System;
using System.Management.Automation;
using NuGet.VisualStudio;

namespace NuGet.PowerShell.Commands
{

    [Cmdlet(VerbsCommon.Open, "PackagePage", DefaultParameterSetName = ParameterAttribute.AllParameterSets, SupportsShouldProcess = true)]
    public class OpenPackagePageCommand : NuGetBaseCommand
    {

        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IVsPackageSourceProvider _packageSourceProvider;

        public OpenPackagePageCommand()
            : this(ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                   ServiceLocator.GetInstance<IVsPackageSourceProvider>())
        {
        }

        public OpenPackagePageCommand(IPackageRepositoryFactory repositoryFactory,
                                IVsPackageSourceProvider packageSourceProvider)
            : base(null, null, null)
        {
            if (repositoryFactory == null)
            {
                throw new ArgumentNullException("repositoryFactory");
            }
            if (packageSourceProvider == null)
            {
                throw new ArgumentNullException("packageSourceProvider");
            }

            _repositoryFactory = repositoryFactory;
            _packageSourceProvider = packageSourceProvider;
        }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        public string Id { get; set; }

        [Parameter(Position = 1)]
        [ValidateNotNull]
        public SemanticVersion Version { get; set; }

        [Parameter(Position = 2)]
        [ValidateNotNullOrEmpty]
        public string Source { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "License")]
        public SwitchParameter License { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ReportAbuse")]
        public SwitchParameter ReportAbuse { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecordCore()
        {
            IPackageRepository repository = GetRepository();

            IPackage package = repository.FindPackage(Id, Version);
            if (package != null)
            {
                Uri targetUrl;
                if (License.IsPresent)
                {
                    targetUrl = package.LicenseUrl;
                }
                else if (ReportAbuse.IsPresent)
                {
                    targetUrl = package.ReportAbuseUrl;
                }
                else
                {
                    targetUrl = package.ProjectUrl;
                }

                if (targetUrl != null)
                {
                    OpenUrl(targetUrl);

                    if (PassThru.IsPresent)
                    {
                        WriteObject(targetUrl);
                    }
                }
                else
                {
                    Logger.Log(MessageLevel.Error, Resources.Cmdlet_UrlMissing, package);
                }
            }
            else
            {
                // show appropriate error message depending on whether Version parameter is set.
                if (Version == null)
                {
                    Logger.Log(MessageLevel.Error, Resources.Cmdlet_PackageIdNotFound, Id);
                }
                else
                {
                    Logger.Log(MessageLevel.Error, Resources.Cmdlet_PackageIdAndVersionNotFound, Id, Version);
                }
            }
        }

        private void OpenUrl(Uri targetUrl)
        {
            // ask for confirmation or if WhatIf is specified
            if (ShouldProcess(targetUrl.OriginalString, Resources.Cmdlet_OpenPackagePageAction))
            {
                UriHelper.OpenExternalLink(targetUrl);
            }
        }

        /// <summary>
        /// Determines the repository to be used based on the Source parameter
        /// </summary>
        private IPackageRepository GetRepository()
        {
            if (!String.IsNullOrEmpty(Source))
            {
                // If a Source parameter is explicitly specified, use it
                return _repositoryFactory.CreateRepository(Source);
            }
            else if (_packageSourceProvider.ActivePackageSource != null)
            {
                // No Source available. Use the active package source to create a new repository
                return _repositoryFactory.CreateRepository(_packageSourceProvider.ActivePackageSource.Source);
            }
            else
            {
                // No active source has been specified. 
                throw new InvalidOperationException(Resources.Cmdlet_NoActivePackageSource);
            }
        }
    }
}