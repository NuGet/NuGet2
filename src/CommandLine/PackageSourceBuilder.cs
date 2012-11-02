using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet
{
    internal static class PackageSourceBuilder
    {
        internal static PackageSourceProvider CreateSourceProvider(ISettings settings)
        {
            var defaultPackageSource = new PackageSource(NuGetConstants.DefaultFeedUrl);

            var officialPackageSource = new PackageSource(NuGetConstants.DefaultFeedUrl, NuGetResources.OfficialPackageSourceName);
            var v1PackageSource = new PackageSource(NuGetConstants.V1FeedUrl, NuGetResources.OfficialPackageSourceName);
            var legacyV2PackageSource = new PackageSource(NuGetConstants.V2LegacyFeedUrl, NuGetResources.OfficialPackageSourceName);

            var packageSourceProvider = new PackageSourceProvider(
                settings,
                new[] { defaultPackageSource },
                new Dictionary<PackageSource, PackageSource> { 
                            { v1PackageSource, officialPackageSource },
                            { legacyV2PackageSource, officialPackageSource }
                        }
            );
            return packageSourceProvider;
        }
    }
}
