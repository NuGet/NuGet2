using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.Diagnostics;

namespace NuGet.Client.Resolution
{
    /// <summary>
    /// Handles the Download package Action by downloading the specified package into the shared repository
    /// (packages folder) for the solution.
    /// </summary>
    public class DownloadActionHandler : IActionHandler
    {
        public Task Execute(PackageAction action, ExecutionContext context)
        {
            // Load the package
            var package = GetPackage(action, context);

            NuGetTraceSources.ActionExecutor.Verbose(
                "download/loadedpackage",
                "[{0}] Loaded package.",
                action.PackageName);

            // Now convert the action and use the V2 Execution logic since we
            // have a true V2 IPackage (either from the cache or an in-memory ZipPackage).
            context.PackageManager.Execute(new PackageOperation(
                package,
                NuGet.PackageAction.Install));

            // Not async yet :)
            return Task.FromResult(0);
        }

        public Task Rollback(PackageAction action, ExecutionContext context)
        {
            throw new NotImplementedException();
        }

        private IPackage GetPackage(PackageAction action, ExecutionContext context)
        {
            var packageSemVer = CoreConverters.SafeToSemVer(action.PackageName.Version);
            var packageName = CoreConverters.SafeToPackageName(action.PackageName);

            var package = context.PackageCache.FindPackage(action.PackageName.Id, packageSemVer);
            if (package != null)
            {
                NuGetTraceSources.ActionExecutor.Info(
                    "download/cachehit",
                    "[{0}] Download: Cache Hit!",
                    action.PackageName);
                // Success!
                return package;
            }

            // Failed to get it from the cache. Try to get the download URL.
            Uri downloadUri;
            try
            {
                downloadUri = action.Package.Value<Uri>(Properties.NupkgUrl);
            }
            catch (UriFormatException urifx)
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.DownloadActionHandler_InvalidDownloadUrl,
                    action.PackageName,
                    action.Package[Properties.NupkgUrl].ToString(),
                    urifx.Message));
            }
            if (downloadUri == null)
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.DownloadActionHandler_NoDownloadUrl,
                    action.PackageName));
            }
            
            // Try to download the package through the cache.
            bool success = context.PackageCache.InvokeOnPackage(
                action.PackageName.Id,
                packageSemVer,
                (targetStream) =>
                    context.PackageDownloader.DownloadPackage(
                        context.CreateHttpClient(downloadUri),
                        packageName,
                        targetStream));
            if (success)
            {
                NuGetTraceSources.ActionExecutor.Info(
                    "download/downloadedtocache",
                    "[{0}] Download: Downloaded to cache",
                    action.PackageName);

                // Try to get it from the cache again
                package = context.PackageCache.FindPackage(action.PackageName.Id, packageSemVer);
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
                    action.PackageName);

                using (var targetStream = new MemoryStream())
                {
                    context.PackageDownloader.DownloadPackage(
                        context.CreateHttpClient(downloadUri), 
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
