using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NuGet.Client.Interop
{
    public static class PackageJsonLd
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static JToken CreatePackageSearchResult(IPackage package, IEnumerable<IPackage> versions)
        {
            var value = new JObject();
            value.Add(new JProperty("@type", new JArray(Types.PackageSearchResult.ToString())));
            AddProp(value, "id", package.Id);
            AddProp(value, "latestVersion", package.Version.ToString());
            AddProp(value, "summary", package.Summary);
            AddProp(value, "iconUrl", package.IconUrl);
            AddProp(value, "packages", versions.Select(v => CreatePackage(v)));
            return value;
        }

        public static JObject CreatePackage(IPackage version)
        {
            var value = new JObject();
            AddProp(value, "@type", new JArray(
                Types.PackageIdentity.ToString(),
                Types.PackageDescription.ToString(),
                Types.PackageDependencies.ToString(),
                Types.PackageLicensing.ToString()));
            
            AddProp(value, "id", version.Id);
            AddProp(value, "version", version.Version.ToString());
            AddProp(value, "summary", version.Summary);
            AddProp(value, "description", version.Description);
            AddProp(value, "authors", version.Authors);
            AddProp(value, "owners", version.Owners);
            AddProp(value, "iconUrl", version.IconUrl);
            AddProp(value, "licenseUrl", version.LicenseUrl);
            AddProp(value, "projectUrl", version.ProjectUrl);
            AddProp(value, "tags", (version.Tags ?? String.Empty).Split(' '));
            AddProp(value, "downloadCount", version.DownloadCount);
            AddProp(value, "published", version.Published.HasValue ? version.Published.Value.ToString("O", CultureInfo.InvariantCulture) : null);
            AddProp(value, "requireLicenseAcceptance", version.RequireLicenseAcceptance);
            AddProp(value, "dependencyGroups", version.DependencySets.Select(set => CreateDependencyGroup(set)));
            return value;
        }

        public static JObject CreateDependencyGroup(PackageDependencySet set)
        {
            var value = new JObject();
            AddProp(value, "@type", Types.DependencyGroup);
            AddProp(value, "targetFramework", set.TargetFramework == null ? null : set.TargetFramework.FullName);
            AddProp(value, "dependencies", set.Dependencies.Select(d => CreateDependency(d)));
            return value;
        }

        public static JObject CreateDependency(PackageDependency dependency)
        {
            var value = new JObject();
            AddProp(value, "@type", Types.Dependency);
            AddProp(value, "id", dependency.Id);
            AddProp(value, "range", dependency.VersionSpec == null ? null : dependency.VersionSpec.ToString());
            return value;
        }

        private static void AddProp(JObject obj, string property, JArray content)
        {
            if (content != null && content.Count != 0)
            {
                obj.Add(new JProperty(property, content));
            }
        }

        private static void AddProp(JObject obj, string property, JToken content)
        {
            if (content != null)
            {
                obj.Add(new JProperty(property, content));
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static void AddProp(JObject obj, string property, object content)
        {
            if (content != null)
            {
                obj.Add(new JProperty(property, content.ToString()));
            }
        }

        // Without this override, a 'string' content parameter falls in to the IEnumerable<T> overload
        // below (T == char), so don't remove it even though it seems useless!
        private static void AddProp(JObject obj, string property, string content)
        {
            if (!String.IsNullOrEmpty(content))
            {
                obj.Add(new JProperty(property, content));
            }
        }

        private static void AddProp<T>(JObject obj, string property, IEnumerable<T> content)
        {
            if (content != null && content.Any())
            {
                obj.Add(new JProperty(property, 
                    new JArray(content)));
            }
        }
    }
}
