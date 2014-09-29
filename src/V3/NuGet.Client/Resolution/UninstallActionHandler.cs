using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.Installation;

namespace NuGet.Client.Resolution
{
    public class UninstallActionHandler : IActionHandler
    {
        public Task Execute(PackageAction action, InstallationHost host, IExecutionLogger logger)
        {
            // Use the core-interop feature to execute the action
            var interop = host.GetRequiredFeature<CoreInteropFeature>();
            interop.UninstallPackage(action.PackageIdentity, action.Target);

            // Not async yet :)
            return Task.FromResult(0);
        }

        public Task Rollback(PackageAction action, InstallationHost host, IExecutionLogger logger)
        {
            // Just run the install action to undo a uninstall
            return new InstallActionHandler().Execute(action, host, logger);
        }
    }
}
