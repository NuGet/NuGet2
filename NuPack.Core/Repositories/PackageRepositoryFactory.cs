namespace NuPack {
    using System;
    using System.IO;
    using Microsoft.Internal.Web.Utils;

    public static class PackageRepositoryFactory {
        public static IPackageRepository CreateRepository(string feedOrPath) {
            if (String.IsNullOrEmpty(feedOrPath)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "feedOrPath");
            }

            if (IsLocalPath(feedOrPath)) {
                return new LocalPackageRepository(feedOrPath);
            }
            return new AtomFeedPackageRepository(new Uri(feedOrPath));
        }

        private static bool IsLocalPath(string feedOrPath) {
            return Path.IsPathRooted(feedOrPath);
        }
    }
}