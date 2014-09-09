using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.Interop;
using NuGet.VisualStudio;

namespace NuGet.Client.VisualStudio
{
    /// <summary>
    /// Manages active source repositories using the NuGet Visual Studio settings interfaces
    /// </summary>
    [Export(typeof(SourceRepositoryManager))]
    public class VsSourceRepositoryManager : SourceRepositoryManager
    {
        private readonly IVsPackageSourceProvider _sourceProvider;
        private readonly IPackageRepositoryFactory _repoFactory;

        public override SourceRepository ActiveRepository
        {
            get
            {
                return new V2SourceRepository(
                    new PackageSource(_sourceProvider.ActivePackageSource.Name, _sourceProvider.ActivePackageSource.Source),
                    _repoFactory.CreateRepository(
                        _sourceProvider.ActivePackageSource.Source));
            }
        }

        public override IEnumerable<PackageSource> AvailableSources
        {
            get
            {
                return _sourceProvider
                    .GetEnabledPackageSources()
                    .Select(
                        s => new PackageSource(s.Name, s.Source));
            }
        }

        [ImportingConstructor]
        public VsSourceRepositoryManager(IVsPackageSourceProvider sourceProvider, IPackageRepositoryFactory repoFactory)
        {
            _sourceProvider = sourceProvider;
            _repoFactory = repoFactory;
        }

        public override void ChangeActiveSource(PackageSource newSource)
        {
            var source = _sourceProvider.GetEnabledPackageSources()
                .FirstOrDefault(s => String.Equals(s.Name, newSource.Name, StringComparison.OrdinalIgnoreCase));
            if (source == null)
            {
                throw new ArgumentException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.VsPackageManagerSession_UnknownSource,
                        newSource.Name),
                    "newSource");
            }

            // The Urls should be equal but if they aren't, there's nothing the user can do about it :(
            Debug.Assert(String.Equals(source.Source, newSource.Url, StringComparison.Ordinal));
        }
    }
}
