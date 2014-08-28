using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client;
using NuGet.Client.Interop;
using NuGet.VisualStudio;

namespace NuGet.Client.VisualStudio
{
    public abstract class VsPackageManagerSession : PackageManagerSession
    {
        private IVsPackageSourceProvider _packageSourceProvider;
        private IPackageRepositoryFactory _repoFactory;

        public override PackageSource ActiveSource
        {
            get { return _packageSourceProvider.ActivePackageSource; }
        }

        public static VsPackageManagerSession ForProject(EnvDTE.Project project)
        {
            return new ProjectPackageManagerSession(project);
        }

        public override IEnumerable<PackageSource> GetAvailableSources()
        {
            return _packageSourceProvider.GetEnabledPackageSourcesWithAggregate();
        }

        public override IPackageSearcher CreateSearcher()
        {
            var v2Repo = _repoFactory.CreateRepository(ActiveSource.Source);
            return new V2InteropSearcher(v2Repo);
        }

        public override void ChangeActiveSource(string newSourceName)
        {
            var source = GetAvailableSources().FirstOrDefault(s =>
                String.Equals(newSourceName, s.Name, StringComparison.OrdinalIgnoreCase));
            if (source == null)
            {
                throw new ArgumentException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.VsPackageManagerSession_UnknownSource,
                        newSourceName), 
                    "newSourceName");
            }
            _packageSourceProvider.ActivePackageSource = source;
        }
    }
}
