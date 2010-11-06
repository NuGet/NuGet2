using System;
using Microsoft.Internal.Web.Utils;

namespace NuGet {
    public class PackageRepositoryFactory : IPackageRepositoryFactory {
        private static readonly IPackageRepositoryFactory _default = new PackageRepositoryFactory();

        public static IPackageRepositoryFactory Default {
            get {
                return _default;
            }
        }

        public IPackageRepository CreateRepository(PackageSource source) {
            if (source == null) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageSource");
            }

            Uri uri = new Uri(source.Source);
            if (uri.IsFile) {
                return new LocalPackageRepository(uri.LocalPath);
            }
            // Make sure we get resolve any fwlinks before creating the repository
            return new DataServicePackageRepository(HttpWebRequestor.GetRedirectedUri(uri));
        }
    }
}
