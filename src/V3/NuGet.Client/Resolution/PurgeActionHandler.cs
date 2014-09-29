using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.Installation;

namespace NuGet.Client.Resolution
{
    public class PurgeActionHandler : IActionHandler
    {
        public Task Execute(PackageAction action, InstallationHost host, IExecutionLogger logger)
        {
            // Use the core-interop feature to execute the action
            var interop = host.GetRequiredFeature<CoreInteropFeature>();
            return interop.PurgePackage(action.PackageIdentity, logger);
        }

        public Task Rollback(PackageAction action, InstallationHost host, IExecutionLogger logger)
        {
            // Just run the download action to undo a purge
            return new DownloadActionHandler().Execute(action, host, logger);
        }
    }
}
