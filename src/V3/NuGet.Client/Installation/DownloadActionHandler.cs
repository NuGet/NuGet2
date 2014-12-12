using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading;
using NuGet.Client.Diagnostics;
using NuGet.Resources;
using NewPackageAction = NuGet.Client.Resolution.PackageAction;

namespace NuGet.Client.Installation
{
    /// <summary>
    /// Handles the Download package Action by downloading the specified package into the shared repository
    /// (packages folder) for the solution.
    /// </summary>    
    public class DownloadActionHandler : IActionHandler
    {
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        public void Execute(NewPackageAction action, IExecutionContext context, CancellationToken cancelToken)
        {
            string downloadUriStr = action.Package[Properties.PackageContent].ToString();
            Uri downloadUri;
            if (String.IsNullOrEmpty(downloadUriStr))
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.DownloadActionHandler_NoDownloadUrl,
                    action.PackageIdentity));
            }
            else if (!Uri.TryCreate(downloadUriStr, UriKind.Absolute, out downloadUri))
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.DownloadActionHandler_InvalidDownloadUrl,
                    action.PackageIdentity,
                    downloadUriStr));
            }

            // Get required features from the target
            var packageManager = action.Target.GetRequiredFeature<IPackageManager>();
            var packageCache = action.Target.TryGetFeature<IPackageCacheRepository>();

            // Load the package
            IPackage package = null;
            if (downloadUri.IsFile)
            {
                // To keep feature backward-compatbility. The .nupkg file on disk may have a shorter name than semantic version.
                // For example: TestPackage 2.0.0.0 may be saved as TestPackage.2.0.nupkg on disk.
                SemanticVersion originalVersion = new SemanticVersion(action.PackageIdentity.Version.ToString());
                IEnumerable<SemanticVersion> possibleVersions = VersionUtility.GetPossibleVersions(originalVersion);
                foreach (SemanticVersion version in possibleVersions)
                {
                    // Get other alternative download path
                    string downloadPath = downloadUri.LocalPath.Replace(originalVersion.ToString(), version.ToString());
                    try
                    {
                        package = new OptimizedZipPackage(downloadPath);
                        break;
                    }
                    catch (ArgumentException)
                    {
                    }
                }

                // Verify the version of OptimizedZipPackage is expected.
                if (package == null || package.Version != originalVersion)
                {
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture, NuGetResources.FileDoesNotExit, downloadUri.LocalPath));
                }
            }
            else
            {
                package = GetPackage(packageCache, action.PackageIdentity, downloadUri);
            }

            NuGetTraceSources.ActionExecutor.Verbose(
                "download/loadedpackage",
                "[{0}] Loaded package.",
                action.PackageIdentity);

            // Now convert the action and use the V2 Execution logic since we
            // have a true V2 IPackage (either from the cache or an in-memory ZipPackage).
            packageManager.Logger = new ShimLogger(context);
            packageManager.Execute(new PackageOperation(
                package,
                NuGet.PackageAction.Install));

            // Run init.ps1 if present. Init is run WITHOUT specifying a target framework.
            ActionHandlerHelpers.ExecutePowerShellScriptIfPresent(
                "init.ps1",
                action.Target,
                package,
                packageManager.PathResolver.GetInstallPath(package),
                context);
        }

        public void Rollback(NewPackageAction action, IExecutionContext context)
        {
            // Just run the purge action to undo a download
            new PurgeActionHandler().Execute(action, context, CancellationToken.None);
        }

        internal static IPackage GetPackage(IPackageCacheRepository packageCache, PackageIdentity packageIdentity, Uri downloadUri)
        {
            var packageSemVer = CoreConverters.SafeToSemanticVersion(packageIdentity.Version);
            var packageName = CoreConverters.SafeToPackageName(packageIdentity);
            var downloader = new PackageDownloader();

            IPackage package = null;
            if (packageCache != null)
            {
                package = packageCache.FindPackage(packageName.Id, packageSemVer);
                if (package != null)
                {
                    NuGetTraceSources.ActionExecutor.Info(
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
                bool success = packageCache.InvokeOnPackage(
                    packageIdentity.Id,
                    packageSemVer,
                    (targetStream) =>
                        downloader.DownloadPackage(
                            new HttpClient(downloadUri),
                            packageName,
                            targetStream));
                if (success)
                {
                    NuGetTraceSources.ActionExecutor.Info(
                        "download/downloadedtocache",
                        "[{0}] Download: Downloaded to cache",
                        packageName);

                    // Try to get it from the cache again
                    package = packageCache.FindPackage(packageIdentity.Id, packageSemVer);
                }
            }

            // Either:
            //  1. We failed to load the package into the cache, which can happen when
            //       access to the %LocalAppData% directory is blocked,
            //       e.g. on Windows Azure Web Site build OR
            //  B. It was purged from the cache before it could be retrieved again.
            // Regardless, the cache isn't working for us, so download it in to memory.
            if (package == null)
            {
                NuGetTraceSources.ActionExecutor.Info(
                    "download/cachefailing",
                    "[{0}] Download: Cache isn't working. Downloading to RAM",
                    packageName);

                using (var targetStream = new MemoryStream())
                {
                    downloader.DownloadPackage(
                        new HttpClient(downloadUri),
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