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
        public void Execute(NewPackageAction action, IExecutionContext context, CancellationToken cancelToken)
        {
            var nugetAware = action.Target.TryGetFeature<NuGetAwareProject>();
            if (nugetAware != null)
            {
                var task = nugetAware.UninstallPackage(
                    action.PackageIdentity,
                    context,
                    cancelToken);
                task.Wait();
                return;
            }

            // Get the project manager
            var projectManager = action.Target.GetRequiredFeature<IProjectManager>();

            // Get the package out of the project manager
            var package = projectManager.LocalRepository.FindPackage(
                action.PackageIdentity.Id,
                CoreConverters.SafeToSemanticVersion(action.PackageIdentity.Version));
            Debug.Assert(package != null);

            // Add the package to the project
            projectManager.Logger = new ShimLogger(context);
            projectManager.Project.Logger = projectManager.Logger;
            projectManager.Execute(new PackageOperation(
                package,
                NuGet.PackageAction.Uninstall));

            // Run uninstall.ps1 if present
            ActionHandlerHelpers.ExecutePowerShellScriptIfPresent(
                "uninstall.ps1",
                action.Target,
                package,
                projectManager.PackageManager.PathResolver.GetInstallPath(package),
                context);
        }

        public void Rollback(NewPackageAction action, IExecutionContext context)
        {
            // Just run the install action to undo a uninstall
            new InstallActionHandler().Execute(action, context, CancellationToken.None);
        }
    }
}
