using NuGet.Client;
using NuGet.Client.ProjectSystem;
using NuGet.Client.Resolution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewPackageAction = NuGet.Client.Resolution.PackageAction;

namespace NuGet.Resolution
{
    internal class Resolver
    {
        public bool IgnoreDependencies { get; set; }

        public DependencyBehavior DependencyVersion { get; set; }

        public bool AllowPrereleaseVersions { get; set; }

        public void AddOperation(PackageActionType operation, IPackage package, IProjectManager projectManager)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<NewPackageAction> ResolveActions()
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<NewPackageAction>> ResolveActionsAsync(
            PackageIdentity packageIdentity,
            PackageActionType operation,
            IEnumerable<Project> targetedProjects,
            Solution solution)
        {
            return await Task.Factory.StartNew(() => Enumerable.Empty<NewPackageAction>());
        }

    }
}
