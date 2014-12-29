using NuGet.Client;
using NuGet.Client.Interop;
using NuGet.Client.VisualStudio.Models;
using NuGet.Versioning;
using NuGet.VisualStudio;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

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
            var source = new PackageSource(
                Strings.VsSourceRepositoryManager_V3SourceName,
                NuGetConstants.V3FeedUrl);
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
                var coreSources = _sourceProvider
                    .GetEnabledPackageSources()
                    .Select(s => new PackageSource(s.Name, s.Source));
                foreach (var source in coreSources)
                {
                    yield return source;
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]      
        [ImportMany(typeof(ResourceProvider))]
        public IEnumerable<Lazy<ResourceProvider, IResourceProviderMetadata>> providers {get;set;}

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
            //Temp code to illustrate the usage of new SourceRepo and resources and to do quick testing to see if all the resources are available at this point in codepath. This will be removed in next iteration.
            //SourceRepository2 repo2 = new SourceRepository2(source, providers);
            //IVsSearch searchResource = repo2.GetResource<IVsSearch>();
            //Debug.Assert(searchResource != null);
            //SearchFilter filter = new SearchFilter(); //create a dummy filter.
            //List<FrameworkName> fxNames = new List<FrameworkName>();
            //fxNames.Add(new FrameworkName(".NET Framework, Version=4.0"));
            //filter.SupportedFrameworks = fxNames;
            //IEnumerable<VisualStudioUISearchMetadata> searchResults = searchResource.GetSearchResultsForVisualStudioUI("Elmah", filter, 0, 100, new System.Threading.CancellationToken()).Result;
            //Debug.Assert(searchResults.Count() > 0); // Check if non empty search result is returned.
            //Debug.Assert(searchResults.Any(p => p.Id.Equals("Elmah", StringComparison.OrdinalIgnoreCase))); //check if there is atleast one result which has Elmah as title.
            //IDownload download = repo2.GetResource<IDownload>();
            //PackageDownloadMetadata downloadMetadata = download.GetNupkgUrlForDownload(new PackageIdentity("jQuery", new NuGetVersion("1.6.4"))).Result;
            //Debug.Assert(downloadMetadata.NupkgDownloadUrl.ToString().Contains("1.6.4"));
            //IMetadata metadata = repo2.GetResource<IMetadata>();
            //NuGetVersion latestVersion = metadata.GetLatestVersion("jQuery").Result;
            //Debug.Assert(latestVersion.ToString().Equals("2.1.1"));
            return new AutoDetectSourceRepository(source, VsVersionHelper.FullVsEdition, _repoFactory);
         
        }
    }
}
