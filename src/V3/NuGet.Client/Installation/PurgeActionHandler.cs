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
    public class PurgeActionHandler : IActionHandler
    {
        public Task Execute(NewPackageAction action, IExecutionLogger logger, CancellationToken cancelToken)
        {
            // Use the core-interop feature to execute the action
            return Task.Run(() =>
            {
                var packageManager = action.Target.GetRequiredFeature<IPackageManager>();

                // Preconditions:
                Debug.Assert(!packageManager.LocalRepository.IsReferenced(
                    action.PackageIdentity.Id,
                    CoreConverters.SafeToSemVer(action.PackageIdentity.Version)),
                    "Expected the purge operation would only be executed AFTER the package was no longer referenced!");

                // Get the package out of the project manager
                var package = packageManager.LocalRepository.FindPackage(
                    action.PackageIdentity.Id,
                    CoreConverters.SafeToSemVer(action.PackageIdentity.Version));
                Debug.Assert(package != null);

                // Purge the package from the local repository
                packageManager.Logger = new ShimLogger(logger);
                packageManager.Execute(new PackageOperation(
                    package,
                    NuGet.PackageAction.Uninstall));
            });
        }

        public Task Rollback(NewPackageAction action, IExecutionLogger logger)
        {
            // Just run the download action to undo a purge
            return new DownloadActionHandler().Execute(action, logger, CancellationToken.None);
        }
    }
}
