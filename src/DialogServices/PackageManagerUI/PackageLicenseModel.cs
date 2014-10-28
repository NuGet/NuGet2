using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Dialog.PackageManagerUI
{
    public class PackageLicenseModel
    {
        public string Id { get; private set; }
        public IEnumerable<string> Authors { get; private set; }
        public Uri LicenseUrl { get; private set; }

        public PackageLicenseModel(string id, Uri licenseUrl, IEnumerable<string> authors)
        {
            Id = id;
            LicenseUrl = licenseUrl;
            Authors = authors;
        }

        public static PackageLicenseModel FromV2Package(IPackageMetadata package)
        {
            return new PackageLicenseModel(
                package.Id,
                package.LicenseUrl,
                package.Authors);
        }

        public static IEnumerable<PackageLicenseModel> FromV2Packages(IEnumerable<IPackage> packages)
        {
            return packages.Select(FromV2Package);
        }
    }
}
