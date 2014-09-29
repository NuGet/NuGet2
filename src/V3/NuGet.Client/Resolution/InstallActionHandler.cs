using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.Installation;

namespace NuGet.Client.Resolution
{
    public class InstallActionHandler : IActionHandler
    {
        public Task Execute(PackageAction action, InstallationHost host, IExecutionLogger logger)
        {
            // Use the core-interop feature to execute the action
            var interop = host.GetRequiredFeature<CoreInteropFeature>();
            interop.InstallPackage(action.PackageIdentity, action.Target);

            // Not async yet :(
            return Task.FromResult(0);
        }

        public Task Rollback(PackageAction action, InstallationHost host, IExecutionLogger logger)
        {
            // Just run the uninstall action to undo a install
            return new UninstallActionHandler().Execute(action, host, logger);
        }
    }
}
