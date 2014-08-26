using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using NuGet.Resolver;
using NuGet.VisualStudio.ClientV3;

namespace NuGet.VisualStudio
{
    public class ProjectPackageManagerSession : VsPackageManagerSession
    {
        private Project _project;
        private IPackageRepository _localRepo;

        public override string Name
        {
            get { return _project.Name; }
        }

        public ProjectPackageManagerSession(Project project, IVsRepositoryManager repositoryManager, IPackageRepository localRepo)
            : base(repositoryManager)
        {
            _project = project;
            _localRepo = localRepo;
        }

        public static ProjectPackageManagerSession Create(Project project)
        {
            var packManFactory = ServiceLocator.GetInstance<IVsPackageManagerFactory>();
            var packMan = packManFactory.CreatePackageManagerToManageInstalledPackages();
            var projMan = packMan.GetProjectManager(project); // TODO: Disable aggregate source!

            return new ProjectPackageManagerSession(
                project,
                ServiceLocator.GetInstance<IVsRepositoryManager>(),
                projMan.LocalRepository);
        }

        public override SemanticVersion GetInstalledVersion(string id)
        {
            var package = _localRepo.FindPackage(id);
            if (package != null)
            {
                return package.Version;
            }
            return null;
        }

        public override IEnumerable<FrameworkName> GetSupportedFrameworks()
        {
            yield return _project.GetTargetFrameworkName();
        }

        public override IEnumerable<IPackageName> GetInstalledPackages()
        {
            throw new NotImplementedException();
        }

        public override bool IsInstalled(string id, SemanticVersion version)
        {
            return _localRepo.Exists(id, version);
        }

        public override async Task<IEnumerable<PackageAction>> ResolveActionsAsync(
            PackageAction action, string packageId, SemanticVersion packageVersion)
        {
            var d = (DependencyBehavior)_dependencyBehavior.SelectedItem;
            var resolver = new ActionResolver()
            {
                IgnoreDependencies = !d.DependencyVersion.HasValue,
                AllowPrereleaseVersions = false
            };

            if (d.DependencyVersion.HasValue)
            {
                resolver.DependencyVersion = d.DependencyVersion.Value;
            }

            var package = ((PackageDetailControlModel)DataContext).Package.Package;

            var actions = await Task.Factory.StartNew(
                () =>
                {
                    resolver.AddOperation(action, package, projectManager);
                    return resolver.ResolveActions();
                });
            return actions;
        }

        public override Task ExecuteActions(IEnumerable<ClientV3.PackageAction> actions)
        {
            //ActionExecutor executor = new ActionExecutor();
            //executor.Execute(actions);
            throw new NotImplementedException();
        }
    }
}
