using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if VS14
using Microsoft.VisualStudio.ProjectSystem.Interop;
#endif

namespace NuGet.Client.Resolution
{
    public class InstallActionHandler : IActionHandler
    {
        public Task Execute(PackageAction action, ExecutionContext context, IExecutionLogger logger)
        {
            var projectManager = context.GetProjectManager(action.Target);

#if VS14
            var nugetAwareProject = projectManager.Project as INuGetPackageManager;
            if (nugetAwareProject != null)
            {
                using (var cts = new CancellationTokenSource())
                {
                    var args = new Dictionary<string, object>();
                    var task = nugetAwareProject.InstallPackageAsync(
                        new NuGetPackageMoniker
                        {
                            Id = action.PackageName.Id,
                            Version = action.PackageName.Version.ToString()
                        },
                        args,
                        logger: null,
                        progress: null,
                        cancellationToken: cts.Token);
                    return task;
                }
            }
#endif

            // Get the package from the shared repository
            var package = context.PackageManager.LocalRepository.FindPackage(
                action.PackageName.Id, CoreConverters.SafeToSemVer(action.PackageName.Version));
            Debug.Assert(package != null); // The package had better be in the local repository!!

            // Add the package to the project
            projectManager.Execute(new PackageOperation(
                package,
                NuGet.PackageAction.Install));

            // Not async yet :)
            return Task.FromResult(0);
        }

        public Task Rollback(PackageAction action, ExecutionContext context, IExecutionLogger logger)
        {
            // Just run the uninstall action to undo a install
            return new UninstallActionHandler().Execute(action, context, logger);
        }
    }
}
