using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.Resolution
{
    public class PurgeActionHandler : IActionHandler
    {
        public Task Execute(PackageAction action, ExecutionContext context)
        {
            // Preconditions:
            Debug.Assert(!context.PackageManager.LocalRepository.IsReferenced(
                action.PackageName.Id,
                CoreConverters.SafeToSemVer(action.PackageName.Version)),
                "Expected the purge operation would only be executed AFTER the package was no longer referenced!");

            // Get the package out of the project manager
            var package = context.PackageManager.LocalRepository.FindPackage(
                action.PackageName.Id,
                CoreConverters.SafeToSemVer(action.PackageName.Version));
            Debug.Assert(package != null);
            
            // Purge the package from the local repository
            context.PackageManager.Execute(new PackageOperation(
                package,
                NuGet.PackageAction.Uninstall));

            // Not async yet :)
            return Task.FromResult(0);
        }

        public Task Rollback(PackageAction action, ExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }
}
