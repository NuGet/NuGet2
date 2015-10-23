using System;
using System.Globalization;
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
        private readonly bool _isNetworkAvailable;
        private readonly string _fallbackToLocalCacheMessge = Resources.Cmdlet_FallbackToCache;
        private readonly string _localCacheFailureMessage = Resources.Cmdlet_LocalCacheFailure;
        private bool _hasConnectedToHttpSource;
        private string _cacheStatusMessage = string.Empty;
        // Type for _currentSource can be either string (actual path to the Source), or PackageSource.
        private object _currentSource = string.Empty;

        public InstallPackageCommand()
            : this(ServiceLocator.GetInstance<ISolutionManager>(),
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                   ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                   ServiceLocator.GetInstance<IVsPackageSourceProvider>(),
                   ServiceLocator.GetInstance<IHttpClientEvents>(),
                   ServiceLocator.GetInstance<IProductUpdateService>(),
                   ServiceLocator.GetInstance<IVsCommonOperations>(),
                   ServiceLocator.GetInstance<IDeleteOnRestartManager>(),
                   isNetworkAvailable())
        {
        }

        internal InstallPackageCommand(
            ISolutionManager solutionManager,
            IVsPackageManagerFactory packageManagerFactory,
            IPackageRepositoryFactory repositoryFactory,
            IVsPackageSourceProvider packageSourceProvider,
            IHttpClientEvents httpClientEvents,
            IProductUpdateService productUpdateService,
            IVsCommonOperations vsCommonOperations,
            IDeleteOnRestartManager deleteOnRestartManager,
            bool networkAvailable)
            : base(solutionManager, packageManagerFactory, httpClientEvents, vsCommonOperations, deleteOnRestartManager)
        {
            _productUpdateService = productUpdateService;
            _repositoryFactory = repositoryFactory;
            _packageSourceProvider = packageSourceProvider;
            
            if (networkAvailable)
            {
                _isNetworkAvailable = isNetworkAvailable();
            }
            else
            {
                _isNetworkAvailable = false;
            }
        }

        private static bool isNetworkAvailable()
        {
            return NetworkInterface.GetIsNetworkAvailable();
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
        public DependencyVersion? DependencyVersion { get; set; }
        
        protected override IVsPackageManager CreatePackageManager()
        {
            if (!SolutionManager.IsSolutionOpen)
            {
                return null;
            }
            
            if (_packageSourceProvider != null && _packageSourceProvider.ActivePackageSource != null && String.IsNullOrEmpty(Source))
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
                isAnySourceAvailable = UriHelper.IsAnySourceAvailable(_packageSourceProvider, _isNetworkAvailable);

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

        private bool IsDowngradePackage()
        {
            //if Version to downgrade is not specified, bail out
            if (Version != null && ProjectManager != null)
            {
                //Check if the package is installed
                IPackage packageToBeUninstalled = ProjectManager.LocalRepository.FindPackage(Id);
                //Downgrade only if package to be installed newly is lower version than the one currently installed
                if (packageToBeUninstalled != null && packageToBeUninstalled.Version > Version)
                {
                   return true;
                }
            }
            return false;
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
                PackageManager.WhatIf = WhatIf;
                if (DependencyVersion.HasValue)
                {
                    PackageManager.DependencyVersion = DependencyVersion.Value;
                }

                if (ProjectManager != null)
                {
                    ProjectManager.DependencyVersion = PackageManager.DependencyVersion;
                    ProjectManager.WhatIf = WhatIf;
                }

                if (!String.IsNullOrEmpty(_cacheStatusMessage))
                {
                    Logger.Log(MessageLevel.Warning, String.Format(CultureInfo.CurrentCulture, _cacheStatusMessage, _packageSourceProvider.ActivePackageSource, Source));
                }                
            
                if (IsDowngradePackage())
                {
                    PackageManager.UpdatePackage(ProjectManager, Id, Version, !IgnoreDependencies, IncludePrerelease.IsPresent, logger: Logger);
                }
                else
                {
                    InstallPackage(PackageManager);
                }
                _hasConnectedToHttpSource |= UriHelper.IsHttpSource(Source, _packageSourceProvider);
            }
            //If the http source is not available, we fallback to NuGet Local Cache
            //Skip if Source flag has been set (Fix for bug http://nuget.codeplex.com/workitem/3776)
            catch (Exception ex)
            {
                if (((ex is System.Net.WebException) ||
                    (ex.InnerException is System.Net.WebException) ||
                    (ex.InnerException is System.InvalidOperationException))
                    && (String.IsNullOrEmpty(Source)))
                {
                    string cache = NuGet.MachineCache.Default.Source;
                    if (!String.IsNullOrEmpty(cache))
                    {
                        Logger.Log(MessageLevel.Warning, String.Format(CultureInfo.CurrentCulture, _fallbackToLocalCacheMessge, _currentSource, cache));
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

            packageManager.InstallPackage(ProjectManager, Id, Version, IgnoreDependencies, IncludePrerelease.IsPresent, logger: Logger);
        }
    }
}