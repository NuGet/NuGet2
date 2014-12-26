using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Client.Diagnostics;
using NuGet.Client.Installation;
using NuGet.Client.Interop;
using NuGet.Client.ProjectSystem;
using NuGet.Resolver;
using NuGet.Versioning;
using OldResolver = NuGet.Resolver.ActionResolver;

namespace NuGet.Client.Resolution
{
    public class ActionResolver
    {
        private readonly SourceRepository _source;
        private readonly ResolutionContext _context;

        public IExecutionLogger Logger
        {
            get;
            set;
        }

        public ActionResolver(SourceRepository source, ResolutionContext context)
        {
            _source = source;
            _context = context;
        }

        public async Task<IEnumerable<PackageAction>> ResolveActionsAsync(
            PackageIdentity packageIdentity,
            PackageActionType operation,
            InstallationTarget target)
        {
            // Construct the Action Resolver
            var oldResolver = new OldResolver();
            if (Logger != null)
            {
                oldResolver.Logger = new ShimLogger(Logger);
            }

            // Apply context settings
            ApplyContext(oldResolver);

            var packageManager = target.GetRequiredFeature<IPackageManager>();
            var nullProjectManager = new NullProjectManager(new CoreInteropPackageManager(packageManager.LocalRepository, new CoreInteropSourceRepository(_source)));

            oldResolver.AddOperation(
                MapNewToOldActionType(operation),
                await CreateVirtualPackage(packageIdentity.Id, packageIdentity.Version),
                nullProjectManager);

            // Resolve actions!
            var actions = await Task.Factory.StartNew(() => oldResolver.ResolveActions());

            // Convert the actions
            var converted =
                from action in actions
                select new PackageAction(
                    MapOldToNewActionType(action.ActionType),
                    new PackageIdentity(
                        action.Package.Id,
                        new NuGetVersion(
                            action.Package.Version.Version,
                            action.Package.Version.SpecialVersion)),
                    UnwrapPackage(action.Package),
                    target,
                    _source,
                    packageIdentity);

            // Identify update operations so we can mark them as such.
            foreach (var group in converted.GroupBy(c => c.PackageIdentity.Id))
            {
                var installs = group.Where(p => p.ActionType == PackageActionType.Install).ToList();
                var uninstalls = group.Where(p => p.ActionType == PackageActionType.Uninstall).ToList();
                if (installs.Count > 0 && uninstalls.Count > 0)
                {
                    var maxInstall = installs.OrderByDescending(a => a.PackageIdentity.Version).First();
                    maxInstall.IsUpdate = true;
                }
            }

            return converted;
        }

        public async Task<IEnumerable<PackageAction>> ResolveActionsAsync(
            PackageIdentity packageIdentity,
            PackageActionType operation,
            IEnumerable<Project> targetedProjects,
            Solution solution)
        {
            // Construct the Action Resolver
            var resolver = new OldResolver();
            if (Logger != null)
            {
                resolver.Logger = new ShimLogger(Logger);
            }

            // Apply context settings
            ApplyContext(resolver);

            // Add the operation request(s)
            NuGetTraceSources.ActionResolver.Verbose("resolving", "Resolving {0} of {1} {2}", operation.ToString(), packageIdentity.Id, packageIdentity.Version.ToNormalizedString());
            foreach (var project in targetedProjects)
            {
                resolver.AddOperation(
                    MapNewToOldActionType(operation),
                    await CreateVirtualPackage(packageIdentity.Id, packageIdentity.Version),
                    new CoreInteropProjectManager(project, _source));
            }

            // Resolve actions!
            var actions = await Task.Factory.StartNew(() => resolver.ResolveActions());

            // Convert the actions
            var converted = 
                from action in actions
                let projectAction = action as PackageProjectAction
                select new PackageAction(
                    MapOldToNewActionType(action.ActionType),
                    new PackageIdentity(
                        action.Package.Id,
                        new NuGetVersion(
                            action.Package.Version.Version,
                            action.Package.Version.SpecialVersion)),
                    UnwrapPackage(action.Package),
                    (projectAction != null ?
                        FindProject(targetedProjects, projectAction.ProjectManager.Project.ProjectName) :
                        (InstallationTarget)solution),
                    _source,
                    packageIdentity);

            // Identify update operations so we can mark them as such.
            foreach (var group in converted.GroupBy(c => c.PackageIdentity.Id))
            {
                var installs = group.Where(p => p.ActionType == PackageActionType.Install).ToList();
                var uninstalls = group.Where(p => p.ActionType == PackageActionType.Uninstall).ToList();
                if (installs.Count > 0 && uninstalls.Count > 0)
                {
                    var maxInstall = installs.OrderByDescending(a => a.PackageIdentity.Version).First();
                    maxInstall.IsUpdate = true;
                }
            }

            return converted;
        }

        private static Project FindProject(IEnumerable<Project> targets, string projectName)
        {
            return targets.FirstOrDefault(p => String.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase));
        }

        private static JObject UnwrapPackage(IPackage package)
        {
            CoreInteropPackage interopPackage = package as CoreInteropPackage;
            if (interopPackage == null)
            {
                return PackageJsonLd.CreatePackage(package);
            }
            return interopPackage.Json;
        }

        private async Task<IPackage> CreateVirtualPackage(string id, NuGetVersion version)
        {
            // Load JSON from source
            var package = await _source.GetPackageMetadata(id, version);

            return new CoreInteropPackage(package);
        }

        private void ApplyContext(OldResolver resolver)
        {
            resolver.AllowPrereleaseVersions = _context.AllowPrerelease;
            resolver.ForceRemove = _context.ForceRemove;
            resolver.RemoveDependencies = _context.RemoveDependencies;

            if (_context.DependencyBehavior == DependencyBehavior.Ignore)
            {
                resolver.IgnoreDependencies = true;
            }
            else
            {
                resolver.DependencyVersion = MapDependencyBehavior(_context.DependencyBehavior);
            }
        }

        private static DependencyVersion MapDependencyBehavior(DependencyBehavior behavior)
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

        private static PackageActionType MapOldToNewActionType(Resolver.PackageActionType packageActionType)
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

        private static NuGet.PackageAction MapNewToOldActionType(PackageActionType operation)
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