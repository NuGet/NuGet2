using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.Diagnostics;
using NuGet.Client.Resolution;

namespace NuGet.Client.Installation
{
    /// <summary>
    /// Downloads a package from a V3 SourceRepository into a V2 IPackageRepository
    /// </summary>
    public class CoreInteropFeature
    {
        private readonly IPackageCacheRepository _packageCache;
        private readonly PackageDownloader _downloader;
        private readonly Func<Uri, IHttpClient> _httpClientFactory;
        private readonly IPackageManager _packageManager;
        private readonly Func<TargetProject, IProjectManager> _projectManagerFetcher;

        public CoreInteropFeature(
            IPackageManager packageManager,
            Func<TargetProject, IProjectManager> projectManagerFetcher,
            IPackageCacheRepository packageCache,
            PackageDownloader downloader,
            Func<Uri, IHttpClient> httpClientFactory)
        {
            _packageCache = packageCache;
            _downloader = downloader;
            _httpClientFactory = httpClientFactory;
            _packageManager = packageManager;
            _projectManagerFetcher = projectManagerFetcher;
        }

        public Task DownloadPackage(PackageIdentity packageIdentity, Uri downloadUri)
        {
            return Task.Factory.StartNew(() =>
            {
                // Load the package
                var package = GetPackage(packageIdentity, downloadUri);

                NuGetTraceSources.ActionExecutor.Verbose(
                    "download/loadedpackage",
                    "[{0}] Loaded package.",
                    packageIdentity);

                // Now convert the action and use the V2 Execution logic since we
                // have a true V2 IPackage (either from the cache or an in-memory ZipPackage).
                _packageManager.Execute(new PackageOperation(
                    package,
                    NuGet.PackageAction.Install));
            });
        }

        public Task PurgePackage(PackageIdentity packageIdentity, IExecutionLogger logger)
        {
            // Preconditions:
            Debug.Assert(!_packageManager.LocalRepository.IsReferenced(
                packageIdentity.Id,
                CoreConverters.SafeToSemVer(packageIdentity.Version)),
                "Expected the purge operation would only be executed AFTER the package was no longer referenced!");

            // Get the package out of the project manager
            var package = _packageManager.LocalRepository.FindPackage(
                packageIdentity.Id,
                CoreConverters.SafeToSemVer(packageIdentity.Version));
            Debug.Assert(package != null);

            // Purge the package from the local repository
            return Task.Factory.StartNew(() =>
            {
                // Purge the package from the local repository
                _packageManager.Logger = new ShimLogger(logger);
                _packageManager.Execute(new PackageOperation(
                    package,
                    NuGet.PackageAction.Uninstall));
            });
        }

        public void InstallPackage(PackageIdentity packageIdentity, TargetProject project)
        {
            // Get the package from the shared repository
            var package = _packageManager.LocalRepository.FindPackage(
                packageIdentity.Id, CoreConverters.SafeToSemVer(packageIdentity.Version));
            Debug.Assert(package != null); // The package had better be in the local repository!!

            var projectManager = _projectManagerFetcher(project);

            // Add the package to the project
            projectManager.Execute(new PackageOperation(
                package,
                NuGet.PackageAction.Install));
        }

        public void UninstallPackage(PackageIdentity packageIdentity, TargetProject project)
        {
            // Get the package out of the project manager
            var projectManager = _projectManagerFetcher(project);
            var package = projectManager.LocalRepository.FindPackage(
                packageIdentity.Id,
                CoreConverters.SafeToSemVer(packageIdentity.Version));
            Debug.Assert(package != null);

            // Add the package to the project
            projectManager.Execute(new PackageOperation(
                package,
                NuGet.PackageAction.Uninstall));
        }

        private IPackage GetPackage(PackageIdentity packageIdentity, Uri downloadUri)
        {
            var packageSemVer = CoreConverters.SafeToSemVer(packageIdentity.Version);
            var packageName = CoreConverters.SafeToPackageName(packageIdentity);

            var package = _packageCache.FindPackage(packageName.Id, packageSemVer);
            if (package != null)
            {
                NuGetTraceSources.V2InstallationFeatures.Info(
                    "download/cachehit",
                    "[{0}] Download: Cache Hit!",
                    packageIdentity);
                // Success!
                return package;
            }

            if (downloadUri == null)
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.DownloadActionHandler_NoDownloadUrl,
                    packageIdentity));
            }

            // Try to download the package through the cache.
            bool success = _packageCache.InvokeOnPackage(
                packageIdentity.Id,
                packageSemVer,
                (targetStream) =>
                    _downloader.DownloadPackage(
                        _httpClientFactory(downloadUri),
                        packageName,
                        targetStream));
            if (success)
            {
                NuGetTraceSources.V2InstallationFeatures.Info(
                    "download/downloadedtocache",
                    "[{0}] Download: Downloaded to cache",
                    packageName);

                // Try to get it from the cache again
                package = _packageCache.FindPackage(packageIdentity.Id, packageSemVer);
            }

            // Either:
            //  1. We failed to load the package into the cache, which can happen when 
            //       access to the %LocalAppData% directory is blocked, 
            //       e.g. on Windows Azure Web Site build OR
            //  B. It was purged from the cache before it could be retrieved again.
            // Regardless, the cache isn't working for us, so download it in to memory.
            if (package == null)
            {
                NuGetTraceSources.V2InstallationFeatures.Info(
                    "download/cachefailing",
                    "[{0}] Download: Cache isn't working. Downloading to RAM",
                    packageName);

                using (var targetStream = new MemoryStream())
                {
                    _downloader.DownloadPackage(
                        _httpClientFactory(downloadUri),
                        packageName,
                        targetStream);

                    targetStream.Seek(0, SeekOrigin.Begin);
                    package = new ZipPackage(targetStream);
                }
            }
            return package;
        }
    }
}
