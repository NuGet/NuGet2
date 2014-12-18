using Newtonsoft.Json.Linq;
using NuGet.Resources;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Client.VisualStudio.PowerShell
{
    // TODO List
    // 1. The unlisted packages are not filtered out. The plan is that Server will return unlisted packages.
    // Test EntityFramework 7.0.0-beta1 is not installed when specify -pre.
    // 2. Should we add Title to the display? If so, need to embed title field in the search result.
    public class PowerShellPackage
    {
        public string Id { get; set; }

        public List<NuGetVersion> Version { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// Get the view of PowerShell Package. Use for Get-Package command. 
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="versionType"></param>
        /// <returns></returns>
        public static List<PowerShellPackage> GetPowerShellPackageView(IEnumerable<JObject> metadata, VersionType versionType)
        {
            List<PowerShellPackage> view = new List<PowerShellPackage>();
            foreach (JObject json in metadata)
            {
                PowerShellPackage package = new PowerShellPackage();
                package.Id = json.Value<string>(Properties.PackageId);
                package.Version = new List<NuGetVersion>();
                string version = string.Empty;
                NuGetVersion nVersion;

                switch (versionType)
                {
                    case VersionType.all:
                        JArray versions = json.Value<JArray>(Properties.Versions);
                        if (!versions.IsEmpty())
                        {
                            if (versions.FirstOrDefault().Type == JTokenType.Object)
                            {
                                package.Version = versions.Select(j => NuGetVersion.Parse((string)j["version"]))
                                    .OrderByDescending(v => v)
                                    .ToList();
                            }

                            if (versions.FirstOrDefault().Type == JTokenType.String)
                            {
                                package.Version = versions.Select(j => (NuGetVersion.Parse((string)j)))
                                    .OrderByDescending(v => v)
                                    .ToList();
                            }
                        }
                        break;
                    case VersionType.latest:
                        version = json.Value<string>(Properties.LatestVersion);
                        nVersion = NuGetVersion.Parse(version);
                        package.Version.Add(nVersion);
                        break;
                    case VersionType.single:
                        version = json.Value<string>(Properties.Version);
                        nVersion = NuGetVersion.Parse(version);
                        package.Version.Add(nVersion);
                        break;
                }

                package.Description = json.Value<string>(Properties.Description);
                if (string.IsNullOrEmpty(package.Description))
                {
                    package.Description = json.Value<string>(Properties.Summary);
                }
                view.Add(package);
            }
            return view;
        }

        /// <summary>
        /// Get all versions of packages that match the search term of packageId.
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="packageId"></param>
        /// <param name="names"></param>
        /// <param name="allowPrerelease"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public static IEnumerable<JObject> GetPackageVersions(SourceRepository repo, string packageId, IEnumerable<FrameworkName> names, bool allowPrerelease, int skip, int take)
        {
            // Specify the search filter for prerelease and target framework
            SearchFilter filter = new SearchFilter();
            filter.IncludePrerelease = allowPrerelease;
            filter.SupportedFrameworks = names;

            IEnumerable<JObject> searchResults = Enumerable.Empty<JObject>();
            try
            {
                Task<IEnumerable<JObject>> task = repo.Search(packageId, filter, skip, take, cancellationToken: CancellationToken.None);
                searchResults = task.Result;
            }
            catch (Exception)
            {
                if (searchResults.IsEmpty())
                {
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture,
                        NuGetResources.UnknownPackage, packageId));
                }
            }
            return searchResults;
        }

        public static string GetLastestVersionForPackage(SourceRepository repo, string packageId, IEnumerable<FrameworkName> names, bool allowPrerelease, 
            NuGetVersion nugetVersion = null, bool isSafe = false, int skip = 0, int take = 30)
        {
            if (packageId == null)
            {
                throw new ArgumentNullException();
            }

            // Specify the search filter for prerelease and target framework
            SearchFilter filter = new SearchFilter();
            filter.IncludePrerelease = allowPrerelease;
            filter.SupportedFrameworks = names;

            string version = String.Empty;
            try
            {
                Task<IEnumerable<JObject>> task = repo.Search(packageId, filter, skip, take, cancellationToken: CancellationToken.None);
                IEnumerable<JObject> packages = task.Result;
                // Get the package with the specific Id.
                JObject package = packages.Where(p => string.Equals(p.Value<string>(Properties.PackageId), packageId, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                // Return latest version if Safe is not required and latestVersion is not null or empty.
                version = package.Value<String>(Properties.LatestVersion);
                if (!isSafe && !string.IsNullOrEmpty(version))
                {
                    return version;
                }

                // Continue to get the latest version when above condition is not met.
                IEnumerable<NuGetVersion> allVersions = Enumerable.Empty<NuGetVersion>();
                var versionList = new List<NuGetVersion>();
                var versions = package.Value<JArray>(Properties.Versions);
                if (versions != null)
                {
                    if (versions[0].Type == JTokenType.String)
                    {
                        // TODO: this part should be removed once the new end point is up and running.
                        versionList = versions
                            .Select(v => NuGetVersion.Parse(v.Value<string>()))
                            .ToList();
                    }
                    else
                    {
                        versionList = versions
                            .Select(v => NuGetVersion.Parse(v.Value<string>("version")))
                            .ToList();
                    }
                }

                if (isSafe && nugetVersion != null)
                {
                    VersionRange spec = GetSafeRange(nugetVersion, allowPrerelease);
                    allVersions = versionList.Where(p =>
                    {
                        var sVersion = new SemanticVersion(p.ToNormalizedString());
                        return p < spec.MaxVersion && p >= spec.MinVersion;
                    });
                }
                version = allVersions.OrderByDescending(v => v).FirstOrDefault().ToNormalizedString();
            }
            catch (Exception)
            {
                if (string.IsNullOrEmpty(version))
                {
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture,
                        NuGetResources.UnknownPackage, packageId));
                }
            }
            return version;
        }

        /// <summary>
        /// Get latest update for package identity
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="identity"></param>
        /// <param name="allowPrerelease"></param>
        /// <returns></returns>
        public static PackageIdentity GetLastestUpdateForPackage(SourceRepository repo, PackageIdentity identity, VsProject project, bool allowPrerelease, bool isSafe)
        {
            string latestVersion = GetLastestVersionForPackage(repo, identity.Id, project.GetSupportedFrameworks(), allowPrerelease, identity.Version, isSafe);
            PackageIdentity latestIdentity = null;
            if (latestVersion != null)
            {
                latestIdentity = new PackageIdentity(identity.Id, NuGetVersion.Parse(latestVersion));
            }
            return latestIdentity;
        }

        /// <summary>
        /// Get latest JObject for package identity
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="identity"></param>
        /// <param name="allowPrerelease"></param>
        /// <returns></returns>
        public static List<JObject> GetLastestJObjectsForPackage(SourceRepository repo, JObject jObject, VsProject project, bool allowPrerelease, int skip, int take, bool takeAllVersions)
        {
            List<JObject> jObjects = new List<JObject>();
            string id = jObject.Value<string>(Properties.PackageId);
            string version = jObject.Value<string>(Properties.Version);
            NuGetVersion nVersion = GetNuGetVersionFromString(version);

            if (!takeAllVersions)
            {
                string latestVersion = GetLastestVersionForPackage(repo, id, project.GetSupportedFrameworks(), allowPrerelease, nVersion, false);
                if (latestVersion != null)
                {
                    NuGetVersion laVersion = GetNuGetVersionFromString(latestVersion);
                    Task<JObject> task = repo.GetPackageMetadata(id, laVersion);
                    JObject latestJObject = task.Result;
                    jObjects.Add(latestJObject);
                }
            }
            else
            {
                Task<IEnumerable<JObject>> task = repo.GetPackageMetadataById(id);
                IEnumerable<JObject> allVersions = task.Result;
                jObjects = allVersions.Where(p => string.Equals(p.Value<string>(Properties.PackageId), id, StringComparison.OrdinalIgnoreCase))
                    .Skip(skip)
                    .Take(take)
                    .ToList();
            }
            return jObjects;
        }

        public static JObject GetPackageByIdAndVersion(SourceRepository sourceRepository, string packageId, string version, bool allowPrereleaseVersions)
        {
            NuGetVersion nVersion = GetNuGetVersionFromString(version);
            if (String.IsNullOrEmpty(packageId))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            Task<JObject> task = sourceRepository.GetPackageMetadata(packageId, nVersion);
            JObject package = task.Result;
            if (package == null)
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    NuGetResources.UnknownPackageSpecificVersion, packageId, version));
            }
            return package;
        }

        public static NuGetVersion GetNuGetVersionFromString(string version)
        {
            NuGetVersion nVersion;
            if (version == null)
            {
                throw new ArgumentNullException();
            }
            else
            {
                bool success = NuGetVersion.TryParse(version, out nVersion);
                if (!success)
                {
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture,
                        Resources.Cmdlet_FailToParseVersion, version));
                }
                return nVersion;
            }
        }

        /// <summary>
        /// The safe range is defined as the highest build and revision for a given major and minor version
        /// </summary>
        public static VersionRange GetSafeRange(NuGetVersion version, bool includePrerlease)
        {
            SemanticVersion max = new SemanticVersion(new Version(version.Major, version.Minor + 1));
            NuGetVersion maxVersion = NuGetVersion.Parse(max.ToString());
            return new VersionRange(version, true, maxVersion, false, includePrerlease);
        }

        public static bool IsPrereleaseVersion(string version)
        {
            SemanticVersion sVersion = new SemanticVersion(version);
            bool isPrerelease = !String.IsNullOrEmpty(sVersion.SpecialVersion);
            return isPrerelease;
        }
    }

    public class PowerShellPackageWithProject
    {
        public string Id { get; set; }

        public List<NuGetVersion> Version { get; set; }

        public string ProjectName { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// Get the view of PowerShell Package. Use for Get-Package command. 
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="versionType"></param>
        /// <returns></returns>
        public static List<PowerShellPackageWithProject> GetPowerShellPackageView(Dictionary<VsProject, IEnumerable<JObject>> dictionary, VersionType versionType)
        {
            List<PowerShellPackageWithProject> views = new List<PowerShellPackageWithProject>();
            foreach (KeyValuePair<VsProject, IEnumerable<JObject>> entry in dictionary)
            {
                List<PowerShellPackage> packages = PowerShellPackage.GetPowerShellPackageView(entry.Value, versionType);
                foreach (PowerShellPackage package in packages)
                {
                    PowerShellPackageWithProject view = new PowerShellPackageWithProject();
                    view.Id = package.Id;
                    view.Version = package.Version;
                    view.Description = package.Description;
                    view.ProjectName = entry.Key.Name;
                    views.Add(view);
                }
            }
            return views;
        }
    }

    public enum VersionType
    {
        all,
        latest,
        single
    }
}
