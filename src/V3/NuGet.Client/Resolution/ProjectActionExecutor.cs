using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.Interop;
using NuGet.Resolver;
using NewPackageAction = NuGet.Client.Resolution.PackageAction;
using OldPackageAction = NuGet.Resolver.PackageAction;

namespace NuGet.Client.Resolution
{
    public class ProjectActionExecutor
    {
        private IProjectManager _projectManager;
        private IPackageManager _packageManager;

        public ProjectActionExecutor(ProjectInstallationTarget target)
        {
            // Unwrap the objects and get the V2 APIs from them.
            _projectManager = target.ProjectManager;
            _packageManager = target.ProjectManager.PackageManager;
        }

        private OldPackageAction ConvertAction(NewPackageAction arg)
        {
            switch (arg.ActionType)
            {
            case NuGet.Client.Resolution.PackageActionType.Install:
                return new PackageProjectAction(
                    NuGet.Resolver.PackageActionType.Install,
                    new CoreInteropPackage(arg.Package),
                    _projectManager);
            case NuGet.Client.Resolution.PackageActionType.Uninstall:
                return new PackageProjectAction(
                    NuGet.Resolver.PackageActionType.Uninstall,
                    new CoreInteropPackage(arg.Package),
                    _projectManager);
            case NuGet.Client.Resolution.PackageActionType.Purge:
                return new PackageSolutionAction(
                    NuGet.Resolver.PackageActionType.DeleteFromPackagesFolder,
                    new CoreInteropPackage(arg.Package),
                    _packageManager);
            case NuGet.Client.Resolution.PackageActionType.Download:
                return new PackageSolutionAction(
                    NuGet.Resolver.PackageActionType.AddToPackagesFolder,
                    new CoreInteropPackage(arg.Package),
                    _packageManager);
            default:
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.ActionResolver_UnsupportedAction,
                    arg.ActionType.ToString()));
            }
        }

        public Task ExecuteActionsAsync(IEnumerable<NewPackageAction> actions)
        {
            var oldExecutor = new NuGet.Resolver.ActionExecutor();
            oldExecutor.Execute(actions.Select(ConvertAction));
            return Task.FromResult(0);
        }
    }
}
