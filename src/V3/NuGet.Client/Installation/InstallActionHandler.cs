using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.Diagnostics;
using NewPackageAction = NuGet.Client.Resolution.PackageAction;

namespace NuGet.Client.Installation
{
    public class InstallActionHandler : IActionHandler
    {
        public Task Execute(NewPackageAction action, IExecutionLogger logger)
        {
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
            return new UninstallActionHandler().Execute(action, logger);
        }
    }
}
