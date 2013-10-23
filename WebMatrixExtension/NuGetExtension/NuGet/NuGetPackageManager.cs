using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.WebMatrix.Extensibility;
using Microsoft.WebMatrix.PackageManagement;
using NuGet;

namespace NuGet.WebMatrix
{
    internal class NuGetPackageManager : INuGetPackageManager
    {
        // "WebMatrix {<assembly version>}-{<build number>}"
        private const string UserAgentClientFormat = "WebMatrix {0}-{1}";

        // We currently hardcode the targetFramework version to .NET40 
        // (VersionUtility.DefaultTargetFramework returns that value)
        // Once, site version is exposed via extensibility in v4 or later
        // we can change it
        private static readonly FrameworkName TargetFramework = VersionUtility.DefaultTargetFramework;
        private static readonly List<string> TargetFrameworks = new List<string>() { TargetFramework.FullName };

        private WebProjectManager _webProjectManager;

        private bool _includePrerelease;

        public NuGetPackageManager(Uri sourceUrl, string siteRoot)
            : this(sourceUrl, siteRoot, null)
        {
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="T:NuGetPackageManager"/> class.
        /// </summary>
        public NuGetPackageManager(Uri sourceUrl, string siteRoot, IWebMatrixHost host)
        {
            _webProjectManager = new WebProjectManager(sourceUrl.AbsoluteUri, siteRoot, host);

            // set the user agent to reflect the webmatrix assembly version and build number
            var extensibilityAssembly = typeof(IWebMatrixHost).Assembly;
            var assemblyVersion = extensibilityAssembly.GetName().Version;
            var buildNumber = FileVersionInfo.GetVersionInfo(extensibilityAssembly.Location).FileBuildPart;
            var clientVersion = String.Format(UserAgentClientFormat, assemblyVersion, buildNumber);
            this.UserAgent = HttpUtility.CreateUserAgentString(clientVersion);

            var sourceRepository = _webProjectManager.SourceRepository as DataServicePackageRepository;
            if (sourceRepository == null)
            {
                // right now we expect that we're always using DataServicePackageRepository to hit a remote feed
                Debug.Assert(sourceUrl.IsFile, "SourceRepository is not a DataServicePackageRepository");
            }
            else
            {
                // initialize settings that filter the list of packages specifically for WebMatrix
                sourceRepository.SendingRequest += new EventHandler<WebRequestEventArgs>(SourceRepository_SendingRequest);
            }
        }

        public string UserAgent
        {
            get;
            private set;
        }

        public IEnumerable<IPackage> FindDependenciesToBeInstalled(IPackage package)
        {
            // We don't currently sync the set of dependencies/packates to be installed
            // with the target framework of the user's site. We'd need to supply a framework
            // version here to add that feature.
            InstallWalker walker = new InstallWalker(
                _webProjectManager.LocalRepository, 
                _webProjectManager.SourceRepository,
                TargetFramework,
                NullLogger.Instance,
                ignoreDependencies: false, 
                allowPrereleaseVersions: IncludePrerelease);
            IEnumerable<PackageOperation> operations = walker.ResolveOperations(package);

            return from operation in operations
                   where operation.Package != package && operation.Action == PackageAction.Install
                   select operation.Package;
        }

        public IEnumerable<IPackage> GetPackagesToBeInstalledForUpdateAll()
        {
            InstallWalker walker = new InstallWalker(
                _webProjectManager.LocalRepository,
                _webProjectManager.SourceRepository,
                TargetFramework,
                NullLogger.Instance,
                ignoreDependencies: false,
                allowPrereleaseVersions: IncludePrerelease);
            
            var packagesToUpdate = GetPackagesWithUpdates();
            var allOperations = new List<PackageOperation>();

            foreach (IPackage package in packagesToUpdate)
            {
                if (!allOperations.Any(operation => operation.Action == PackageAction.Install &&
                    operation.Package.Id == package.Id &&
                    operation.Package.Version == package.Version))
                {
                    var operations = walker.ResolveOperations(package);
                    allOperations.AddRange(operations);
                }
            }

            return (from operation in allOperations
                   where operation.Action == PackageAction.Install
                   select operation.Package).Distinct();
        }

        public IPackage FindPackage(string packageId, SemanticVersion version)
        {
            return _webProjectManager.SourceRepository.FindPackage(packageId, version, allowPrereleaseVersions: IncludePrerelease, allowUnlisted: false);
        }

        public IEnumerable<IPackage> FindPackages(IEnumerable<string> packageIds)
        {
            return _webProjectManager.SourceRepository.FindPackages(packageIds);
        }

        public IQueryable<IPackage> GetInstalledPackages()
        {
            return _webProjectManager.GetInstalledPackages(null);
        }

        public IEnumerable<IPackage> GetPackagesWithUpdates()
        {
            return _webProjectManager.GetPackagesWithUpdates(null, false);
        }

        public IQueryable<IPackage> GetRemotePackages()
        {
            if (IncludePrerelease)
            {
                return _webProjectManager.GetRemotePackages(null, false)
                    .Where(p => p.IsAbsoluteLatestVersion);
            }
            else
            {
                return _webProjectManager.GetRemotePackages(null, false)
                    .Where(p => p.IsLatestVersion);
            }
        }

        public virtual IEnumerable<string> InstallPackage(IPackage package)
        {
            AppDomain appDomain = null;
            try
            {
                appDomain = AppDomain.CreateDomain(Guid.NewGuid().ToString());
                return _webProjectManager.InstallPackage(package, appDomain);
            }
            finally
            {
                if (appDomain != null)
                {
                    AppDomain.Unload(appDomain);
                }
            }
        }

        public bool IsPackageInstalled(IPackage package)
        {
            return _webProjectManager.IsPackageInstalled(package);
        }

        public virtual IQueryable<IPackage> SearchRemotePackages(string searchText)
        {
            if (IncludePrerelease)
            {
                return _webProjectManager.SourceRepository.Search(searchText, targetFrameworks: TargetFrameworks, allowPrereleaseVersions: IncludePrerelease)
                    .Where(p => p.IsAbsoluteLatestVersion);
            }
            else
            {
                return _webProjectManager.SourceRepository.Search(searchText, targetFrameworks: TargetFrameworks, allowPrereleaseVersions: IncludePrerelease)
                    .Where(p => p.IsLatestVersion);
            }
        }

        public virtual IEnumerable<string> UninstallPackage(IPackage package)
        {
            return _webProjectManager.UninstallPackage(package, false);
        }

        public virtual IEnumerable<string> UpdatePackage(IPackage package)
        {
            AppDomain appDomain = null;
            try
            {
                appDomain = AppDomain.CreateDomain(Guid.NewGuid().ToString());
                return _webProjectManager.UpdatePackage(package, appDomain);
            }
            finally
            {
                if (appDomain != null)
                {
                    AppDomain.Unload(appDomain);
                }
            }
        }

        public IEnumerable<string> UpdateAllPackages()
        {
            return _webProjectManager.UpdateAllPackages();
        }

        public IPackage GetUpdate(IPackage package)
        {
            return _webProjectManager.GetUpdate(package);
        }

        protected string SourceRepositorySource
        {
            get
            {
                return (_webProjectManager.SourceRepository != null) ?
                    _webProjectManager.SourceRepository.Source :
                    null;
            }
        }

        private void SourceRepository_SendingRequest(object sender, WebRequestEventArgs e)
        {
            HttpUtility.SetUserAgent(e.Request, this.UserAgent);
        }

        public virtual bool SupportsEnableDisable
        {
            get { return false; }
        }

        public virtual bool IsPackageEnabled(IPackage package)
        {
            return true;
        }

        public virtual void TogglePackageEnabled(IPackage package)
        {
            throw new NotImplementedException();
        }

        public virtual bool IsMandatory(IPackage package)
        {
            return false;
        }

        public virtual bool IncludePrerelease
        {
            get
            {
                return _includePrerelease;
            }

            set
            {
                _webProjectManager.IncludePrerelease = _includePrerelease = value;
            }
        }
    }
}
