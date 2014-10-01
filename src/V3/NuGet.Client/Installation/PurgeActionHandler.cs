using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewPackageAction = NuGet.Client.Resolution.PackageAction;

namespace NuGet.Client.Installation
{
    public class PurgeActionHandler : IActionHandler
    {
        public Task Execute(NewPackageAction action, InstallationTarget target, IExecutionLogger logger)
        {
            // Use the core-interop feature to execute the action
            return Task.Run(() =>
            {
                var interop = target.GetRequiredFeature<NuGetCoreInstallationFeature>();
                interop.PurgePackage(action.PackageIdentity, logger);
            });
        }

        public Task Rollback(NewPackageAction action, InstallationTarget target, IExecutionLogger logger)
        {
            // Just run the download action to undo a purge
            return new DownloadActionHandler().Execute(action, target, logger);
        }
    }
}
