using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewPackageAction = NuGet.Client.Resolution.PackageAction;

namespace NuGet.Client.Installation
{
    public class UninstallActionHandler : IActionHandler
    {
        public Task Execute(NewPackageAction action, InstallationTarget target, IExecutionLogger logger)
        {
            // Use the core-interop feature to execute the action
            var interop = target.GetRequiredFeature<NuGetCoreInstallationFeature>();
            var package = interop.UninstallPackage(action.PackageIdentity, action.Target);

            // Run uninstall.ps1 if present
            ActionHandlerHelpers.ExecutePowerShellScriptIfPresent(
                "uninstall.ps1",
                target,
                action.Target,
                package);

            // Not async yet :)
            return Task.FromResult(0);
        }

        public Task Rollback(NewPackageAction action, InstallationTarget target, IExecutionLogger logger)
        {
            // Just run the install action to undo a uninstall
            return new InstallActionHandler().Execute(action, target, logger);
        }
    }
}
