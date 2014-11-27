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
using System.Collections.Concurrent;

namespace NuGet.Client.VisualStudio
{
    /// <summary>
    /// Manages active source repositories using the NuGet Visual Studio settings interfaces
    /// </summary>
    [Export(typeof(SourceRepositoryManager))]
    public class VsSourceRepositoryManager : SourceRepositoryManager
    {
        private static readonly PackageSource NuGetV3PreviewSource = CreateV3PreviewSource();

        private readonly IVsPackageSourceProvider _sourceProvider;
        private readonly IPackageRepositoryFactory _repoFactory;
        private readonly ConcurrentDictionary<string, SourceRepository> _repos = new ConcurrentDictionary<string, SourceRepository>();

        private static PackageSource CreateV3PreviewSource()
        {
            var url = "https://az320820.vo.msecnd.net/ver3-preview/index.json";
            var source = new PackageSource(
                Strings.VsSourceRepositoryManager_V3SourceName,
                url);
            return source;
        }

        public override SourceRepository ActiveRepository
        {
            get
            {
                Debug.Assert(!_sourceProvider.ActivePackageSource.IsAggregate(), "Active source is the aggregate source! This shouldn't happen!");

                if (_sourceProvider.ActivePackageSource == null)
                {
                    return null;
                }

                return GetRepo(new PackageSource(
                    _sourceProvider.ActivePackageSource.Name,
                    _sourceProvider.ActivePackageSource.Source));
            }
        }

        public override SourceRepository CreateSourceRepository(PackageSource packageSource)
        {
            return GetRepo(packageSource);
        }

        public override IEnumerable<PackageSource> AvailableSources
        {
            get
            {
                yield return NuGetV3PreviewSource;

                var coreSources = _sourceProvider
                    .GetEnabledPackageSources()
                    .Select(s => new PackageSource(s.Name, s.Source));
                foreach (var source in coreSources)
                {
                    yield return source;
                }
            }
        }

        [ImportingConstructor]
        public VsSourceRepositoryManager(IVsPackageSourceProvider sourceProvider, IPackageRepositoryFactory repoFactory)
        {
            _sourceProvider = sourceProvider;
            _sourceProvider.PackageSourcesSaved += (sender, e) =>
            {
                if (PackageSourcesChanged != null)
                {
                    PackageSourcesChanged(this, EventArgs.Empty);
                }
            };
            _repoFactory = repoFactory;
        }

        public override void ChangeActiveSource(PackageSource newSource)
        {
            if (newSource.Equals(NuGetV3PreviewSource))
            {
                return; // Can't set it as default
            }

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

            // Update the active package source
            _sourceProvider.ActivePackageSource = source;
        }

        public override event EventHandler PackageSourcesChanged;
        private SourceRepository GetRepo(PackageSource p)
        {
            return _repos.GetOrAdd(p.Url, _ => CreateRepo(p));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "These objects live until end of process, at which point they will be disposed automatically")]
        private SourceRepository CreateRepo(PackageSource source)
        {
            return new AutoDetectSourceRepository(source, VsVersionHelper.FullVsEdition, _repoFactory);
        }
    }
}
