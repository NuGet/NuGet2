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
                var repositories = sources.Select(sourceProvider.ResolveSource)
                                             .Select(factory.CreateRepository)
                                             .ToList();
                repository = new AggregateRepository(repositories);
            }
            else
            {
                repository = sourceProvider.GetAggregate(factory, ignoreFailingRepositories: true);
            }

            return repository;
        }
    }
}
