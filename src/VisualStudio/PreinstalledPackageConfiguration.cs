using System.Collections.Generic;
using System.Linq;

namespace NuGet.VisualStudio
{
    internal sealed class PreinstalledPackageConfiguration
    {
        public PreinstalledPackageConfiguration(string repositoryPath, IEnumerable<PreinstalledPackageInfo> packages, bool isPreunzipped)
        {
            Packages = packages.ToList().AsReadOnly();
            RepositoryPath = repositoryPath;
            IsPreunzipped = isPreunzipped;
        }

        public ICollection<PreinstalledPackageInfo> Packages { get; private set; }
        public string RepositoryPath { get; private set; }
        public bool IsPreunzipped { get; private set; }
    }
}