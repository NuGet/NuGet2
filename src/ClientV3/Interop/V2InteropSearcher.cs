using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JsonLD.Core;
using Newtonsoft.Json.Linq;

namespace NuGet.VisualStudio.Client.Interop
{
    public class V2InteropSearcher : IPackageSearcher
    {
        private DataServicePackageRepository _repository;

        public V2InteropSearcher(DataServicePackageRepository repository)
        {
            _repository = repository;
        }

        public Task<IEnumerable<JToken>> Search(string searchTerm, SearchFilter filters, int skip, int take, CancellationToken ct)
        {
            return Task.Factory.StartNew(() => _repository.Search(
                searchTerm, filters.SupportedFrameworks, filters.IncludePrerelease)
                .Skip(skip)
                .Take(take)
                .ToList()
                .Select(p => CreatePackageSearchResult(p)), ct);
        }
        private JToken CreatePackageSearchResult(IPackage package)
        {
            // Need to fetch all the versions of this package (this is slow, but we're in V2-interop land, so whatever :))
            var versions = _repository.FindPackagesById(package.Id);

            var value = new JObject(
                new JProperty("@type", new JArray(Uris.Types.PackageSearchResult.AbsoluteUri)),
                MakeProp(Uris.Properties.PackageId, package.Id),
                MakeProp(Uris.Properties.LatestVersion, package.Version.ToString()),
                MakeProp(Uris.Properties.Summary, package.Summary),
                MakeProp(Uris.Properties.IconUrl, package.IconUrl),
                MakeProp(Uris.Properties.PackageVersion, new JArray(versions.Select(v => CreatePackageVersionDetail(v)))));
            return value;
        }

        private JToken MakeValue(object content)
        {
            return new JArray(new JObject(new JProperty("@value", content)));
        }

        private JProperty MakeProp(Uri name, Uri content)
        {
            return MakeProp(name, content.AbsoluteUri);
        }
        private JProperty MakeProp(Uri name, JArray content)
        {
            return new JProperty(name.AbsoluteUri, content);
        }

        private JProperty MakeProp(Uri name, object content)
        {
            return new JProperty(name.AbsoluteUri, MakeValue(content));
        }

        private JObject CreatePackageVersionDetail(IPackage version)
        {
            return new JObject(
                MakeProp(Uris.Properties.PackageId, version.Id),
                MakeProp(Uris.Properties.Version, version.Version.ToString()),
                MakeProp(Uris.Properties.Summary, version.Summary),
                MakeProp(Uris.Properties.Description, version.Description),
                MakeProp(Uris.Properties.Author, new JArray(version.Authors.Select(a => MakeValue(a)))),
                MakeProp(Uris.Properties.Owner, new JArray(version.Owners.Select(a => MakeValue(a)))),
                MakeProp(Uris.Properties.IconUrl, version.IconUrl.AbsoluteUri),
                MakeProp(Uris.Properties.LicenseUrl, version.LicenseUrl),
                MakeProp(Uris.Properties.ProjectUrl, version.ProjectUrl),
                MakeProp(Uris.Properties.Tags, version.Tags),
                MakeProp(Uris.Properties.DownloadCount, version.DownloadCount),
                MakeProp(Uris.Properties.Published, version.Published.HasValue ? version.Published.Value.ToString("O") : null),
                MakeProp(Uris.Properties.DependencyGroup, new JArray(version.DependencySets.Select(set => CreateDependencyGroup(set)))));

        }

        private JObject CreateDependencyGroup(PackageDependencySet set)
        {
            return new JObject(
                MakeProp(Uris.Properties.TargetFramework, set.TargetFramework.FullName),
                MakeProp(Uris.Properties.Dependency, new JArray(set.Dependencies.Select(d => CreateDependency(d)))));
        }

        private JObject CreateDependency(PackageDependency dependency)
        {
            return new JObject(
                MakeProp(Uris.Properties.PackageId, dependency.Id),
                MakeProp(Uris.Properties.VersionRange, dependency.VersionSpec.ToString()));
        }
    }
}
