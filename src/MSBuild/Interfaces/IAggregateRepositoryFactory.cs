using System.Collections.Generic;

namespace NuGet.MSBuild {
    public interface IAggregateRepositoryFactory {
        IPackageRepository createSpecificSettingsRepository(string nugetConfigPath);
        IPackageRepository createDefaultSettingsRepository();
        IPackageRepository createSpecificFeedsRepository(bool ignoreFailedRepositories, IEnumerable<string> feedUrls);
    }
}