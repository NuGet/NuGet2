using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Resolver;

namespace NuGet.Client.Interop
{
    public class V2InteropActionResolver : IActionResolver
    {
        private IPackageRepository _sourceRepository;
        private IProjectManager _projectManager;
        public ILogger Logger { get; private set; }

        public V2InteropActionResolver(IPackageRepository sourceRepository, IProjectManager projectManager, ILogger logger)
        {
            _sourceRepository = sourceRepository;
            _projectManager = projectManager;
            Logger = logger;
        }

        public Task<IEnumerable<PackageActionDescription>> ResolveActions(PackageActionType action, PackageName target, ResolverContext context)
        {
            Logger.Log(MessageLevel.Info, "Resolving {0} {1}", action, target);

            // TODO: Figure out a way to avoid this? We need the IPackage but we never want to expose that to the
            // UI, so we have to look it up again.
            Logger.Log(MessageLevel.Debug, "Fetching package data for {0}", target);
            var package = _sourceRepository.FindPackage(target.Id, target.Version);

            var resolver = CreateResolver(context);

            resolver.AddOperation(
                ConvertAction(action),
                package,
                _projectManager);
            var resolved = resolver.ResolveActions();

            var ret = resolved.Select(a => (PackageActionDescription)new PackageActionDescriptionWrapper(a));

            // Add license acceptance actions
            var acceptLicenses = resolved
                .Where(a => a.ActionType == Resolver.PackageActionType.AddToPackagesFolder && a.Package.RequireLicenseAcceptance)
                .Select(a => new PackageActionDescription(
                    PackageActionType.AcceptLicense,
                    new PackageName(a.Package.Id, a.Package.Version),
                    a.Package.LicenseUrl.ToString()));

            return Task.FromResult(Enumerable.Concat(ret, acceptLicenses));
        }

        private ActionResolver CreateResolver(ResolverContext context)
        {
            var resolver = new ActionResolver()
            {
                AllowPrereleaseVersions = context.AllowPrerelease
            };
            if(context.DependencyBehavior == DependencyBehavior.Ignore) {
                resolver.IgnoreDependencies = true;
            } else {
                resolver.DependencyVersion = GetDependencyVersion(context.DependencyBehavior);
            }
            return resolver;
        }

        private DependencyVersion GetDependencyVersion(DependencyBehavior dependencyBehavior)
        {
            switch (dependencyBehavior)
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
                Debug.Fail("FATAL Error: Unsupported DependencyBehavior value: {0}", dependencyBehavior.ToString());
                throw new ArgumentException(
                    String.Format(CultureInfo.CurrentCulture, Strings.V2InteropActionResolver_UnsupportedDependencyBehavior, dependencyBehavior),
                    "dependencyBehavior");
            }
        }

        private PackageAction ConvertAction(PackageActionType action)
        {
            switch (action)
            {
            case PackageActionType.Install:
                return PackageAction.Install;
            case PackageActionType.Uninstall:
                return PackageAction.Uninstall;
            default:
                throw new ArgumentException(
                    String.Format(CultureInfo.CurrentCulture, Strings.V2InteropActionResolver_UnrecognizedAction, action.ToString()),
                    "action");
            }
        }
    }
}
