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
    // 1. Should we add Title to the display? If so, need to embed title field in the search result.
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
                        if (versions != null && !versions.IsEmpty())
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
                        else
                        {
                            version = json.Value<string>(Properties.Version);
                            nVersion = NuGetVersion.Parse(version);
                            package.Version.Add(nVersion);
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
            filter.SupportedFrameworks = names.Select(n => VersionUtility.GetShortFrameworkName(n));

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

        /// <summary>
        /// Get all versions of a specific package with packageId
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="packageId"></param>
        /// <param name="names"></param>
        /// <param name="allowPrerelease"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public static IEnumerable<NuGetVersion> GetAllVersionsForPackageId(JObject package)
        {
            // Get all versions of package
            List<NuGetVersion> versionList = new List<NuGetVersion>();
            JArray versions = null;
            if (package != null)
            {
                versions = package.Value<JArray>(Properties.Versions);
            }

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
            return versionList;
        }

        /// <summary>
        /// Get JObject of a specific package with packageId
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="packageId"></param>
        /// <param name="names"></param>
        /// <param name="allowPrerelease"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public static JObject GetJObjectForPackageId(SourceRepository repo, string packageId, IEnumerable<FrameworkName> names, bool allowPrerelease, int skip, int take)
        {
            if (packageId == null)
            {
                throw new ArgumentNullException();
            }

            // Specify the search filter for prerelease and target framework
            SearchFilter filter = new SearchFilter();
            filter.IncludePrerelease = allowPrerelease;
            filter.SupportedFrameworks = names.Select(n => VersionUtility.GetShortFrameworkName(n));
            JObject package = null;

            try
            {
                Task<IEnumerable<JObject>> task = repo.Search(packageId, filter, skip, take, cancellationToken: CancellationToken.None);
                IEnumerable<JObject> packages = task.Result;
                // Get the package with the specific Id.
                package = packages.Where(p => string.Equals(p.Value<string>(Properties.PackageId), packageId, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            }
            catch (Exception)
            {
                if (package == null)
                {
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture,
                        NuGetResources.UnknownPackage, packageId));
                }
            }
            return package;
        }

        public static string GetLastestVersionForPackage(SourceRepository repo, string packageId, IEnumerable<FrameworkName> names, bool allowPrerelease,
            NuGetVersion nugetVersion = null, bool isSafe = false, int skip = 0, int take = 30)
        {
            string version = string.Empty;
            try
            {
                JObject package = GetJObjectForPackageId(repo, packageId, names, allowPrerelease, skip, take);
                // Return latest version if Safe is not required and latestVersion is not null or empty.
                if (package != null)
                {
                    version = package.Value<String>(Properties.LatestVersion);
                }

                if (!isSafe && !string.IsNullOrEmpty(version))
                {
                    return version;
                }

                // Continue to get the latest version when above condition is not met.
                IEnumerable<NuGetVersion> allVersions = Enumerable.Empty<NuGetVersion>();
                var versionList = GetAllVersionsForPackageId(package);

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
                    if (laVersion > nVersion)
                    {
                        Task<JObject> task = repo.GetPackageMetadata(id, laVersion);
                        JObject latestJObject = task.Result;
                        jObjects.Add(latestJObject);
                    }
                }
            }
            else
            {
                JObject package = GetJObjectForPackageId(repo, id, project.GetSupportedFrameworks(), allowPrerelease, skip, take);
                IEnumerable<NuGetVersion> versionList = GetAllVersionsForPackageId(package).OrderByDescending(v => v);
                // Work around a bug in repo.Search(), where prerelease versions are not filtered out.
                if (!allowPrerelease)
                {
                    versionList = versionList.Where(p => p > nVersion && p.IsPrerelease == false);
                }
                else
                {
                    versionList = versionList.Where(p => p > nVersion);
                }

                foreach (NuGetVersion updateVersion in versionList)
                {
                    JObject updateObject = GetPackageByIdAndVersion(repo, id, updateVersion.ToNormalizedString(), allowPrerelease);
                    jObjects.Add(updateObject);
                }
            }
            return jObjects;
        }

        /// <summary>
        /// Get the JObject of a package with known packageId and version
        /// </summary>
        /// <param name="sourceRepository"></param>
        /// <param name="packageId"></param>
        /// <param name="version"></param>
        /// <param name="allowPrereleaseVersions"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Parse the NuGetVersion from string
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
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
        public static VersionRange GetSafeRange(NuGetVersion version, bool includePrerelease)
        {
            SemanticVersion max = new SemanticVersion(new Version(version.Major, version.Minor + 1));
            NuGetVersion maxVersion = NuGetVersion.Parse(max.ToString());
            return new VersionRange(version, true, maxVersion, false, includePrerelease);
        }

        /// <summary>
        /// Get the update version for Dependent package, based on the specification of Highest, HighestMinor, HighestPatch and Lowest.
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="identity"></param>
        /// <param name="updateVersion"></param>
        /// <param name="names"></param>
        /// <param name="allowPrerelease"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public static NuGetVersion GetUpdateVersionForDependentPackage(SourceRepository repo, PackageIdentity identity, DependencyVersion updateVersion, IEnumerable<FrameworkName> names, bool allowPrerelease,
            int skip = 0, int take = 30)
        {
            if (identity == null)
            {
                return null;
            }

            JObject package = GetJObjectForPackageId(repo, identity.Id, names, allowPrerelease, skip, take);
            IEnumerable<NuGetVersion> allVersions = GetAllVersionsForPackageId(package);
            // Find all versions that are higher than the package's current version
            allVersions = allVersions.Where(p => p > identity.Version).OrderByDescending(v => v);

            if (updateVersion == DependencyVersion.Lowest)
            {
                return allVersions.LastOrDefault();
            }
            else if (updateVersion == DependencyVersion.Highest)
            {
                return allVersions.FirstOrDefault();
            }
            else if (updateVersion == DependencyVersion.HighestPatch)
            {
                var groups = from p in allVersions
                             group p by new { p.Version.Major, p.Version.Minor } into g
                             orderby g.Key.Major, g.Key.Minor
                             select g;
                return (from p in groups.First()
                        orderby p.Version descending
                        select p).FirstOrDefault();
            }
            else if (updateVersion == DependencyVersion.HighestMinor)
            {
                var groups = from p in allVersions
                             group p by new { p.Version.Major } into g
                             orderby g.Key.Major
                             select g;
                return (from p in groups.First()
                        orderby p.Version descending
                        select p).FirstOrDefault();
            }

            throw new ArgumentOutOfRangeException("updateVersion");
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
