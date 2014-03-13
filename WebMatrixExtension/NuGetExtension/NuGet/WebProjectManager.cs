using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WebMatrix.Core;
using Microsoft.WebMatrix.Extensibility;
using Microsoft.WebMatrix.SourceControl;
using NuGet;
using NuGet.Runtime;

namespace NuGet.WebMatrix
{
    public class WebProjectManager
    {
        private const string WebPagesPreferredTag = " aspnetwebpages ";
        private readonly IProjectManager _projectManager;
        private readonly string _siteRoot;
        private bool _allowPrereleaseVersions;

        // We currently hardcode the targetFramework version to .NET40 
        // (VersionUtility.DefaultTargetFramework returns that value)
        // Once, site version is exposed via extensibility in v4 or later
        // we can change it
        private static readonly FrameworkName TargetFramework = VersionUtility.DefaultTargetFramework;
        private static readonly List<FrameworkName> TargetFrameworks = new List<FrameworkName>() { TargetFramework };

        private ErrorLogger Logger
        {
            get;
            set;
        }

        public bool IncludePrerelease
        {
            private get
            {
                return _allowPrereleaseVersions;
            }

            set
            {
                _allowPrereleaseVersions = value;
            }
        }

        public WebProjectManager(string remoteSource, string siteRoot)
            : this(remoteSource, siteRoot, null)
        {
        }

        public WebProjectManager(string remoteSource, string siteRoot, IWebMatrixHost host)
            : this(PackageRepositoryFactory.Default.CreateRepository(remoteSource), siteRoot, host)
        {
        }

        public WebProjectManager(IPackageRepository source, string siteRoot)
            : this(source, siteRoot, null)
        {
        }

        public WebProjectManager(IPackageRepository source, string siteRoot, IWebMatrixHost host)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (String.IsNullOrEmpty(siteRoot))
            {
                throw new ArgumentException("siteRoot");
            }

            _siteRoot = siteRoot;
            string webRepositoryDirectory = GetWebRepositoryDirectory(siteRoot);

            Logger = new ErrorLogger(host);

            var project = new WebProjectSystem(siteRoot);

            project.Logger = Logger;

            _projectManager = new ProjectManager(sourceRepository: source,
                                                   pathResolver: new DefaultPackagePathResolver(webRepositoryDirectory),
                                                   localRepository: PackageRepositoryFactory.Default.CreateRepository(webRepositoryDirectory),
                                                   project: project);
        }

        internal WebProjectManager(IProjectManager projectManager, string siteRoot)
        {
            if (String.IsNullOrEmpty(siteRoot))
            {
                throw new ArgumentException("siteRoot");
            }

            if (projectManager == null)
            {
                throw new ArgumentNullException("projectManager");
            }

            _siteRoot = siteRoot;
            _projectManager = projectManager;
        }

        public IPackageRepository LocalRepository
        {
            get { return _projectManager.LocalRepository; }
        }

        public IPackageRepository SourceRepository
        {
            get { return _projectManager.SourceRepository; }
        }

        internal bool DoNotAddBindingRedirects { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#",
            Justification = "We want to ensure we get server-side counts for the IQueryable which can only be performed before we collapse versions.")]
        public virtual IQueryable<IPackage> GetRemotePackages(string searchTerms, bool filterPreferred)
        {
            var packages = GetPackages(SourceRepository, searchTerms, _allowPrereleaseVersions);
            if (filterPreferred)
            {
                packages = packages.Where(p => p.Tags.ToLower().Contains(WebPagesPreferredTag));
            }

            // Order by download count and Id to allow collapsing 
            return packages.OrderByDescending(p => p.DownloadCount)
                .ThenBy(p => p.Id);
        }

        public IQueryable<IPackage> GetInstalledPackages(string searchTerms)
        {
            // Always return all installed packages including prerelease packages
            return GetPackages(LocalRepository, searchTerms, allowPrereleaseVersions: true);
        }

        public IEnumerable<IPackage> GetPackagesWithUpdates(string searchTerms, bool filterPreferredPackages)
        {
            var packagesToUpdate = GetPackages(LocalRepository, searchTerms, _allowPrereleaseVersions);
            if (filterPreferredPackages)
            {
                packagesToUpdate = packagesToUpdate.Where(p => p.Tags.ToLower().Contains(WebPagesPreferredTag));
            }

            return SourceRepository.GetUpdates(packagesToUpdate, includePrerelease: _allowPrereleaseVersions, includeAllVersions:false, targetFrameworks: TargetFrameworks).AsQueryable();
        }

        internal IEnumerable<string> InstallPackage(IPackage package)
        {
            return InstallPackage(package, AppDomain.CurrentDomain);
        }

        /// <summary>
        /// Installs and adds a package reference to the project
        /// </summary>
        /// <returns>Warnings encountered when installing the package.</returns>
        public IEnumerable<string> InstallPackage(IPackage package, AppDomain appDomain)
        {
            return InstallPackage(package, false, appDomain);
        }

        /// <summary>
        /// Installs and adds a package reference to the project
        /// </summary>
        /// <returns>Warnings encountered when installing the package.</returns>
        public IEnumerable<string> InstallPackage(IPackage package, bool ignoreDependencies, AppDomain appDomain)
        {
            IEnumerable<string> result = PerformLoggedAction(() =>
            {
                _projectManager.AddPackageReference(package.Id, package.Version, ignoreDependencies, allowPrereleaseVersions: _allowPrereleaseVersions);
                AddBindingRedirects(appDomain);
            });
            return result;
        }
        
        internal IEnumerable<string> UpdatePackage(IPackage package)
        {
            return UpdatePackage(package, AppDomain.CurrentDomain);
        }

        /// <summary>
        /// Updates a package reference. Installs the package to the App_Data repository if it does not already exist.
        /// </summary>
        /// <returns>Warnings encountered when updating the package.</returns>
        public IEnumerable<string> UpdatePackage(IPackage package, AppDomain appDomain)
        {
            return PerformLoggedAction(() =>
            {
                _projectManager.UpdatePackageReference(package.Id, package.Version, updateDependencies: true, allowPrereleaseVersions: _allowPrereleaseVersions);
                AddBindingRedirects(appDomain);
            });
        }

        public IEnumerable<string> UpdateAllPackages()
        {
            var packageSorter = new PackageSorter(targetFramework: TargetFramework);
            // Get the packages in reverse dependency order then run update on each one i.e. if A -> B run Update(A) then Update(B)
            var packagesToUpdate = GetPackagesWithUpdates(null, false);
            var packages = packageSorter.GetPackagesByDependencyOrder(LocalRepository).Reverse();
            var allErrors = new List<string>();

            foreach (var package in packages)
            {
                var packageToUpdateNow = packagesToUpdate.Where(p => p.Id.Equals(package.Id, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
                if (packageToUpdateNow != null)
                {
                    // While updating we might remove packages that were initially in the list. e.g.
                    // A 1.0 -> B 2.0, A 2.0 -> [], since updating to A 2.0 removes B, we end up skipping it.
                    if (LocalRepository.Exists(packageToUpdateNow.Id))
                    {
                        AppDomain appDomain = null;
                        try
                        {
                            appDomain = AppDomain.CreateDomain(Guid.NewGuid().ToString());
                            var errors = UpdatePackage(packageToUpdateNow, appDomain);
                            if (!errors.IsEmpty())
                            {
                                allErrors.Concat(errors);
                            }
                        }
                        finally
                        {
                            if (appDomain != null)
                            {
                                AppDomain.Unload(appDomain);
                            }
                        }
                    }
                }
            }
            return allErrors;
        }

        /// <summary>
        /// Removes a package reference and uninstalls the package
        /// </summary>
        /// <returns>Warnings encountered when uninstalling the package.</returns>
        public IEnumerable<string> UninstallPackage(IPackage package, bool removeDependencies)
        {
            return PerformLoggedAction(() =>
            {
                _projectManager.RemovePackageReference(package.Id, forceRemove: false, removeDependencies: removeDependencies);
            });
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "It seems more appropriate to deal with IPackages")]
        public bool IsPackageInstalled(IPackage package)
        {
            return LocalRepository.Exists(package);
        }

        public IPackage GetUpdate(IPackage package)
        {
            return SourceRepository.GetUpdates(new[] { package }, includePrerelease: _allowPrereleaseVersions, includeAllVersions: false, targetFrameworks: TargetFrameworks).SingleOrDefault();
        }

        private void AddBindingRedirects(AppDomain appDomain)
        {
            if (DoNotAddBindingRedirects)
            {
                return;
            }
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(appDomain_AssemblyResolve);
            // We can't use HttpRuntime.BinDirectory since there is no runtime when installing via WebMatrix.
            var binDirectory = Path.Combine(_siteRoot, "bin");
            var assemblies = RemoteAssembly.GetAssembliesForBindingRedirect(appDomain, binDirectory);
            var bindingRedirects = BindingRedirectResolver.GetBindingRedirects(assemblies);

            if (bindingRedirects.Any())
            {
                // NuGet ends up reading our web.config file regardless of if any bindingRedirects are needed.
                var bindingRedirectManager = new BindingRedirectManager(_projectManager.Project, "web.config");
                bindingRedirectManager.AddBindingRedirects(bindingRedirects);
            }
            AppDomain.CurrentDomain.AssemblyResolve -= appDomain_AssemblyResolve;
        }

        /// <summary>
        /// WebMatrix extensions folder is not on the ApplicationBase of an AppDomain
        /// This means NuGet.Core.dll and NuGetExtension.dll cannot be loaded and might result in FileLoadException
        /// This ResolveEventHandler helps solve this issue
        /// </summary>
        private static Assembly appDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName assemblyName = new AssemblyName(args.Name);
            if (assemblyName.Name.Equals("NuGet.Core"))
            {
                return typeof(IAssembly).Assembly;
            }
            if (assemblyName.Name.Contains("NuGetExtension"))
            {
                return typeof(RemoteAssembly).Assembly;
            }

            // THIS SHOULD NEVER HAPPEN. Because, only NuGetExtension.dll and NuGet.Core.dll
            // are not on the application base of the ApplicationDomain
            return Assembly.Load(assemblyName);
        }

        private IEnumerable<string> PerformLoggedAction(Action action)
        {
            _projectManager.Logger = Logger;

            Logger.Clear();

            try
            {
                action();
            }
            finally
            {
                _projectManager.Logger = null;
            }

            return Logger.Errors;
        }

        internal IEnumerable<IPackage> GetPackagesRequiringLicenseAcceptance(IPackage package)
        {
            return GetPackagesRequiringLicenseAcceptance(package, localRepository: LocalRepository, sourceRepository: SourceRepository, allowPrereleaseVersions: _allowPrereleaseVersions);
        }

        internal static IEnumerable<IPackage> GetPackagesRequiringLicenseAcceptance(IPackage package, IPackageRepository localRepository, IPackageRepository sourceRepository, bool allowPrereleaseVersions)
        {
            var dependencies = GetPackageDependencies(package, localRepository, sourceRepository, allowPrereleaseVersions);

            return from p in dependencies
                   where p.RequireLicenseAcceptance
                   select p;
        }

        private static IEnumerable<IPackage> GetPackageDependencies(IPackage package, IPackageRepository localRepository, IPackageRepository sourceRepository, bool allowPrereleaseVersions)
        {
            InstallWalker walker = new InstallWalker(
                localRepository: localRepository, 
                sourceRepository: sourceRepository, 
                logger: NullLogger.Instance,
                targetFramework: TargetFramework,
                ignoreDependencies: false, 
                allowPrereleaseVersions: allowPrereleaseVersions);
            IEnumerable<PackageOperation> operations = walker.ResolveOperations(package);

            return from operation in operations
                   where operation.Action == PackageAction.Install
                   select operation.Package;
        }

        internal static IQueryable<IPackage> GetPackages(IPackageRepository repository, string searchTerm, bool allowPrereleaseVersions)
        {
            return repository.Search(searchTerm: searchTerm, targetFrameworks: TargetFrameworks.Select(f => f.FullName), allowPrereleaseVersions: allowPrereleaseVersions);
        }

        internal static string GetWebRepositoryDirectory(string siteRoot)
        {
            return Path.Combine(siteRoot, "App_Data", "packages");
        }

        private class ErrorLogger : ILogger
        {
            private readonly IList<string> _errors = new List<string>();
            private readonly IWebMatrixHost _host;
            private bool _overwriteAll, _ignoreAll;

            internal static TaskScheduler GetCurrentTaskScheduler()
            {
                TaskScheduler scheduler = null;
                try
                {
                    // the scheduler should be the current Sync Context
                    scheduler = TaskScheduler.FromCurrentSynchronizationContext();
                }
                catch (InvalidOperationException)
                {
                    scheduler = TaskScheduler.Default;
                }

                return scheduler;
            }

            public ErrorLogger(IWebMatrixHost host)
            {
                if (host == null)
                {
                    throw new ArgumentNullException("host");
                }

                _host = host;
            }

            public IEnumerable<string> Errors
            {
                get { return _errors; }
            }

            public void Log(MessageLevel level, string message, params object[] args)
            {
                if (level == MessageLevel.Warning
                    || level == MessageLevel.Error)
                {
                    var formatted = String.Format(CultureInfo.CurrentCulture, message, args);
                    _errors.Add(formatted);

                    // we throw here to cancel the whole nuget install in case of warning or error
                    // otherwise it will "succeed" and will never try to update again
                    throw new InvalidOperationException(formatted);
                }
            }

            public void Clear()
            {
                _errors.Clear();
                _overwriteAll = _ignoreAll = false;
            }

            public FileConflictResolution ResolveFileConflict(string message)
            {
                if (_overwriteAll)
                {
                    return FileConflictResolution.OverwriteAll;
                }

                if (_ignoreAll)
                {
                    return FileConflictResolution.IgnoreAll;
                }

                FileConflictResolution resolution = Helpers.DispatchInvokeIfNecessary(() =>
                {
                    var window = new FileConflictDialog
                    {
                        Message = message
                    };

                    bool? result = _host.ShowDialog(null, window);

                    return (result == null || result == false) ? FileConflictResolution.Ignore : window.UserSelection;
                });

                _overwriteAll = (resolution == FileConflictResolution.OverwriteAll);
                _ignoreAll = (resolution == FileConflictResolution.IgnoreAll);

                return resolution;
            }
        }
    }
}
