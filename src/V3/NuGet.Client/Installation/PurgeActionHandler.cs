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
        public void Execute(NewPackageAction action, IExecutionContext context, CancellationToken cancelToken)
        {
            // Use the core-interop feature to execute the action
            var packageManager = action.Target.GetRequiredFeature<IPackageManager>();

            // Preconditions:
            Debug.Assert(!packageManager.LocalRepository.IsReferenced(
                action.PackageIdentity.Id,
                CoreConverters.SafeToSemanticVersion(action.PackageIdentity.Version)),
                "Expected the purge operation would only be executed AFTER the package was no longer referenced!");

            // Get the package out of the project manager
            var package = packageManager.LocalRepository.FindPackage(
                action.PackageIdentity.Id,
                CoreConverters.SafeToSemanticVersion(action.PackageIdentity.Version));
            Debug.Assert(package != null);

            // Purge the package from the local repository
            packageManager.Logger = new ShimLogger(context);
            packageManager.Execute(new PackageOperation(
                package,
                NuGet.PackageAction.Uninstall));
        }

        public void Rollback(NewPackageAction action, IExecutionContext context)
        {
            // Just run the download action to undo a purge
            new DownloadActionHandler().Execute(action, context, CancellationToken.None);
        }
    }
}
