using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JsonLD.Core;
using Newtonsoft.Json.Linq;

namespace NuGet.Client.Interop
{
    public class V2SourceRepository : SourceRepository
    {
        private readonly IPackageRepository _repository;
        private readonly PackageSource _source;

        public override PackageSource Source { get { return _source; } }

        public V2SourceRepository(PackageSource source, IPackageRepository repository)
        {
            _source = source;

            _repository = repository;
        }

        public override Task<IEnumerable<JObject>> Search(string searchTerm, SearchFilter filters, int skip, int take, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() => _repository.Search(
                searchTerm, filters.SupportedFrameworks.Select(fx => fx.FullName), filters.IncludePrerelease)
                .Skip(skip)
                .Take(take)
                .ToList()
                .Select(p => CreatePackageSearchResult(p)), cancellationToken);
        }

        private JObject CreatePackageSearchResult(IPackage package)
        {
            var versions = _repository.FindPackagesById(package.Id);
            return PackageJsonLd.CreatePackageSearchResult(package, versions);
        }
    }
}
