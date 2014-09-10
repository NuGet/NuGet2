using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Versioning;
using NuGet.VisualStudio;

namespace NuGet.Client.VisualStudio
{
    public class VsSolutionInstallationTarget : InstallationTarget
    {
        EnvDTE.Solution _solution;
        string _name;

        public VsSolutionInstallationTarget(EnvDTE.Solution solution)
        {
            _solution = solution;
            _name = string.Format("Solution '{0}'", _solution.GetName());
        }

        public EnvDTE.Solution Solution
        {
            get
            {
                return _solution;
            }
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        public IEnumerable<PackageIdentity> GetInstalledPackages(EnvDTE.Project project)
        {
            var projectManager = ServiceLocator.GetInstance<IVsPackageManagerFactory>()
                .CreatePackageManagerToManageInstalledPackages()
                .GetProjectManager(project);
            return projectManager.LocalRepository.GetPackages().Select(p => new PackageIdentity(
                p.Id,
                new NuGetVersion(p.Version.Version, p.Version.SpecialVersion, null)));
        }

        public override Task<IEnumerable<Newtonsoft.Json.Linq.JObject>> SearchInstalledPackages(string searchTerm, int skip, int take, System.Threading.CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<PackageIdentity> GetInstalledPackages()
        {
            throw new NotImplementedException();
        }

        public override Versioning.NuGetVersion GetInstalledVersion(string packageId)
        {
            return null; // !!!
        }

        public override bool IsInstalled(string packageId, Versioning.NuGetVersion packageVersion)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<System.Runtime.Versioning.FrameworkName> GetSupportedFrameworks()
        {
            return new System.Runtime.Versioning.FrameworkName[] { };
        }

        public override Task ExecuteActionsAsync(IEnumerable<Resolution.PackageAction> actions)
        {
            throw new NotImplementedException();
        }
    }
}
