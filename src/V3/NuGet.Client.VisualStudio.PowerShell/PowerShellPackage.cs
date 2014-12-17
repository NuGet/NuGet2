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
    public class PowerShellPackage
    {
        public string Id { get; set; }

        public NuGetVersion Version { get; set; }

        public string Description { get; set; }

        public static List<PowerShellPackage> GetPowerShellPackageView(IEnumerable<JObject> metadata)
        {
            List<PowerShellPackage> view = new List<PowerShellPackage>();
            foreach (JObject json in metadata)
            {
                PowerShellPackage package = new PowerShellPackage();
                package.Id = json.Value<string>(Properties.PackageId);

                // TODO: Update the logic here.
                NuGetVersion nVersion = NuGetVersion.Parse(json.Value<string>(Properties.LatestVersion));
                if (nVersion == null)
                {
                    nVersion = NuGetVersion.Parse(json.Value<string>(Properties.Version));
                }
                else
                {
                    package.Version = NuGetVersion.Parse(nVersion.ToNormalizedString());
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
        public static IEnumerable<JObject> GetAllVersionsForPackage(SourceRepository repo, string packageId, IEnumerable<FrameworkName> names, bool allowPrerelease, int skip, int take)
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

        /// <summary>
        /// Return the latest version of packages that match the search tearm of packageID
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="packageId"></param>
        /// <param name="names"></param>
        /// <param name="allowPrerelease"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public static IEnumerable<JObject> GetLastestPackages(SourceRepository repo, string packageId, IEnumerable<FrameworkName> names, bool allowPrerelease, int skip, int take)
        {
            // Specify the search filter for prerelease and target framework
            SearchFilter filter = new SearchFilter();
            filter.IncludePrerelease = allowPrerelease;
            filter.SupportedFrameworks = names;

            IEnumerable<JObject> packages = Enumerable.Empty<JObject>();
            try
            {
                Task<IEnumerable<JObject>> task = repo.Search(packageId, filter, skip, take, cancellationToken: CancellationToken.None);
                packages = task.Result;
            }
            catch (Exception)
            {
                if (packages.IsEmpty())
                {
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture,
                        NuGetResources.UnknownPackage, packageId));
                }
            }
            return packages;
        }

        public static string GetLastestVersionForPackage(SourceRepository repo, string packageId, IEnumerable<FrameworkName> names, bool allowPrerelease, 
            NuGetVersion nugetVersion = null, bool isSafe = false, int skip = 0, int take = 30)
        {
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
        public static JObject GetLastestJObjectForPackage(SourceRepository repo, PackageIdentity identity, VsProject project, bool allowPrerelease, bool isSafe)
        {
            string latestVersion = GetLastestVersionForPackage(repo, identity.Id, project.GetSupportedFrameworks(), allowPrerelease, identity.Version, isSafe);
            JObject latestJObject = null;
            if (latestVersion != null)
            {
                Task<JObject> task = repo.GetPackageMetadata(identity.Id, identity.Version);
                latestJObject = task.Result;
            }
            return latestJObject;
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
}
