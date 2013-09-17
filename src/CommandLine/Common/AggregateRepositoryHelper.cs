using System.Collections.Generic;
using System.Linq;

namespace NuGet.Common
{
    public static class AggregateRepositoryHelper
    {
        public static AggregateRepository CreateAggregateRepositoryFromSources(IPackageRepositoryFactory factory, IPackageSourceProvider sourceProvider, IEnumerable<string> sources)
        {
            AggregateRepository repository;
            if (sources != null && sources.Any())
            {
                var repositories = sources.Select(s => sourceProvider.ResolveSource(s))
                                             .Select(factory.CreateRepository)
                                             .ToList();
                repository = new AggregateRepository(repositories);
            }
            else
            {
                repository = sourceProvider.CreateAggregateRepository(factory, ignoreFailingRepositories: true);
            }

            return repository;
        }
    }
}
