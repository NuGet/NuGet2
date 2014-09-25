using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.Resolution
{
    public class UninstallActionHandler : IActionHandler
    {
        public Task Execute(PackageAction action, ExecutionContext context, ILogger logger)
        {
            // Get the package out of the project manager
            var package = context.ProjectManager.LocalRepository.FindPackage(
                action.PackageName.Id,
                CoreConverters.SafeToSemVer(action.PackageName.Version));
            Debug.Assert(package != null);

            // Add the package to the project
            context.ProjectManager.Execute(new PackageOperation(
                package,
                NuGet.PackageAction.Uninstall));

            // Not async yet :)
            return Task.FromResult(0);
        }

        public Task Rollback(PackageAction action, ExecutionContext context, ILogger logger)
        {
            // Just run the install action to undo a uninstall
            return new InstallActionHandler().Execute(action, context, logger);
        }
    }
}
