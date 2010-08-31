namespace NuPack {
    using System.IO;
    using System;

    public static class PackageRepositoryFactory {
        public static IPackageRepository CreateRepository(string feedOrPath) {
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