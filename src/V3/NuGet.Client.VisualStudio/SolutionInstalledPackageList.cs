using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Client.Interop;
using NuGet.Versioning;

namespace NuGet.Client.VisualStudio
{
    /// <summary>
    /// A class that contains the list of packages installed in a solution.
    /// </summary>
    public class SolutionInstalledPackageList : InstalledPackagesList
    {
        private Dictionary<EnvDTE.Project, Dictionary<string, InstalledPackageReference>> _installedPackages;
        private Dictionary<string, InstalledPackageReference> _installedSolutionLevelPackages;

        private IPackageRepository _packagesFolderRepo;

        public SolutionInstalledPackageList(IPackageRepository packagesFolderRepo)
        {
            _installedPackages = new Dictionary<EnvDTE.Project, Dictionary<string, InstalledPackageReference>>();
            _installedSolutionLevelPackages = new Dictionary<string, InstalledPackageReference>(StringComparer.OrdinalIgnoreCase);
            _packagesFolderRepo = packagesFolderRepo;
        }

        /// <summary>
        /// Gets the list of projects in the solution
        /// </summary>
        public IEnumerable<EnvDTE.Project> Projects
        {
            get
            {
                return _installedPackages.Keys;
            }
        }

        /// <summary>
        /// Adds a project.
        /// </summary>
        /// <param name="project">The project to add.</param>
        public void AddProject(EnvDTE.Project project)
        {
            _installedPackages[project] = new Dictionary<string, InstalledPackageReference>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Adds a package that is installed in a project.
        /// </summary>
        /// <param name="project">The project where the package is installed. This project must have been
        /// added through <code>AddProject</code> before.
        /// <param name="package">The package to add.</param>
        public void Add(EnvDTE.Project project, InstalledPackageReference package)
        {
            _installedPackages[project][package.Identity.Id] = package;
        }

        /// <summary>
        /// Adds a solution level package.
        /// </summary>
        /// <param name="package">The package to add.</param>
        public void AddSolutionLevelPackage(InstalledPackageReference package)
        {
            _installedSolutionLevelPackages[package.Identity.Id] = package;
        }

        public InstalledPackageReference GetInstalledPackage(EnvDTE.Project project, string packageId)
        {
            Dictionary<string, InstalledPackageReference> d;
            if (!_installedPackages.TryGetValue(project, out d))
            {
                return null;
            }

            InstalledPackageReference package;
            if (!d.TryGetValue(packageId, out package))
            {
                return null;
            }

            return package;
        }

        /// <summary>
        /// Gets the version of installed package in a project.
        /// </summary>
        /// <param name="project">The project where the package is installed.</param>
        /// <param name="packageId">Id of the package.</param>
        /// <returns>The version of the installed package. Or null if the project does not have
        /// the package installed.</returns>
        public NuGetVersion GetInstalledVersion(EnvDTE.Project project, string packageId)
        {
            var installedPackage = GetInstalledPackage(project, packageId);
            if (installedPackage == null)
            {
                return null;
            }

            return installedPackage.Identity.Version;
        }

        public override System.Threading.Tasks.Task<IEnumerable<Newtonsoft.Json.Linq.JObject>> GetAllInstalledPackagesAndMetadata()
        {
            throw new NotImplementedException();
        }

        public override System.Threading.Tasks.Task<IEnumerable<Newtonsoft.Json.Linq.JObject>> Search(string searchTerm, int skip, int take, System.Threading.CancellationToken cancelToken)
        {
            return Task.FromResult(
                _packagesFolderRepo.Search(searchTerm, allowPrereleaseVersions: true)
                    .Skip(skip).Take(take).ToList()
                    .Select(p => PackageJsonLd.CreatePackageSearchResult(p, new[] { p })));
        }

        public override IEnumerable<InstalledPackageReference> GetInstalledPackageReferences()
        {
            throw new NotImplementedException();
        }

        public override InstalledPackageReference GetInstalledPackage(string packageId)
        {
            var oldestInstalledVersion = this.Projects
                .Select(project => this.GetInstalledPackage(project, packageId))
                .Where(package => package != null)
                .OrderBy(package => package.Identity.Version)
                .FirstOrDefault();
            return oldestInstalledVersion;
        }

        public override bool IsInstalled(string packageId, NuGetVersion packageVersion)
        {
            // !!!
            throw new NotImplementedException();
        }
    }
}