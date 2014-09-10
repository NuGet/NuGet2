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

        public Task<IEnumerable<PackageAction>> ResolveActionsAsync(
            string id,
            NuGetVersion version,
            PackageActionType operation)
        {
            //// Construct the Action Resolver
            //var resolver = new OldResolver();

            //// Apply context settings
            //ApplyContext(resolver);

            //// Add the operation request
            //resolver.AddOperation(
            //    MapAction(operation),
            //    CreateVirtualPackage(id, version));

            //// Resolve actions!
            //var actions = 
            return Task.FromResult(Enumerable.Empty<PackageAction>());
        }

        private IPackage CreateVirtualPackage(string id, NuGetVersion version)
        {
            return new V3InteropPackage(id, version);
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
                throw new InvalidOperationException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.ActionResolver_UnsupportedDependencyBehavior,
                        behavior));
            }
        }

        private NuGet.PackageAction MapAction(PackageActionType operation)
        {
            switch (operation)
            {
            case PackageActionType.Install:
                return NuGet.PackageAction.Install;
            case PackageActionType.Uninstall:
                return NuGet.PackageAction.Uninstall;
            default:
                throw new InvalidOperationException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.ActionResolver_UnsupportedAction,
                        operation));
            }
        }
    }
}
