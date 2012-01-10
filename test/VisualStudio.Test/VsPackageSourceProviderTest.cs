using System.Collections.Generic;
using System.Linq;
using NuGet.Test;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    public class VsPackageSourceProviderTest
    {
        [Fact]
        public void CtorIfFirstRunningAddsDefaultSource()
        {
            // Arrange
            var userSettings = new MockUserSettingsManager();
            var packageSourceProvider = CreateDefaultSourceProvider(userSettings);
            var provider = new VsPackageSourceProvider(userSettings, packageSourceProvider);

            // Act
            var sources = provider.LoadPackageSources().ToList();


            // Assert
            Assert.Equal(1, sources.Count);
            Assert.Equal("https://nuget.org/api/v2/", sources[0].Source);
        }

        [Fact]
        public void CtorMigrateV1FeedToV2Feed()
        {
            // Arrange
            var userSettings = new MockUserSettingsManager();
            userSettings.SetValue(
                PackageSourceProvider.PackageSourcesSectionName,
                "NuGet official package source",
                "https://go.microsoft.com/fwlink/?LinkID=206669");
            var sourceProvider = CreateDefaultSourceProvider(userSettings);
            var provider = new VsPackageSourceProvider(userSettings, sourceProvider);

            // Act
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(1, sources.Count);
            Assert.Equal("https://nuget.org/api/v2/", sources[0].Source);
        }

        [Fact]
        public void CtorMigrateV2LegacyFeedToV2Feed()
        {
            // Arrange
            var userSettings = new MockUserSettingsManager();
            userSettings.SetValue(
                PackageSourceProvider.PackageSourcesSectionName,
                "NuGet official package source",
                "https://go.microsoft.com/fwlink/?LinkID=230477");
            var sourceProvider = CreateDefaultSourceProvider(userSettings);
            var provider = new VsPackageSourceProvider(userSettings, sourceProvider);

            // Act
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(1, sources.Count);
            Assert.Equal("https://nuget.org/api/v2/", sources[0].Source);
        }

        [Fact]
        public void CtorMigrateV1FeedToV2FeedAndPreserveIsEnabledProperty()
        {
            // Arrange
            var userSettings = new MockUserSettingsManager();
            userSettings.SetValue(
                PackageSourceProvider.PackageSourcesSectionName,
                "NuGet official package source",
                "https://go.microsoft.com/fwlink/?LinkID=206669");

            // disable the official source
            userSettings.SetValue(
                PackageSourceProvider.DisabledPackageSourcesSectionName,
                "NuGet official package source",
                "true");

            var provider = new VsPackageSourceProvider(userSettings);

            // Act
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(1, sources.Count);
            Assert.Equal("https://nuget.org/api/v2/", sources[0].Source);
            Assert.False(sources[0].IsEnabled);
        }

        [Fact]
        public void PreserveActiveSourceWhileMigratingNuGetFeed()
        {
            // Arrange
            var userSettings = new MockUserSettingsManager();
            userSettings.SetValues(
                PackageSourceProvider.PackageSourcesSectionName,
                new KeyValuePair<string, string>[] {
                    new KeyValuePair<string, string>("NuGet official package source", "https://go.microsoft.com/fwlink/?LinkID=206669"),
                    new KeyValuePair<string, string>("one", "onesource"),
                }
            );
            userSettings.SetValue(VsPackageSourceProvider.ActivePackageSourceSectionName, "one", "onesource");

            var provider = new VsPackageSourceProvider(userSettings);

            // Act
            var activeSource = provider.ActivePackageSource;

            // Assert
            AssertPackageSource(activeSource, "one", "onesource");
        }

        [Fact]
        public void CtorAddsAggregrateIfNothingWasPersistedIntoSettingsManager()
        {
            // Arrange
            var userSettings = new MockUserSettingsManager();
            var packageSourceProvider = CreateDefaultSourceProvider(userSettings);
            var provider = new VsPackageSourceProvider(userSettings, packageSourceProvider);

            // Act
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(1, sources.Count);
            Assert.Equal("NuGet official package source", sources[0].Name);
        }

        [Fact]
        public void MigrateActivePackageSourceToV2()
        {
            // Arrange
            var settings = new MockUserSettingsManager();
            var provider = new VsPackageSourceProvider(settings);
            settings.SetValue(
                VsPackageSourceProvider.ActivePackageSourceSectionName,
                "NuGet official package source",
                "https://go.microsoft.com/fwlink/?LinkID=206669");

            // Act
            PackageSource activePackageSource = provider.ActivePackageSource;

            // Assert
            AssertPackageSource(activePackageSource, "NuGet official package source", "https://nuget.org/api/v2/");
        }

        [Fact]
        public void SetActivePackageSourcePersistsItToSettingsManager()
        {
            // Arrange
            var userSettings = new MockUserSettingsManager();
            var packageSourceProvider = CreateDefaultSourceProvider(userSettings);
            packageSourceProvider.SavePackageSources(new[] { new PackageSource("source", "name"), new PackageSource("source1", "name1") });
            var provider = new VsPackageSourceProvider(userSettings, packageSourceProvider);

            // Act
            provider.ActivePackageSource = new PackageSource("source", "name");

            // Assert
            var activeValue = userSettings.GetValue(VsPackageSourceProvider.ActivePackageSourceSectionName, "name");
            Assert.Equal("source", activeValue);

            var invalidActiveValue = userSettings.GetValue(VsPackageSourceProvider.ActivePackageSourceSectionName, "invalidName");
            Assert.Null(invalidActiveValue);
        }

        [Fact]
        public void SettingActivePackageSourceToNonExistantSourceThrows()
        {
            // Arrange
            var userSettings = new MockUserSettingsManager();
            var packageSourceProvider = CreateDefaultSourceProvider(userSettings);
            var provider = new VsPackageSourceProvider(userSettings, packageSourceProvider);

            // Act
            ExceptionAssert.ThrowsArgumentException(() => provider.ActivePackageSource = new PackageSource("a", "a"), "value", "The package source does not belong to the collection of available sources.");
        }

        [Fact]
        public void SettingsWithMoreThanOneAggregateSourceAreModifiedToNotHaveOne()
        {
            // Arrange
            var userSettings = new MockUserSettingsManager();
            var packageSourceProvider = CreateDefaultSourceProvider(userSettings);
            var provider = new VsPackageSourceProvider(userSettings, packageSourceProvider);

            // Act
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(1, sources.Count);
            Assert.Equal("NuGet official package source", sources[0].Name);
        }

        private static void AssertPackageSource(PackageSource ps, string name, string source)
        {
            Assert.Equal(name, ps.Name);
            Assert.Equal(source, ps.Source);
        }

        private static PackageSourceProvider CreateDefaultSourceProvider(ISettings settings)
        {
            return new PackageSourceProvider(settings, VsPackageSourceProvider.DefaultSources, VsPackageSourceProvider.FeedsToMigrate);
        }
    }
}