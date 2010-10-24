using System;
using Microsoft.Internal.Web.Utils;

namespace NuPack {
    public class PackageRepositoryFactory : IPackageRepositoryFactory {
        private static readonly IPackageRepositoryFactory _default = new PackageRepositoryFactory();

        public static IPackageRepositoryFactory Default {
            get {
                return _default;
            }
        }

        public IPackageRepository CreateRepository(string source) {
            if (String.IsNullOrEmpty(source)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "source");
            }

            Uri uri = new Uri(source);
            if (uri.IsFile) {
                return new LocalPackageRepository(uri.LocalPath);
            }
            return new DataServicePackageRepository(uri);
        }
    }
}