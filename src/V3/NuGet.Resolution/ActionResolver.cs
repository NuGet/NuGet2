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
using NuGet.Versioning;
//using OldResolver = NuGet.Resolver.ActionResolver;
using NewResolver = NuGet.Resolution.Resolver;
using NewPackageAction = NuGet.Client.Resolution.PackageAction;
using NuGet.Client;
using NuGet.Client.Resolution;

namespace NuGet.Resolution
{
    public class ActionResolver
    {
        private readonly SourceRepository _source;
        private readonly ResolutionContext _context;

        public ActionResolver(SourceRepository source, ResolutionContext context)
        {
            _source = source;
            _context = context;
        }

        public async Task<IEnumerable<NewPackageAction>> ResolveActionsAsync(
            PackageIdentity packageIdentity,
            PackageActionType operation,
            IEnumerable<Project> targetedProjects,
            Solution solution)
        {
            // Construct the Action Resolver
            var resolver = new NewResolver(operation, packageIdentity, _source);

            // Apply context settings
            ApplyContext(resolver);

            // Add the operation request(s)
            NuGetTraceSources.ActionResolver.Verbose("resolving", "Resolving {0} of {1} {2}", operation.ToString(), packageIdentity.Id, packageIdentity.Version.ToNormalizedString());
            foreach (var project in targetedProjects)
            {
                resolver.AddOperationTarget(project);
            }

            // Resolve actions!
            var actions = await resolver.ResolveActionsAsync();
            
            // Identify update operations so we can mark them as such.
            foreach (var group in actions.GroupBy(c => c.PackageIdentity.Id))
            {
                var installs = group.Where(p => p.ActionType == PackageActionType.Install).ToList();
                var uninstalls = group.Where(p => p.ActionType == PackageActionType.Uninstall).ToList();
                if (installs.Count > 0 && uninstalls.Count > 0)
                {
                    var maxInstall = installs.OrderByDescending(a => a.PackageIdentity.Version).First();
                    maxInstall.IsUpdate = true;
                }
            }

            return actions;
        }

        private async Task<IPackage> CreateVirtualPackage(string id, NuGetVersion version)
        {
            // Load JSON from source
            var package = await _source.GetPackageMetadata(id, version);

            return new CoreInteropPackage(package);
        }

        private void ApplyContext(NewResolver resolver)
        {
            resolver.AllowPrereleaseVersions = _context.AllowPrerelease;

            if (_context.DependencyBehavior == DependencyBehavior.Ignore)
            {
                resolver.IgnoreDependencies = true;
            }
            else
            {
                resolver.DependencyVersion = _context.DependencyBehavior;
            }
        }
    }
}