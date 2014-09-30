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
        public Task Execute(NewPackageAction action, InstallationTarget target, IExecutionLogger logger)
        {
            // Use the core-interop feature to install the package
            var interop = target.GetRequiredFeature<NuGetCoreInstallationFeature>();
            var package = interop.InstallPackage(action.PackageIdentity, action.Target);

            // Run install.ps1 if present
            ActionHandlerHelpers.ExecutePowerShellScriptIfPresent(
                "install.ps1",
                target,
                action.Target,
                package);

            // Not async yet :(
            return Task.FromResult(0);
        }

        public Task Rollback(NewPackageAction action, InstallationTarget target, IExecutionLogger logger)
        {
            // Just run the uninstall action to undo a install
            return new UninstallActionHandler().Execute(action, target, logger);
        }
    }
}
