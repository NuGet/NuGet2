using System.Collections.Generic;
using System.Linq;
using Microsoft.WebMatrix.Extensibility;
using NuGet;

namespace NuGet.WebMatrix
{
    internal class NuGetFeedSourceStore : FeedSourceStore, IFeedSourceStore
    {
        private PackageSourceProvider _packageSourceProvider;

        public NuGetFeedSourceStore(IPreferences preferences)
            : base(preferences)
        {
            _packageSourceProvider = new PackageSourceProvider(
                Settings.LoadDefaultSettings(null),
                defaultSources: new[] { new PackageSource("https://www.nuget.org/api/v2", Resources.NuGet_PackageSourceName) });
        }

        public override void SavePackageSources(IEnumerable<FeedSource> sources)
        {
            _packageSourceProvider.SavePackageSources(sources.Select((source) => source.ToNuGetPackageSource()));
        }

        public override IEnumerable<FeedSource> LoadPackageSources()
        {
            // Convert each PackageSource to a FeedSource and add to the collection
            // the conversion will fail if the package has an invalid Url
            return _packageSourceProvider.LoadPackageSources()
                .Select((packageSource) => packageSource.ToFeedSource())
                .Where(p => p != null);
        }
    }
}
