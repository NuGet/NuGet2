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
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider, new Mock<IVsShellInfo>().Object);

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
            var provider = new VsPackageSourceProvider(userSettings.Object, sourceProvider, new Mock<IVsShellInfo>().Object);

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
            var provider = new VsPackageSourceProvider(userSettings.Object, sourceProvider, new Mock<IVsShellInfo>().Object);

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

            var provider = new VsPackageSourceProvider(userSettings.Object, new Mock<IVsShellInfo>().Object);

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

            var provider = new VsPackageSourceProvider(userSettings.Object, new Mock<IVsShellInfo>().Object);

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
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider, new Mock<IVsShellInfo>().Object);

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
            var provider = new VsPackageSourceProvider(settings.Object, new Mock<IVsShellInfo>().Object);

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
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider.Object, new Mock<IVsShellInfo>().Object);

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
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider, new Mock<IVsShellInfo>().Object);

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
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider, new Mock<IVsShellInfo>().Object);

            // Act
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(1, sources.Count);
            Assert.Equal("NuGet official package source", sources[0].Name);
        }

        [Fact]
        public void GetActivePackageSourceWillSwapInTheWindows8ExpressSourceWhenRunningWindows8Express()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            userSettings.Setup(_ => _.GetValues(It.IsAny<string>())).Returns(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("NuGet official package source", "https://nuget.org/api/v2/")
            });
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            var vsShellInfo = new Mock<IVsShellInfo>();
            vsShellInfo.Setup(_ => _.IsVisualStudioExpressForWindows8).Returns(true);
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider.Object, vsShellInfo.Object);

            // Act
            var source = provider.ActivePackageSource;

            // Assert
            Assert.Equal("Visual Studio Express for Windows 8 official package source", source.Name);
        }

        [Fact]
        public void GetActivePackageSourceWillNotSwapInTheWindows8ExpressSourceWhenNotRunningWindows8Express()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            userSettings.Setup(_ => _.GetValues(It.IsAny<string>())).Returns(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("NuGet official package source", "theActiveSource")
            });
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            var vsShellInfo = new Mock<IVsShellInfo>();
            vsShellInfo.Setup(_ => _.IsVisualStudioExpressForWindows8).Returns(false);
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider.Object, vsShellInfo.Object);

            // Act
            var source = provider.ActivePackageSource;

            // Assert
            Assert.Equal("NuGet official package source", source.Name);
        }

        [Fact]
        public void SetActivePackageSourceWillSwapInTheDefaultSourceWhenRunningWindows8Express()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            packageSourceProvider.Setup(_ => _.LoadPackageSources()).Returns(new[]
            {
                new PackageSource("theFirstSource", "theFirstFeed"),
                new PackageSource("https://nuget.org/api/v2/", "NuGet official package source"),
                new PackageSource("theThirdSource", "theThirdFeed"),
            });
            var vsShellInfo = new Mock<IVsShellInfo>();
            vsShellInfo.Setup(_ => _.IsVisualStudioExpressForWindows8).Returns(true);
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider.Object, vsShellInfo.Object);

            // Act
            provider.ActivePackageSource = new PackageSource(NuGetConstants.VisualStudioExpressForWindows8FeedUrl, "Visual Studio Express for Windows 8 official package source");

            // Assert
            userSettings.Verify(_ => _.SetValue(It.IsAny<string>(), "NuGet official package source", It.IsAny<string>()));
        }

        [Fact]
        public void SetActivePackageSourceWillNotSwapInTheDefaultSourceWhenNotRunningWindows8Express()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            packageSourceProvider.Setup(_ => _.LoadPackageSources()).Returns(new[]
            {
                new PackageSource("theFirstSource", "theFirstFeed"),
                new PackageSource("https://nuget.org/api/v2/curated-feeds/express-for-windows8/", "Visual Studio Express for Windows 8 official package source"),
                new PackageSource("theThirdSource", "theThirdFeed"),
            });
            var vsShellInfo = new Mock<IVsShellInfo>();
            vsShellInfo.Setup(_ => _.IsVisualStudioExpressForWindows8).Returns(false);
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider.Object, vsShellInfo.Object);

            // Act
            provider.ActivePackageSource = new PackageSource(NuGetConstants.VisualStudioExpressForWindows8FeedUrl, "Visual Studio Express for Windows 8 official package source");

            // Assert
            userSettings.Verify(_ => _.SetValue(It.IsAny<string>(), "Visual Studio Express for Windows 8 official package source", It.IsAny<string>()));
        }

        [Fact]
        public void LoadPackageSourcesWillSwapInTheWindows8SourceForTheDefaultSourceWhenRunningWindows8Express()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            packageSourceProvider.Setup(_ => _.LoadPackageSources()).Returns(new[]
            {
                new PackageSource("theFirstSource", "theFirstFeed"),
                new PackageSource("https://nuget.org/api/v2/", "NuGet official package source"),
                new PackageSource("theThirdSource", "theThirdFeed"),
            });
            var vsShellInfo = new Mock<IVsShellInfo>();
            vsShellInfo.Setup(_ => _.IsVisualStudioExpressForWindows8).Returns(true);
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider.Object, vsShellInfo.Object);

            // Act
            var sources = provider.LoadPackageSources();

            // Assert
            Assert.Equal("Visual Studio Express for Windows 8 official package source", sources.ElementAt(1).Name);
        }

        [Fact]
        public void LoadPackageSourcesWillNotSwapInTheWindows8SourceForTheDefaultSourceWhenNotRunningWindows8Express()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            packageSourceProvider.Setup(_ => _.LoadPackageSources()).Returns(new[]
            {
                new PackageSource("theFirstSource", "theFirstFeed"),
                new PackageSource("https://nuget.org/api/v2/", "NuGet official package source"),
                new PackageSource("theThirdSource", "theThirdFeed"),
            });
            var vsShellInfo = new Mock<IVsShellInfo>();
            vsShellInfo.Setup(_ => _.IsVisualStudioExpressForWindows8).Returns(false);
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider.Object, vsShellInfo.Object);

            // Act
            var sources = provider.LoadPackageSources();

            // Assert
            Assert.Equal("NuGet official package source", sources.ElementAt(1).Name);
        }

        [Fact]
        public void SavePackageSourcesWillSwapInTheDefaultFeedForTheWindows8ExpressFeedWhenRunningWindows8Express()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            var vsShellInfo = new Mock<IVsShellInfo>();
            vsShellInfo.Setup(_ => _.IsVisualStudioExpressForWindows8).Returns(true);
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider.Object, vsShellInfo.Object);

            // Act
            provider.SavePackageSources(new[]
            {
                new PackageSource("theFirstSource", "theFirstFeed"),
                new PackageSource("https://nuget.org/api/v2/curated-feeds/express-for-windows8/", "Visual Studio Express for Windows 8 official package source"){ IsOfficial = true },
                new PackageSource("theThirdSource", "theThirdFeed"),
            });

            // Assert
            packageSourceProvider.Verify(_ => _.SavePackageSources(It.Is<IEnumerable<PackageSource>>(packageSources => packageSources.ElementAt(1).Name == "NuGet official package source")));
        }

        [Fact]
        public void SavePackageSourcesWillNotSwapInTheDefaultFeedForTheWindows8ExpressFeedWhenNotRunningWindows8Express()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            var vsShellInfo = new Mock<IVsShellInfo>();
            vsShellInfo.Setup(_ => _.IsVisualStudioExpressForWindows8).Returns(false);
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider.Object, vsShellInfo.Object);

            // Act
            provider.SavePackageSources(new[]
            {
                new PackageSource("theFirstSource", "theFirstFeed"),
                new PackageSource("https://nuget.org/api/v2/curated-feeds/express-for-windows8/", "Visual Studio Express for Windows 8 official package source"){ IsOfficial = true },
                new PackageSource("theThirdSource", "theThirdFeed"),
            });

            // Assert
            packageSourceProvider.Verify(_ => _.SavePackageSources(It.Is<IEnumerable<PackageSource>>(packageSources => packageSources.ElementAt(1).Name == "Visual Studio Express for Windows 8 official package source")));
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