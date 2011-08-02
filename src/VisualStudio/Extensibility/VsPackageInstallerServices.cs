using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace NuGet.VisualStudio {
    [Export(typeof(IVsPackageInstallerServices))]
    public class VsPackageInstallerServices : IVsPackageInstallerServices {
        private readonly IVsPackageManagerFactory _packageManagerFactory;

        [ImportingConstructor]
        public VsPackageInstallerServices(IVsPackageManagerFactory packageManagerFactory) {
            _packageManagerFactory = packageManagerFactory;
        }

        public IEnumerable<IVsPackageMetadata> GetInstalledPackages() {
            var packageManager = _packageManagerFactory.CreatePackageManager();

            return from package in packageManager.LocalRepository.GetPackages()
                   select new VsPackageMetadata(package,
                                                packageManager.PathResolver.GetInstallPath(package));
        }
    }
}
