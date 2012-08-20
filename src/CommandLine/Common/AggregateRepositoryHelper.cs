using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGet.Common
{
    public static class AggregateRepositoryHelper
    {
        public static AggregateRepository CreateAggregateRepositoryFromSources(IPackageRepositoryFactory factory, IPackageSourceProvider sourceProvider, IEnumerable<string> sources, 
            bool useBlobStorageSourceForDefault = false)
        {
            bool ignoreFailingRepositories = sources.IsEmpty();
            sources = sources.IsEmpty() ? sourceProvider.GetEnabledPackageSources().Select(s => s.Source) : 
                                          sources.Select(s => sourceProvider.ResolveSource(s));
            
            var repositories = sources.Select(s => CreateRepository(factory, s, useBlobStorageSourceForDefault))
                                      .ToList();
            return new AggregateRepository(repositories) { IgnoreFailingRepositories = ignoreFailingRepositories };
        }

        private static IPackageRepository CreateRepository(IPackageRepositoryFactory factory, string source, bool useBlobStorageSourceForDefault)
        {
            if (useBlobStorageSourceForDefault && source.Equals(NuGetConstants.DefaultFeedUrl, StringComparison.OrdinalIgnoreCase))
            {
                return new BlobStoragePackageRepository();
            }
            return factory.CreateRepository(source);
        }
    }
}
