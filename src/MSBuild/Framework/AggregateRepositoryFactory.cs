using System;
using System.Collections.Generic;
using System.IO;

namespace NuGet.MSBuild
{
    public class AggregateRepositoryFactory : IAggregateRepositoryFactory {

        public IPackageRepository createSpecificSettingsRepository(string nugetConfigPath) {
            UserSettings settings;
            try {
                var fi = new FileInfo(nugetConfigPath);
                settings = new UserSettings(new PhysicalFileSystem(fi.DirectoryName), fi.Name, true);
            }
            catch (FileNotFoundException ex ) {
                throw new InvalidOperationException(ex.Message, ex);
            }
            PackageSourceProvider provider = new PackageSourceProvider(settings);
            PackageRepositoryFactory factory = PackageRepositoryFactory.Default;
            return provider.GetAggregate(factory);
        }

        public IPackageRepository createDefaultSettingsRepository() {
            PackageSourceProvider provider = PackageSourceProvider.Default;
            PackageRepositoryFactory factory = PackageRepositoryFactory.Default;
            return provider.GetAggregate(factory);
        }

        public IPackageRepository createSpecificFeedsRepository(bool ignoreFailingRepositories, IEnumerable<string> feeds) {
            PackageSourceProvider provider = PackageSourceProvider.Default;
            PackageRepositoryFactory factory = PackageRepositoryFactory.Default;
            return provider.GetAggregate(factory, ignoreFailingRepositories, feeds);
        }
    }
}