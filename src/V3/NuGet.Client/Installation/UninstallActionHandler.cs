using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewPackageAction = NuGet.Client.Resolution.PackageAction;

namespace NuGet.Client.Installation
{
    public class UninstallActionHandler : IActionHandler
    {
        public Task Execute(NewPackageAction action, IExecutionLogger logger, CancellationToken cancelToken)
        {
            var nugetAware = action.Target.TryGetFeature<NuGetAwareProject>();
            if (nugetAware != null)
            {
                return nugetAware.UninstallPackage(
                    action.PackageIdentity,
                    logger,
                    cancelToken);
            }

            return Task.Run(() =>
            {
                // Get the project manager
                var projectManager = action.Target.GetRequiredFeature<IProjectManager>();
                
                // Get the package out of the project manager
                var package = projectManager.LocalRepository.FindPackage(
                    action.PackageIdentity.Id,
                    CoreConverters.SafeToSemVer(action.PackageIdentity.Version));
                Debug.Assert(package != null);

                // Add the package to the project
                projectManager.Logger = new ShimLogger(logger);
                projectManager.Execute(new PackageOperation(
                    package,
                    NuGet.PackageAction.Uninstall));

                // Run uninstall.ps1 if present
                ActionHandlerHelpers.ExecutePowerShellScriptIfPresent(
                    "uninstall.ps1",
                    action.Target,
                    package,
                    projectManager.PackageManager.PathResolver.GetInstallPath(package),
                    logger);
            });
        }

        public Task Rollback(NewPackageAction action, IExecutionLogger logger)
        {
            // Just run the install action to undo a uninstall
            return new InstallActionHandler().Execute(action, logger, CancellationToken.None);
        }
    }
}
