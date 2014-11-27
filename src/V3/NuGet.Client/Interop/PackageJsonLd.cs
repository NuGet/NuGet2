using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NuGet.Client.Interop
{
    public static class PackageJsonLd
    {
        /// <summary>
        /// Create JObject representing UiSearchResultPackage.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="versions">all versions of this package.</param>
        /// <returns></returns>
        public static JObject CreatePackageSearchResult(IPackageMetadata package, IEnumerable<SemanticVersion> versions)
        {
            var value = new JObject();
            value.Add(new JProperty(Properties.Type, new JArray(Types.PackageSearchResult.ToString())));
            AddProp(value, Properties.PackageId, package.Id);
            AddProp(value, Properties.LatestVersion, package.Version.ToString());
            AddProp(value, Properties.Summary, package.Summary);
            AddProp(value, Properties.Description, package.Description);
            AddProp(value, Properties.IconUrl, package.IconUrl);

            var allVersions = versions.ToList();
            if (!allVersions.Contains(package.Version))
            {
                allVersions.Add(package.Version);
            }
            
            AddProp(value, Properties.Versions, 
                new JArray(allVersions.Select(v => {
                    var obj = new JObject(); 
                    obj.Add("version", v.ToString());
                    return obj;
                })));
            return value;
        }

        public static JObject CreatePackage(IPackage version)
        {
            return CreatePackage(version, repoRoot: null, pathResolver: null);
        }

        public static JObject CreatePackage(IPackage version, string repoRoot, IPackagePathResolver pathResolver)
        {
            var value = new JObject();
            AddProp(value, Properties.Type, new JArray(
                Types.PackageIdentity.ToString(),
                Types.PackageDescription.ToString(),
                Types.PackageDependencies.ToString(),
                Types.PackageLicensing.ToString()));

            AddProp(value, Properties.PackageId, version.Id);
            AddProp(value, Properties.Version, version.Version.ToString());
            AddProp(value, Properties.Summary, version.Summary);
            AddProp(value, Properties.Description, version.Description);
            AddProp(value, Properties.Authors, String.Join(", ", version.Authors));
            AddProp(value, Properties.Owners, String.Join(", ", version.Owners));
            AddProp(value, Properties.IconUrl, version.IconUrl);
            AddProp(value, Properties.LicenseUrl, version.LicenseUrl);
            AddProp(value, Properties.ProjectUrl, version.ProjectUrl);
            AddProp(value, Properties.Tags, version.Tags == null ? null : version.Tags.Split(' '));
            AddProp(value, Properties.DownloadCount, version.DownloadCount);
            AddProp(value, Properties.Published, version.Published.HasValue ? version.Published.Value.ToString("O", CultureInfo.InvariantCulture) : null);
            AddProp(value, Properties.RequireLicenseAcceptance, version.RequireLicenseAcceptance);
            AddProp(value, Properties.DependencyGroups, version.DependencySets.Select(set => CreateDependencyGroup(set)));

            if (version.MinClientVersion != null)
            {
                AddProp(value, Properties.MinimumClientVersion, version.MinClientVersion.ToString());
            }

            var dsPackage = version as DataServicePackage;
            if (dsPackage != null)
            {
                AddProp(value, Properties.PackageContent, dsPackage.DownloadUrl);
            }
            else if (pathResolver != null)
            {
                AddProp(
                    value,
                    Properties.PackageContent,
                    Path.Combine(repoRoot, pathResolver.GetPackageFileName(version)));
            }

            return value;
        }

        public static JObject CreateDependencyGroup(PackageDependencySet set)
        {
            var value = new JObject();
            AddProp(value, Properties.Type, Types.DependencyGroup);
            AddProp(value, Properties.TargetFramework, set.TargetFramework == null ? null : VersionUtility.GetShortFrameworkName(set.TargetFramework));
            AddProp(value, Properties.Dependencies, set.Dependencies.Select(d => CreateDependency(d)));
            return value;
        }

        public static JObject CreateDependency(PackageDependency dependency)
        {
            var value = new JObject();
            AddProp(value, Properties.Type, Types.Dependency);
            AddProp(value, Properties.PackageId, dependency.Id);
            AddProp(value, Properties.Range, dependency.VersionSpec == null ? null : dependency.VersionSpec.ToString());
            return value;
        }

        public static PackageDependency DependencyFromJson(JObject dependency)
        {
            var range = dependency.Value<string>(Properties.Range);
            var versionSpec = String.IsNullOrEmpty(range) ? null : VersionUtility.ParseVersionSpec(range);

            return new PackageDependency(
                dependency.Value<string>(Properties.PackageId),
                versionSpec);
        }

        public static PackageDependencySet DependencySetFromJson(JObject dependencySet)
        {
            var deps = dependencySet.Value<JArray>(Properties.Dependencies);
            IEnumerable<PackageDependency> depEnum;
            if (deps == null)
            {
                depEnum = Enumerable.Empty<PackageDependency>();
            }
            else
            {
                depEnum = deps.Select(t => DependencyFromJson((JObject)t));
            }

            string fxName = dependencySet.Value<string>(Properties.TargetFramework);

            return new PackageDependencySet(
                String.IsNullOrEmpty(fxName) ? null : VersionUtility.ParseFrameworkName(fxName),
                depEnum);
        }

        public static IPackage PackageFromJson(JObject json)
        {
            return new CoreInteropPackage(json);
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
