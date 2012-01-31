using System.Collections.Generic;
using System.Linq;
using Moq;
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
            var userSettings = new Mock<ISettings>();
            var packageSourceProvider = CreateDefaultSourceProvider(userSettings.Object);
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider);

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
            var userSettings = new Mock<ISettings>();
            userSettings.Setup(s => s.GetValues("packageSources"))
                    .Returns(new[] { new KeyValuePair<string, string>("NuGet official package source", "https://go.microsoft.com/fwlink/?LinkID=206669") });
            var sourceProvider = CreateDefaultSourceProvider(userSettings.Object);
            var provider = new VsPackageSourceProvider(userSettings.Object, sourceProvider);

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
            var userSettings = new Mock<ISettings>();
            userSettings.Setup(s => s.GetValues("packageSources"))
                        .Returns(new[] { new KeyValuePair<string, string>("NuGet official package source", "https://go.microsoft.com/fwlink/?LinkID=230477") });
            var sourceProvider = CreateDefaultSourceProvider(userSettings.Object);
            var provider = new VsPackageSourceProvider(userSettings.Object, sourceProvider);

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
            var userSettings = new Mock<ISettings>();
            userSettings.Setup(s => s.GetValues("packageSources"))
                        .Returns(new[] { new KeyValuePair<string, string>("NuGet official package source", "https://go.microsoft.com/fwlink/?LinkID=206669") });

            // disable the official source
            userSettings.Setup(s => s.GetValues("disabledPackageSources"))
                        .Returns(new[] { new KeyValuePair<string, string>("NuGet official package source", "true") });

            var provider = new VsPackageSourceProvider(userSettings.Object);

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
            var userSettings = new Mock<ISettings>();
            userSettings.Setup(s => s.GetValues("packageSources")).Returns(
                new[] {
                    new KeyValuePair<string, string>("NuGet official package source", "https://go.microsoft.com/fwlink/?LinkID=206669"),
                    new KeyValuePair<string, string>("one", "onesource"),
                });
            userSettings.Setup(s => s.GetValues("activePackageSource"))
                        .Returns(new[] { new KeyValuePair<string, string>("one", "onesource") });

            var provider = new VsPackageSourceProvider(userSettings.Object);

            // Act
            var activeSource = provider.ActivePackageSource;

            // Assert
            AssertPackageSource(activeSource, "one", "onesource");
        }

        [Fact]
        public void CtorAddsAggregrateIfNothingWasPersistedIntoSettingsManager()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            var packageSourceProvider = CreateDefaultSourceProvider(userSettings.Object);
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider);

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
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValue("activePackageSource", "NuGet official package source"))
                    .Returns("https://go.microsoft.com/fwlink/?LinkID=206669");
            var provider = new VsPackageSourceProvider(settings.Object);

            // Act
            PackageSource activePackageSource = provider.ActivePackageSource;

            // Assert
            AssertPackageSource(activePackageSource, "NuGet official package source", "https://nuget.org/api/v2/");
        }

        [Fact]
        public void SetActivePackageSourcePersistsItToSettingsManager()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            userSettings.Setup(s => s.SetValue("activePackageSource", "name", "source")).Verifiable();

            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            packageSourceProvider.Setup(s => s.LoadPackageSources())
                                 .Returns(new[] { new PackageSource("source", "name"), new PackageSource("source1", "name1") });
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider.Object);

            // Act
            provider.ActivePackageSource = new PackageSource("source", "name");

            // Assert
            userSettings.Verify();
        }

        [Fact]
        public void SettingActivePackageSourceToNonExistantSourceThrows()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            var packageSourceProvider = CreateDefaultSourceProvider(userSettings.Object);
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider);

            // Act
            ExceptionAssert.ThrowsArgumentException(() => provider.ActivePackageSource = new PackageSource("a", "a"), "value",
                "The package source does not belong to the collection of available sources.");
        }

        [Fact]
        public void SettingsWithMoreThanOneAggregateSourceAreModifiedToNotHaveOne()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            var packageSourceProvider = CreateDefaultSourceProvider(userSettings.Object);
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider);

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