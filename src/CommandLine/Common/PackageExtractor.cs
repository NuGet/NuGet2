using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Common
{
    internal static class PackageExtractor
    {
        /// <summary>
        /// Install a package with locking to allow multiple concurrent extractions to work without disk contention.
        /// </summary>
        public static void InstallPackage(IPackageManager packageManager, IPackage package)
        {
            var uniqueToken = GenerateUniqueToken(packageManager, package.Id, package.Version);
            // Prerelease flag does not matter since we already have the package to install and we ignore dependencies.
            ExecuteLocked(uniqueToken, () => packageManager.InstallPackage(package, ignoreDependencies: true, allowPrereleaseVersions: true));
        }

        /// <summary>
        /// We want to base the lock name off of the full path of the package, however, the Mutex looks for files on disk if a path is given.
        /// Additionally, it also fails if the string is longer than 256 characters. Therefore we obtain a base-64 encoded hash of the path.
        /// </summary>
        /// <seealso cref="http://social.msdn.microsoft.com/forums/en-us/clr/thread/D0B3BF82-4D23-47C8-8706-CC847157AC81"/>
        private static string GenerateUniqueToken(IPackageManager packageManager, string packageId, SemanticVersion version)
        {
            var packagePath = packageManager.FileSystem.GetFullPath(packageManager.PathResolver.GetPackageFileName(packageId, version));
            var pathBytes = Encoding.UTF8.GetBytes(packagePath);
            var hashProvider = new CryptoHashProvider("SHA256");

            return Convert.ToBase64String(hashProvider.CalculateHash(pathBytes)).ToUpperInvariant();
        }

        private static void ExecuteLocked(string name, Action action)
        {
            bool created;
            using (var mutex = new Mutex(initiallyOwned: true, name: name, createdNew: out created))
            {
                try
                {
                    // We need to ensure only one instance of the executable performs the install. All other instances need to wait 
                    // for the package to be installed. We'd cap the waiting duration so that other instances aren't waiting indefinitely.
                    if (created)
                    {
                        action();
                    }
                    else
                    {
                        mutex.WaitOne(TimeSpan.FromMinutes(2));
                    }
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }
    }
}
