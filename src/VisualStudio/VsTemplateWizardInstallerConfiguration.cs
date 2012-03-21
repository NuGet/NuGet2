using System.Collections.Generic;
using System.Linq;

namespace NuGet.VisualStudio
{
    internal sealed class VsTemplateWizardInstallerConfiguration
    {
        public VsTemplateWizardInstallerConfiguration(string repositoryPath, IEnumerable<VsTemplateWizardPackageInfo> packages, bool isPreunzipped)
        {
            Packages = packages.ToList().AsReadOnly();
            RepositoryPath = repositoryPath;
            IsPreunzipped = isPreunzipped;
        }

        public ICollection<VsTemplateWizardPackageInfo> Packages { get; private set; }
        public string RepositoryPath { get; private set; }
        public bool IsPreunzipped { get; private set; }
    }
}