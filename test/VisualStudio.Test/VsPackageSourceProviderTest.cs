using System.Collections.Generic;
using System.Linq;
using NuGet.Test;
using Xunit;

namespace NuGet.VisualStudio.Test
{

    public class VsPackageSourceProviderTest
    {
        [Fact]
        public void AddSourceThrowsIfSourceIsNull()
        {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var packageSourceProvider = new MockPackageSourceProvider();
            var provider = new VsPackageSourceProvider(userSettingsManager, packageSourceProvider);

            // Act
            ExceptionAssert.ThrowsArgNull(() => provider.AddPackageSource(null), "source");
        }

        [Fact]
        public void CtorIfFirstRunningAddsDefaultSource()
        {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var packageSourceProvider = new MockPackageSourceProvider();
            var provider = new VsPackageSourceProvider(userSettingsManager, packageSourceProvider);

            // Act
            var sources = provider.LoadPackageSources().ToList();


            // Assert
            Assert.Equal(1, sources.Count);
            Assert.Equal("https://go.microsoft.com/fwlink/?LinkID=230477", sources[0].Source);
        }

        [Fact]
        public void CtorMigrateV1FeedToV2Feed()
        {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            userSettingsManager.SetValue(
                PackageSourceProvider.PackageSourcesSectionName,
                "NuGet official package source",
                "https://go.microsoft.com/fwlink/?LinkID=206669");

            var provider = new VsPackageSourceProvider(userSettingsManager);

            // Act
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(1, sources.Count);
            Assert.Equal("https://go.microsoft.com/fwlink/?LinkID=230477", sources[0].Source);
        }

        [Fact]
        public void CtorMigrateV1FeedToV2FeedAndPreserveIsEnabledProperty()
        {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            userSettingsManager.SetValue(
                PackageSourceProvider.PackageSourcesSectionName,
                "NuGet official package source",
                "https://go.microsoft.com/fwlink/?LinkID=206669");

            // disable the official source
            userSettingsManager.SetValue(
                PackageSourceProvider.DisabledPackageSourcesSectionName,
                "NuGet official package source",
                "true");

            var provider = new VsPackageSourceProvider(userSettingsManager);

            // Act
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(1, sources.Count);
            Assert.Equal("https://go.microsoft.com/fwlink/?LinkID=230477", sources[0].Source);
            Assert.False(sources[0].IsEnabled);
        }

        [Fact]
        public void PreserveActiveSourceWhileMigratingNuGetFeed()
        {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            userSettingsManager.SetValues(
                PackageSourceProvider.PackageSourcesSectionName,
                new KeyValuePair<string, string>[] {
                    new KeyValuePair<string, string>("NuGet official package source", "https://go.microsoft.com/fwlink/?LinkID=206669"),
                    new KeyValuePair<string, string>("one", "onesource"),
                }
            );
            userSettingsManager.SetValue(VsPackageSourceProvider.ActivePackageSourceSectionName, "one", "onesource");

            var provider = new VsPackageSourceProvider(userSettingsManager);

            // Act
            var activeSource = provider.ActivePackageSource;

            // Assert
            AssertPackageSource(activeSource, "one", "onesource");
        }

        [Fact]
        public void CtorAddsAggregrateIfNothingWasPersistedIntoSettingsManager()
        {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var packageSourceProvider = new MockPackageSourceProvider();
            var provider = new VsPackageSourceProvider(userSettingsManager, packageSourceProvider);

            // Act
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(1, sources.Count);
            Assert.Equal("NuGet official package source", sources[0].Name);
        }

        [Fact]
        public void AddSourceSetsPersistsSourcesToSettingsManager()
        {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var packageSourceProvider = new MockPackageSourceProvider();
            var provider = new VsPackageSourceProvider(userSettingsManager, packageSourceProvider);

            // Act
            for (int i = 0; i < 10; i++)
            {
                provider.AddPackageSource(new PackageSource("source" + i, "name" + i));
            }

            // Assert
            var values = packageSourceProvider.LoadPackageSources().ToList();

            // 11 = 10 package sources that we added + NuGet official source
            Assert.Equal(11, values.Count);
            Assert.Equal(Resources.VsResources.OfficialSourceName, values[0].Name);
            for (int i = 0; i < 10; i++)
            {
                AssertPackageSource(values[i + 1], "name" + i, "source" + i);
            }
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
            AssertPackageSource(activePackageSource, "NuGet official package source", "https://go.microsoft.com/fwlink/?LinkID=230477");
        }

        [Fact]
        public void SetActivePackageSourcePersistsItToSettingsManager()
        {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var packageSourceProvider = new MockPackageSourceProvider();
            packageSourceProvider.SavePackageSources(new[] { new PackageSource("source", "name"), new PackageSource("source1", "name1") });
            var provider = new VsPackageSourceProvider(userSettingsManager, packageSourceProvider);

            // Act
            provider.ActivePackageSource = new PackageSource("source", "name");

            // Assert
            var activeValue = userSettingsManager.GetValue(VsPackageSourceProvider.ActivePackageSourceSectionName, "name");
            Assert.Equal("source", activeValue);

            var invalidActiveValue = userSettingsManager.GetValue(VsPackageSourceProvider.ActivePackageSourceSectionName, "invalidName");
            Assert.Null(invalidActiveValue);
        }

        [Fact]
        public void RemoveSourceThrowsIfSourceIsNull()
        {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var packageSourceProvider = new MockPackageSourceProvider();
            var provider = new VsPackageSourceProvider(userSettingsManager, packageSourceProvider);

            // Act
            ExceptionAssert.ThrowsArgNull(() => provider.RemovePackageSource(null), "source");
        }

        [Fact]
        public void RemovingUnknownPackageSourceReturnsFalse()
        {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var packageSourceProvider = new MockPackageSourceProvider();
            var provider = new VsPackageSourceProvider(userSettingsManager, packageSourceProvider);

            // Act
            bool result = provider.RemovePackageSource(new PackageSource("a", "a"));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void RemovingExistingPackageSourceReturnsFalse()
        {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var packageSource = new PackageSource("a", "a");
            var packageSourceProvider = new MockPackageSourceProvider();
            packageSourceProvider.SavePackageSources(new[] { packageSource });
            var provider = new VsPackageSourceProvider(userSettingsManager, packageSourceProvider);

            // Act
            bool result = provider.RemovePackageSource(packageSource);

            // Assert
            Assert.True(result);

            // values should be null because we don't persist aggregate source into user settings file
            var values = userSettingsManager.GetValues(PackageSourceProvider.PackageSourcesSectionName);
            Assert.Null(values);
        }

        [Fact]
        public void RemovingActivePackageSourceSetsActivePackageSourceToNull()
        {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var packageSourceProvider = new MockPackageSourceProvider();
            var provider = new VsPackageSourceProvider(userSettingsManager, packageSourceProvider);
            var packageSource = new PackageSource("a", "a");
            provider.SavePackageSources(new[] { packageSource });
            provider.ActivePackageSource = packageSource;

            // Act
            bool result = provider.RemovePackageSource(packageSource);

            // Assert
            Assert.True(result);
            Assert.Null(provider.ActivePackageSource);

            // values should be null because we don't persist aggregate source into user settings file
            var values = userSettingsManager.GetValues(PackageSourceProvider.PackageSourcesSectionName);
            Assert.Null(values);
        }

        [Fact]
        public void SettingActivePackageSourceToNonExistantSourceThrows()
        {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var packageSourceProvider = new MockPackageSourceProvider();
            var provider = new VsPackageSourceProvider(userSettingsManager, packageSourceProvider);

            // Act
            ExceptionAssert.ThrowsArgumentException(() => provider.ActivePackageSource = new PackageSource("a", "a"), "value", "The package source does not belong to the collection of available sources.");
        }

        [Fact]
        public void SettingsWithMoreThanOneAggregateSourceAreModifiedToNotHaveOne()
        {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var packageSourceProvider = new MockPackageSourceProvider();
            var provider = new VsPackageSourceProvider(userSettingsManager, packageSourceProvider);

            // Act
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(1, sources.Count);
            Assert.Equal("NuGet official package source", sources[0].Name);
        }

        private void AssertPackageSource(PackageSource ps, string name, string source)
        {
            Assert.Equal(name, ps.Name);
            Assert.Equal(source, ps.Source);
        }

    }
}