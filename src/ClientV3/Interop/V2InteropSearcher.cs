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
    public class V2InteropSearcher : IPackageSearcher
    {
        private IPackageRepository _repository;

        public V2InteropSearcher(IPackageRepository repository)
        {
            _repository = repository;
        }

        public Task<IEnumerable<JToken>> Search(string searchTerm, SearchFilter filters, int skip, int take, CancellationToken ct)
        {
            return Task.Factory.StartNew(() => _repository.Search(
                searchTerm, filters.SupportedFrameworks.Select(fx => fx.FullName), filters.IncludePrerelease)
                .Skip(skip)
                .Take(take)
                .ToList()
                .Select(p => CreatePackageSearchResult(p)), ct);
        }
        private JToken CreatePackageSearchResult(IPackage package)
        {
            // Need to fetch all the versions of this package (this is slow, but we're in V2-interop land, so whatever :))
            var versions = _repository.FindPackagesById(package.Id);

            var value = new JObject();
            value.Add(new JProperty("@type", new JArray(Uris.Types.PackageSearchResult.AbsoluteUri)));
            AddProp(value, Uris.Properties.PackageId, package.Id);
            AddProp(value, Uris.Properties.LatestVersion, package.Version.ToString());
            AddProp(value, Uris.Properties.Summary, package.Summary);
            AddProp(value, Uris.Properties.IconUrl, package.IconUrl);
            AddProp(value, Uris.Properties.PackageVersion, new JArray(versions.Select(v => CreatePackageVersionDetail(v))));
            return value;
        }

        private JObject CreatePackageVersionDetail(IPackage version)
        {
            var value = new JObject();
            AddProp(value, Uris.Properties.PackageId, version.Id);
            AddProp(value, Uris.Properties.Version, version.Version.ToString());
            AddProp(value, Uris.Properties.Summary, version.Summary);
            AddProp(value, Uris.Properties.Description, version.Description);
            AddProp(value, Uris.Properties.Author, new JArray(version.Authors.Select(a => MakeValue(a))));
            AddProp(value, Uris.Properties.Owner, new JArray(version.Owners.Select(a => MakeValue(a))));
            AddProp(value, Uris.Properties.IconUrl, version.IconUrl);
            AddProp(value, Uris.Properties.LicenseUrl, version.LicenseUrl);
            AddProp(value, Uris.Properties.ProjectUrl, version.ProjectUrl);
            AddProp(value, Uris.Properties.Tags, version.Tags);
            AddProp(value, Uris.Properties.DownloadCount, version.DownloadCount);
            AddProp(value, Uris.Properties.Published, version.Published.HasValue ? version.Published.Value.ToString("O") : null);
            AddProp(value, Uris.Properties.DependencyGroup, new JArray(version.DependencySets.Select(set => CreateDependencyGroup(set))));
            return value;
        }

        private JObject CreateDependencyGroup(PackageDependencySet set)
        {
            var value = new JObject();
            AddProp(value, Uris.Properties.TargetFramework, set.TargetFramework == null ? null : set.TargetFramework.FullName);
            AddProp(value, Uris.Properties.Dependency, new JArray(set.Dependencies.Select(d => CreateDependency(d))));
            return value;
        }

        private JObject CreateDependency(PackageDependency dependency)
        {
            var value = new JObject();
            AddProp(value, Uris.Properties.PackageId, dependency.Id);
            AddProp(value, Uris.Properties.VersionRange, dependency.VersionSpec == null ? null : dependency.VersionSpec.ToString());
            return value;
        }

        private JToken MakeValue(object content)
        {
            return new JObject(new JProperty("@value", content));
        }

        private void AddProp(JObject obj, Uri property, JArray content)
        {
            if (content != null && content.Count != 0)
            {
                obj.Add(new JProperty(property.AbsoluteUri, content));
            }
        }

        private void AddProp(JObject obj, Uri property, Uri content)
        {
            if (content != null)
            {
                AddProp(obj, property, content.ToString());
            }
        }

        private void AddProp(JObject obj, Uri property, object content)
        {
            if (content != null)
            {
                obj.Add(new JProperty(property.AbsoluteUri, new JArray(MakeValue(content))));
            }
        }
    }
}
