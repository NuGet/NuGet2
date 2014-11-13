using Newtonsoft.Json.Linq;
using NuGet.Resources;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace NuGet.Client.VisualStudio.PowerShell
{
    public class PowerShellPackageViewModel
    {
        public string Id { get; set; }

        public NuGetVersion Version { get; set; }

        public string Description { get; set; }

        public static List<PowerShellPackageViewModel> GetPowerShellPackageView(IEnumerable<JObject> metadata)
        {
            List<PowerShellPackageViewModel> view = new List<PowerShellPackageViewModel>();
            foreach (JObject json in metadata)
            {
                PowerShellPackageViewModel model = new PowerShellPackageViewModel();
                model.Id = json.Value<string>(Properties.PackageId);
                model.Version = NuGetVersion.Parse(json.Value<string>(Properties.Version));
                model.Description = json.Value<string>(Properties.Description);
                if (string.IsNullOrEmpty(model.Description))
                {
                    model.Description = json.Value<string>(Properties.Summary);
                }
                view.Add(model);
            }
            return view;
        }

        // TODO List
        // 1. The unlisted packages are not filtered out. The plan is that Server will return unlisted packages.
        // Test EntityFramework 7.0.0-beta1 is not installed when specify -pre.
        // 2. GetLastestVersionForPackage supports local repository such as UNC share.
        public static string GetLastestVersionForPackage(SourceRepository repo, string packageId, bool allowPrerelease)
        {
            string version = String.Empty;
            try
            {
                Task<IEnumerable<JObject>> packages = repo.GetPackageMetadataById(packageId);
                var r = packages.Result;
                var allVersions = r.Select(p => NuGetVersion.Parse(p.Value<string>(Properties.Version)));
                if (!allowPrerelease)
                {
                    allVersions = allVersions.Where(p => !p.IsPrerelease);
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
        /// The safe range is defined as the highest build and revision for a given major and minor version
        /// </summary>
        public static IVersionSpec GetSafeRange(SemanticVersion version)
        {
            return new VersionSpec
            {
                IsMinInclusive = true,
                MinVersion = version,
                MaxVersion = new SemanticVersion(new Version(version.Version.Major, version.Version.Minor + 1))
            };
        }

        public static bool IsPrereleaseVersion(string version)
        {
            SemanticVersion sVersion = new SemanticVersion(version);
            bool isPrerelease = !String.IsNullOrEmpty(sVersion.SpecialVersion);
            return isPrerelease;
        }
    }
}
