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

        public virtual IPackageRepository CreateRepository(PackageSource packageSource) {
            if (packageSource == null) {
                throw new ArgumentNullException("packageSource");
            }


            if (packageSource.IsAggregate) {
                throw new NotSupportedException();
            }

            Uri uri = new Uri(packageSource.Source);
            if (uri.IsFile) {
                return new LocalPackageRepository(uri.LocalPath);
            }
            // Make sure we get resolve any fwlinks before creating the repository
            return new DataServicePackageRepository(HttpWebRequestor.GetRedirectedUri(uri));
        }
    }
}
