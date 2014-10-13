using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Client.Diagnostics;
using NewPackageAction = NuGet.Client.Resolution.PackageAction;

namespace NuGet.Client.Installation
{
    public class InstallActionHandler : IActionHandler
    {
        public Task Execute(NewPackageAction action, IExecutionLogger logger, CancellationToken cancelToken)
        {
            var nugetAware = action.Target.TryGetFeature<NuGetAwareProject>();
            if (nugetAware != null)
            {
                // TODO: this is a hack to get the supported frameworks. Since action.Package 
                // does not contain this info for now, we have to download the package to 
                // get this info.
                var downloadUri = action.Package.Value<Uri>(Properties.PackageContent);
                var packageCache = action.Target.GetRequiredFeature<IPackageCacheRepository>();
                var package = DownloadActionHandler.GetPackage(
                    packageCache,
                    action.PackageIdentity,
                    downloadUri);
                var frameworks = package.GetSupportedFrameworks();
                
                return nugetAware.InstallPackage(
                    action.PackageIdentity,
                    frameworks,
                    logger,
                    cancelToken);
            }

            return Task.Run(() =>
            {
                // Get the package manager and project manager from the target
                var packageManager = action.Target.GetRequiredFeature<IPackageManager>();
                var projectManager = action.Target.GetRequiredFeature<IProjectManager>();

                // Get the package from the shared repository
                var package = packageManager.LocalRepository.FindPackage(
                    action.PackageIdentity.Id, CoreConverters.SafeToSemVer(action.PackageIdentity.Version));
                Debug.Assert(package != null); // The package had better be in the local repository!!

                // Add the package to the project
                projectManager.Logger = new ShimLogger(logger);
                projectManager.Execute(new PackageOperation(
                    package,
                    NuGet.PackageAction.Install));

                // Run install.ps1 if present
                ActionHandlerHelpers.ExecutePowerShellScriptIfPresent(
                    "install.ps1",
                    action.Target,
                    package,
                    packageManager.PathResolver.GetInstallPath(package),
                    logger);
            });
        }

        public Task Rollback(NewPackageAction action, IExecutionLogger logger)
        {
            // Just run the uninstall action to undo a install
            return new UninstallActionHandler().Execute(action, logger, CancellationToken.None);
        }
    }
}
