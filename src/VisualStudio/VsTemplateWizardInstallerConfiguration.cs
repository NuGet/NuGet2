using System.Collections.Generic;
using System.Linq;

namespace NuGet.VisualStudio {
    internal sealed class VsTemplateWizardInstallerConfiguration {
        public VsTemplateWizardInstallerConfiguration(string repositoryPath, IEnumerable<VsTemplateWizardPackageInfo> packages) {
            Packages = packages.ToList().AsReadOnly();
            RepositoryPath = repositoryPath;
        }

        public ICollection<VsTemplateWizardPackageInfo> Packages { get; private set; }
        public string RepositoryPath { get; private set; }
    }
}