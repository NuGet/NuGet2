using System;
using System.Globalization;
using NuGet.Resources;

namespace NuGet {
    public class PackageRepositoryFactory : IPackageRepositoryFactory {

        public virtual IPackageRepository CreateRepository(string packageSource) {
            if (packageSource == null) {
                throw new ArgumentNullException("packageSource");
            }

            Uri uri = new Uri(packageSource);
            if (uri.IsFile) {
                return new LocalPackageRepository(uri.LocalPath);
            }

            IHttpClient client = null;

            try {
                client = new RedirectedHttpClient(uri);
            }
            catch (Exception exception) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    NuGetResources.UnavailablePackageSource, packageSource),
                    exception);
            }

            // Make sure we get resolve any fwlinks before creating the repository
            return new DataServicePackageRepository(client);
        }
    }
}
