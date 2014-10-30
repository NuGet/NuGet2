using Newtonsoft.Json.Linq;
using NuGet.Client;
using NuGet.Versioning;
using System.Linq;

namespace NuGet.Resolution
{
    public static class PackageExtensions
    {
        public static JArray GetDependencies(this JObject package)
        {
            //TODO: Support DependencyGroup TargetFramework. For now, always take the first dependencyGroup
            if (package[Properties.DependencyGroups] == null)
                return new JArray();
            return package[Properties.DependencyGroups][0][Properties.Dependencies] as JArray;
        }

        public static PackageIdentity AsPackageIdentity(this JObject package)
        {
            return new PackageIdentity(package.GetId(), new NuGetVersion(package.Value<string>(Properties.Version)));
        }

        public static string GetId(this JObject package)
        {
            return package.Value<string>(Properties.PackageId);
        }

        public static string GetVersionAsString(this JObject package)
        {
            return package.Value<string>(Properties.Version);
        }
        public static NuGetVersion GetVersion(this JObject package)
        {
            return NuGetVersion.Parse(package.GetVersionAsString());
        }

        public static VersionRange FindDependencyRange(this JObject package, string id)
        {
            var dependencies = package.GetDependencies();
            if(dependencies == null)
            {
                return null;
            }

            var dependency = dependencies.Cast<JObject>().FirstOrDefault(d => d.Value<string>(Properties.PackageId) == id);
            if (dependency == null)
            {
                return null;
            }

            string rangeString = dependency.Value<string>(Properties.Range);
            if (string.IsNullOrEmpty(rangeString))
            {
                return null;
            }

            VersionRange range = null;
            return VersionRange.TryParse(rangeString, out range) ? range : null;
        }
    }
}
