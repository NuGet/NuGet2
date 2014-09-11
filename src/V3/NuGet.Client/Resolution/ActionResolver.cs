using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.Interop;
using NuGet.Resolver;
using NuGet.Versioning;
using OldResolver = NuGet.Resolver.ActionResolver;

namespace NuGet.Client.Resolution
{
    public class ActionResolver
    {
        private readonly SourceRepository _source;
        private readonly InstallationTarget _target;
        private readonly ResolutionContext _context;

        public ActionResolver(SourceRepository source, InstallationTarget target, ResolutionContext context)
        {
            _source = source;
            _target = target;
            _context = context;
        }

        public async Task<IEnumerable<PackageAction>> ResolveActionsAsync(
            string id,
            NuGetVersion version,
            PackageActionType operation)
        {
            // Construct the Action Resolver
            var resolver = new OldResolver();

            // Apply context settings
            ApplyContext(resolver);

            // Add the operation request
            resolver.AddOperation(
                MapNewToOldActionType(operation),
                CreateVirtualPackage(id, version),
                new CoreInteropProjectManager(_target, _source));

            // Resolve actions!
            var actions = await Task.Factory.StartNew(() => resolver.ResolveActions());
            
            // Convert the actions and return them
            return from action in actions
                   let projectAction = action as PackageProjectAction
                   select new PackageAction(
                       MapOldToNewActionType(action.ActionType),
                       new PackageIdentity(
                           action.Package.Id,
                           new NuGetVersion(
                                action.Package.Version.Version,
                                action.Package.Version.SpecialVersion)),
                       PackageJsonLd.CreatePackage(action.Package),
                       (projectAction != null ?
                            projectAction.ProjectManager.Project.ProjectName :
                            String.Empty));

        }

        private IPackage CreateVirtualPackage(string id, NuGetVersion version)
        {
            return new CoreInteropPackage(id, version);
        }

        private void ApplyContext(OldResolver resolver)
        {
            resolver.AllowPrereleaseVersions = _context.AllowPrerelease;
            
            if (_context.DependencyBehavior == DependencyBehavior.Ignore)
            {
                resolver.IgnoreDependencies = true;
            }
            else
            {
                resolver.DependencyVersion = MapDependencyBehavior(_context.DependencyBehavior);
            }
        }

        private DependencyVersion MapDependencyBehavior(DependencyBehavior behavior)
        {
            // Ignore is checked before calling this.
            Debug.Assert(behavior != DependencyBehavior.Ignore);

            switch (behavior)
            {
            case DependencyBehavior.Lowest:
                return DependencyVersion.Lowest;
            case DependencyBehavior.HighestPatch:
                return DependencyVersion.HighestPatch;
            case DependencyBehavior.HighestMinor:
                return DependencyVersion.HighestMinor;
            case DependencyBehavior.Highest:
                return DependencyVersion.Highest;
            default:
                throw new ArgumentException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.ActionResolver_UnsupportedDependencyBehavior,
                        behavior),
                    "behavior");
            }
        }

        private PackageActionType MapOldToNewActionType(Resolver.PackageActionType packageActionType)
        {
            switch (packageActionType)
            {
            case NuGet.Resolver.PackageActionType.Install:
                return PackageActionType.Install;
            case NuGet.Resolver.PackageActionType.Uninstall:
                return PackageActionType.Uninstall;
            case NuGet.Resolver.PackageActionType.AddToPackagesFolder:
                return PackageActionType.Download;
            case NuGet.Resolver.PackageActionType.DeleteFromPackagesFolder:
                return PackageActionType.Purge;
            default:
                throw new ArgumentException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.ActionResolver_UnsupportedAction,
                        packageActionType),
                    "packageActionType");
            }
        }

        private NuGet.PackageAction MapNewToOldActionType(PackageActionType operation)
        {
            switch (operation)
            {
            case PackageActionType.Install:
                return NuGet.PackageAction.Install;
            case PackageActionType.Uninstall:
                return NuGet.PackageAction.Uninstall;
            default:
                throw new ArgumentException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.ActionResolver_UnsupportedAction,
                        operation),
                    "operation");
            }
        }
    }
}
