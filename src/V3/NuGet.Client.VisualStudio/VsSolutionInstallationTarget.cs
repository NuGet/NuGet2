using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NuGet.VisualStudio;

namespace NuGet.Client.VisualStudio
{
    public class VsSolutionInstallationTarget : InstallationTarget
    {
        private EnvDTE.Solution _solution;
        private string _name;
        private SolutionInstalledPackageList _installedPackageList;
        private IVsPackageManager _packageManager;

        public VsSolutionInstallationTarget(EnvDTE.Solution solution)
        {
            _solution = solution;
            _name = string.Format(
                CultureInfo.CurrentCulture,
                Strings.Lable_Solution,
                _solution.GetName());

            _packageManager = ServiceLocator.GetInstance<IVsPackageManagerFactory>()
                .CreatePackageManagerToManageInstalledPackages();
            CreateInstalledPackages();
        }

        public void CreateInstalledPackages()
        {
            _installedPackageList = new SolutionInstalledPackageList(_packageManager.LocalRepository);
            foreach (EnvDTE.Project project in _solution.Projects)
            {
                _installedPackageList.AddProject(project);

                foreach (var package in GetInstalledPackages(project))
                {
                    _installedPackageList.Add(
                        project,
                        NuGet.Client.CoreConverters.SafeToInstalledPackageReference(package));
                }
            }
        }

        public override bool IsSolutionOpen
        {
            get
            {
                return _solution.IsOpen;
            }
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// Gets the list of packages installed in the project.
        /// </summary>
        /// <param name="project">The project to check.</param>
        /// <returns>The list of packages installed in the project.</returns>
        private IEnumerable<PackageReference> GetInstalledPackages(EnvDTE.Project project)
        {
            var projectManager = _packageManager.GetProjectManager(project);
            var repo = (PackageReferenceRepository)projectManager.LocalRepository;
            return repo.GetPackageReferences();
        }

        public override InstalledPackagesList Installed
        {
            get
            {
                return _installedPackageList;
            }
        }

        public override Task<IEnumerable<InstalledPackagesList>> GetInstalledPackagesInAllProjects()
        {
            throw new NotImplementedException();
        }

        public override IProjectSystem ProjectSystem
        {
            get { throw new NotImplementedException(); }
        }

        public override IEnumerable<System.Runtime.Versioning.FrameworkName> GetSupportedFrameworks()
        {
            yield break; // !!! NOT IMPLEMENTED!
        }

        public override Task ExecuteActionsAsync(IEnumerable<Resolution.PackageAction> actions)
        {
            throw new NotImplementedException();
        }
    }
}