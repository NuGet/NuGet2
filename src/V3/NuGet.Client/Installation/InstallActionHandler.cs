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
        public void Execute(NewPackageAction action, IExecutionContext context, CancellationToken cancelToken)
        {
            var nugetAware = action.Target.TryGetFeature<NuGetAwareProject>();
            if (nugetAware != null)
            {
                // TODO: this is a hack to get the supported frameworks. Since action.Package 
                // does not contain this info for now, we have to download the package to 
                // get this info.
                var packageContent = action.Package[Properties.PackageContent].ToString();
                var downloadUri = new Uri(packageContent);
                IPackage package;
                if (downloadUri.IsFile)
                {
                    package = new OptimizedZipPackage(packageContent);
                }
                else
                {
                    var packageCache = action.Target.GetRequiredFeature<IPackageCacheRepository>();
                    package = DownloadActionHandler.GetPackage(
                        packageCache,
                        action.PackageIdentity,
                        downloadUri);
                }
                var frameworks = package.GetSupportedFrameworks();
                var task = nugetAware.InstallPackage(
                    action.PackageIdentity,
                    frameworks,
                    context,
                    cancelToken);
                task.Wait();
            }
            else
            {
                // TODO: PMC - Write Disclamer Texts
                // TODO: Dialog & PMC - open Readme.txt

                // Get the package manager and project manager from the target
                var packageManager = action.Target.GetRequiredFeature<IPackageManager>();
                var projectManager = action.Target.GetRequiredFeature<IProjectManager>();

                // Get the package from the shared repository
                var package = packageManager.LocalRepository.FindPackage(
                    action.PackageIdentity.Id, CoreConverters.SafeToSemVer(action.PackageIdentity.Version));
                Debug.Assert(package != null); // The package had better be in the local repository!!

                // Ping the metrics service
                action.Source.RecordMetric(
                    action.ActionType,
                    action.PackageIdentity,
                    action.DependentPackage,
                    action.IsUpdate,
                    action.Target);

                // Add the package to the project
                projectManager.Logger = new ShimLogger(context);
                projectManager.Project.Logger = projectManager.Logger;
                projectManager.Execute(new PackageOperation(
                    package,
                    NuGet.PackageAction.Install));

                // Run install.ps1 if present
                ActionHandlerHelpers.ExecutePowerShellScriptIfPresent(
                    "install.ps1",
                    action.Target,
                    package,
                    packageManager.PathResolver.GetInstallPath(package),
                    context);
            }
        }

        public void Rollback(NewPackageAction action, IExecutionContext context)
        {
            // Just run the uninstall action to undo a install
            new UninstallActionHandler().Execute(action, context, CancellationToken.None);
        }
    }
}
