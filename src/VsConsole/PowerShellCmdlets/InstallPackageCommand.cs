using System;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Net.NetworkInformation;
using NuGet.VisualStudio;

namespace NuGet.PowerShell.Commands
{
    /// <summary>
    /// This command installs the specified package into the specified project.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Install, "Package")]
    public class InstallPackageCommand : ProcessPackageBaseCommand
    {
        private readonly IVsPackageSourceProvider _packageSourceProvider;
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IProductUpdateService _productUpdateService;
        private bool _hasConnectedToHttpSource;

        public InstallPackageCommand()
            : this(ServiceLocator.GetInstance<ISolutionManager>(),
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                   ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                   ServiceLocator.GetInstance<IVsPackageSourceProvider>(),
                   ServiceLocator.GetInstance<IHttpClientEvents>(),
                   ServiceLocator.GetInstance<IProductUpdateService>(),
                   ServiceLocator.GetInstance<IVsCommonOperations>(),
                   ServiceLocator.GetInstance<IDeleteOnRestartManager>())
        {
        }

        public InstallPackageCommand(
            ISolutionManager solutionManager,
            IVsPackageManagerFactory packageManagerFactory,
            IPackageRepositoryFactory repositoryFactory,
            IVsPackageSourceProvider packageSourceProvider,
            IHttpClientEvents httpClientEvents,
            IProductUpdateService productUpdateService,
            IVsCommonOperations vsCommonOperations,
            IDeleteOnRestartManager deleteOnRestartManager)
            : base(solutionManager, packageManagerFactory, httpClientEvents, vsCommonOperations, deleteOnRestartManager)
        {
            _productUpdateService = productUpdateService;
            _repositoryFactory = repositoryFactory;
            _packageSourceProvider = packageSourceProvider;
        }

        [Parameter(Position = 2)]
        [ValidateNotNull]
        public SemanticVersion Version { get; set; }

        [Parameter(Position = 3)]
        [ValidateNotNullOrEmpty]
        public string Source { get; set; }

        [Parameter]
        public SwitchParameter IgnoreDependencies { get; set; }

        [Parameter, Alias("Prerelease")]
        public SwitchParameter IncludePrerelease { get; set; }

        [Parameter]
        public FileConflictAction FileConflictAction { get; set; }

        [Parameter]
        public SwitchParameter WhatIf { get; set; }

        [Parameter]
        public SwitchParameter MaxDependencyPatches { get; set; }

        private string _fallbackToLocalCacheMessge = Resources.Cmdlet_FallbackToCache;
        private string _localCacheFailureMessage = Resources.Cmdlet_LocalCacheFailure;
        private string _cacheStatusMessage = String.Empty;
        // Type for _currentSource can be either string (actual path to the Source), or PackageSource.
        private object _currentSource = String.Empty;

        protected override IVsPackageManager CreatePackageManager()
        {
            if (!SolutionManager.IsSolutionOpen)
            {
                return null;
            }
            
            if (_packageSourceProvider != null && _packageSourceProvider.ActivePackageSource != null)
            {
                FallbackToCacheIfNeccessary();
            }            
            
            if (!String.IsNullOrEmpty(Source))
            {
                var repository = CreateRepositoryFromSource(_repositoryFactory, _packageSourceProvider, Source);
                return repository == null ? null : PackageManagerFactory.CreatePackageManager(repository, useFallbackForDependencies: true);
            }

            return base.CreatePackageManager();
        }

        private void FallbackToCacheIfNeccessary()
        {
            /**** Fallback to Cache logic***/
            //1. Check if there is any http source (in active sources or Source switch)
            //2. Check if any one of the UNC or local sources is available (in active sources)
            //3. If none of the above is true, fallback to cache

            //Check if any of the active package source is available. This function will return true if there is any http source in active sources
            //For http sources, we will continue and fallback to cache at a later point if the resource is unavailable
            
            if (String.IsNullOrEmpty(Source))
            {
                bool isAnySourceAvailable = false;
                _currentSource = _packageSourceProvider.ActivePackageSource;
                isAnySourceAvailable = UriHelper.IsAnySourceAvailable(_packageSourceProvider, NetworkInterface.GetIsNetworkAvailable());

                //if no local or UNC source is available or no source is http, fallback to local cache
                if (!isAnySourceAvailable)
                {
                    Source = NuGet.MachineCache.Default.Source;
                    CacheStatusMessage(_currentSource, Source);
                }
            }

            //At this point, Source might be value from -Source switch or NuGet Local Cache
            /**** End of Fallback to Cache logic ***/
        }

        protected override void ProcessRecordCore()
        {
            if (!SolutionManager.IsSolutionOpen)
            {
                ErrorHandler.ThrowSolutionNotOpenTerminatingError();
            }

            try
            {
                SubscribeToProgressEvents();

                if (PackageManager == null)
                {
                    return;
                }
                
                if (ProjectManager != null)
                {
                    ProjectManager.MaxDependencyPatches = MaxDependencyPatches;
                }

                if (!String.IsNullOrEmpty(_cacheStatusMessage))
                {
                    this.Log(MessageLevel.Warning, String.Format(CultureInfo.CurrentCulture, _cacheStatusMessage, _packageSourceProvider.ActivePackageSource, Source));
                }

                InstallPackage(PackageManager);
                _hasConnectedToHttpSource |= UriHelper.IsHttpSource(Source, _packageSourceProvider);
            }
            //If the http source is not available, we fallback to NuGet Local Cache
            catch (Exception ex)
            {
                if ((ex is System.Net.WebException) ||
                    (ex.InnerException is System.Net.WebException) ||
                    (ex.InnerException is System.InvalidOperationException))
                {
                    string cache = NuGet.MachineCache.Default.Source;
                    if (!String.IsNullOrEmpty(cache))
                    {
                        this.Log(MessageLevel.Warning, String.Format(CultureInfo.CurrentCulture, _fallbackToLocalCacheMessge, _currentSource, cache));
                        var repository = CreateRepositoryFromSource(_repositoryFactory, _packageSourceProvider, cache);
                        IVsPackageManager packageManager = (repository == null ? null : PackageManagerFactory.CreatePackageManager(repository, useFallbackForDependencies: true));
                        InstallPackage(packageManager);
                    }
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                UnsubscribeFromProgressEvents();
            }
        }

        public override FileConflictResolution ResolveFileConflict(string message)
        {
            if (FileConflictAction == FileConflictAction.Overwrite)
            {
                return FileConflictResolution.Overwrite;
            }

            if (FileConflictAction == FileConflictAction.Ignore)
            {
                return FileConflictResolution.Ignore;
            }

            return base.ResolveFileConflict(message);
        }

        protected override void EndProcessing()
        {
            base.EndProcessing();

            CheckForNuGetUpdate();
        }

        private void CheckForNuGetUpdate()
        {
            if (_productUpdateService != null && _hasConnectedToHttpSource)
            {
                _productUpdateService.CheckForAvailableUpdateAsync();
            }
        }

        private void CacheStatusMessage(object currentSource, string cacheSource)
        {
            if (!String.IsNullOrEmpty(cacheSource))
            {
                _cacheStatusMessage = String.Format(CultureInfo.CurrentCulture, _fallbackToLocalCacheMessge, currentSource, Source);
            }
            else
            {
                _cacheStatusMessage = String.Format(CultureInfo.CurrentCulture, _localCacheFailureMessage, currentSource);
            }
        }       

        private void InstallPackage(IVsPackageManager packageManager)
        {
            if (packageManager == null)
            {
                return;
            }

            packageManager.MaxDependencyPatches = MaxDependencyPatches;
            packageManager.WhatIf = WhatIf;
            packageManager.InstallPackage(ProjectManager, Id, Version, IgnoreDependencies, IncludePrerelease.IsPresent, logger: this);
        }
    }
}