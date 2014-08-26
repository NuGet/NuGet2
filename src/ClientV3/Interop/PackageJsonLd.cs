using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NuGet.VisualStudio.ClientV3.Interop
{
    internal static class PackageJsonLd
    {
        public static JObject CreatePackageSearchResult(IPackage package, IEnumerable<IPackage> versions)
        {
            var value = new JObject();
            value.Add(new JProperty("@type", new JArray(Uris.Types.PackageSearchResult.AbsoluteUri)));
            AddProp(value, Uris.Properties.PackageId, package.Id);
            AddProp(value, Uris.Properties.LatestVersion, package.Version.ToString());
            AddProp(value, Uris.Properties.Summary, package.Summary);
            AddProp(value, Uris.Properties.IconUrl, package.IconUrl);
            AddProp(value, Uris.Properties.PackageVersion, new JArray(versions.Select(v => CreatePackageVersionDetail(v))));
            return value;
        }
        private static JObject CreatePackageVersionDetail(IPackage version)
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

        private static JObject CreateDependencyGroup(PackageDependencySet set)
        {
            var value = new JObject();
            AddProp(value, Uris.Properties.TargetFramework, set.TargetFramework == null ? null : set.TargetFramework.FullName);
            AddProp(value, Uris.Properties.Dependency, new JArray(set.Dependencies.Select(d => CreateDependency(d))));
            return value;
        }

        private static JObject CreateDependency(PackageDependency dependency)
        {
            var value = new JObject();
            AddProp(value, Uris.Properties.PackageId, dependency.Id);
            AddProp(value, Uris.Properties.VersionRange, dependency.VersionSpec == null ? null : dependency.VersionSpec.ToString());
            return value;
        }

        private static JToken MakeValue(object content)
        {
            return new JObject(new JProperty("@value", content));
        }

        private static void AddProp(JObject obj, Uri property, JArray content)
        {
            if (content != null && content.Count != 0)
            {
                obj.Add(new JProperty(property.AbsoluteUri, content));
            }
        }

        private static void AddProp(JObject obj, Uri property, Uri content)
        {
            if (content != null)
            {
                AddProp(obj, property, content.ToString());
            }
        }

        private static void AddProp(JObject obj, Uri property, object content)
        {
            if (content != null)
            {
                obj.Add(new JProperty(property.AbsoluteUri, new JArray(MakeValue(content))));
            }
        }
    }
}
