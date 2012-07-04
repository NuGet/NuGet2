using System;
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
        public void LoadPackageSourcesAddOfficialSourceIfMissing()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            userSettings.Setup(s => s.GetValues("packageSources"))
                        .Returns(new[] { new KeyValuePair<string, string>("my source", "http://nuget.org") });
            var sourceProvider = CreateDefaultSourceProvider(userSettings.Object);
            var provider = new VsPackageSourceProvider(userSettings.Object, sourceProvider, new Mock<IVsShellInfo>().Object);

            // Act
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(2, sources.Count);
            AssertPackageSource(sources[0], "my source", "http://nuget.org");
            AssertPackageSource(sources[1], "NuGet official package source", "https://nuget.org/api/v2/");
            Assert.False(sources[1].IsEnabled);
            Assert.True(sources[1].IsOfficial);
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
        public void GetActivePackageSourceWillPreserveWindows8ExpressSourceWhenRunningWindows8Express()
        {
            // Arrange
            var userSettings = new Mock<ISettings>(MockBehavior.Strict);
            userSettings.Setup(_ => _.GetValues("packageSources")).Returns(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("NuGet official package source", "https://nuget.org/api/v2/")
            });
            userSettings.Setup(_ => _.GetValues("activePackageSource")).Returns(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Windows 8 Packages", NuGetConstants.VSExpressForWindows8FeedUrl)
            });

            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            var vsShellInfo = new Mock<IVsShellInfo>();
            vsShellInfo.Setup(_ => _.IsVisualStudioExpressForWindows8).Returns(true);
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider.Object, vsShellInfo.Object);

            // Act
            var source = provider.ActivePackageSource;

            // Assert
            AssertPackageSource(source, "Windows 8 Packages", NuGetConstants.VSExpressForWindows8FeedUrl);
        }

        [Fact]
        public void SetActivePackageSourceAcceptsValueForWindows8FeedWhenRunningWindows8Express()
        {
            // Arrange
            var userSettings = new Mock<ISettings>(MockBehavior.Strict);
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            packageSourceProvider.Setup(_ => _.LoadPackageSources()).Returns(new[]
            {
                new PackageSource("theFirstSource", "theFirstFeed"),
                new PackageSource("https://nuget.org/api/v2/", "NuGet official package source"),
                new PackageSource("theThirdSource", "theThirdFeed"),
            });

            userSettings.Setup(_ => _.GetValues("activePackageSource")).Returns(new[]
            {
                new KeyValuePair<string, string>("theFirstFeed", "theFirstSource")
            });
            userSettings.Setup(_ => _.DeleteSection("activePackageSource")).Returns(true);
            userSettings.Setup(_ => _.SetValue("activePackageSource", "Windows 8 packages", NuGetConstants.VSExpressForWindows8FeedUrl)).Verifiable();

            var vsShellInfo = new Mock<IVsShellInfo>();
            vsShellInfo.Setup(_ => _.IsVisualStudioExpressForWindows8).Returns(true);
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider.Object, vsShellInfo.Object);

            // Act
            provider.ActivePackageSource = new PackageSource(NuGetConstants.VSExpressForWindows8FeedUrl, "Windows 8 packages");

            // Assert
            userSettings.Verify();
        }

        [Fact]
        public void TheDisabledStateOfWindows8FeedIsPersistedWhenRunningOnWindows8Express()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            var packageSourceProvider = new Mock<IPackageSourceProvider>();

            var vsShellInfo = new Mock<IVsShellInfo>();
            vsShellInfo.Setup(_ => _.IsVisualStudioExpressForWindows8).Returns(true);
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider.Object, vsShellInfo.Object);
            packageSourceProvider.Setup(p => p.DisablePackageSource(It.IsAny<PackageSource>())).Callback<PackageSource>(
                ps => AssertPackageSource(ps, "Windows 8 Packages", NuGetConstants.VSExpressForWindows8FeedUrl));

            // Act
            var packageSources = new PackageSource[]
            {
                new PackageSource(NuGetConstants.VSExpressForWindows8FeedUrl, "Windows 8 packages", isEnabled: false, isOfficial: true),
                new PackageSource("theFirstSource", "theFirstFeed", isEnabled: true)
            };
            provider.SavePackageSources(packageSources);

            // Assert
            packageSourceProvider.Verify(p => p.DisablePackageSource(It.IsAny<PackageSource>()), Times.Once());
        }

        [Fact]
        public void TheEnabledStateOfWindows8FeedIsNotPersistedWhenRunningOnWindows8Express()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            var packageSourceProvider = new Mock<IPackageSourceProvider>();

            var vsShellInfo = new Mock<IVsShellInfo>();
            vsShellInfo.Setup(_ => _.IsVisualStudioExpressForWindows8).Returns(true);
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider.Object, vsShellInfo.Object);

            // Act
            var packageSources = new PackageSource[]
            {
                new PackageSource(NuGetConstants.VSExpressForWindows8FeedUrl, "Windows 8 packages", isEnabled: true, isOfficial: true)
            };
            provider.SavePackageSources(packageSources);

            // Assert
            packageSourceProvider.Verify(p => p.DisablePackageSource(It.IsAny<PackageSource>()), Times.Never());
        }

        [Fact]
        public void TheEnabledStateOfWindows8FeedIsRestoredWhenRunningOnWindows8Express()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            packageSourceProvider.Setup(_ => _.LoadPackageSources()).Returns(new PackageSource[]
                {
                    new PackageSource("source", "name"),
                    new PackageSource("theFirstSource", "theFirstFeed", isEnabled: true)
                });

            var vsShellInfo = new Mock<IVsShellInfo>();
            vsShellInfo.Setup(_ => _.IsVisualStudioExpressForWindows8).Returns(true);
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider.Object, vsShellInfo.Object);
            packageSourceProvider.Setup(p => p.IsPackageSourceEnabled(
                                                It.Is<PackageSource>(ps => ps.Name.Equals("Windows 8 packages", StringComparison.OrdinalIgnoreCase))))
                                 .Returns(false);

            // Act
            var packageSources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(4, packageSources.Count);
            AssertPackageSource(packageSources[0], "Windows 8 Packages", NuGetConstants.VSExpressForWindows8FeedUrl);
            Assert.False(packageSources[0].IsEnabled);
        }

        [Fact]
        public void SetActivePackageSourceToWindows8FeedWillThrowWhenNotRunningWindows8Express()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            packageSourceProvider.Setup(_ => _.LoadPackageSources()).Returns(new[]
            {
                new PackageSource("theFirstSource", "theFirstFeed"),
                new PackageSource("theOfficialSource", "NuGet official source"),
                new PackageSource("theThirdSource", "theThirdFeed"),
            });
            var vsShellInfo = new Mock<IVsShellInfo>();
            vsShellInfo.Setup(_ => _.IsVisualStudioExpressForWindows8).Returns(false);
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider.Object, vsShellInfo.Object);

            // Act & Assert
            ExceptionAssert.ThrowsArgumentException(
                () => provider.ActivePackageSource = new PackageSource(NuGetConstants.VSExpressForWindows8FeedUrl, "Windows 8 packages"), 
                "value",
                "The package source does not belong to the collection of available sources.");
        }

        [Fact]
        public void LoadPackageSourcesWillAddTheWindows8SourceAtTheFrontWhenRunningWindows8Express()
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
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(4, sources.Count);
            AssertPackageSource(sources[0], "Windows 8 Packages", NuGetConstants.VSExpressForWindows8FeedUrl);
            AssertPackageSource(sources[1], "theFirstFeed", "theFirstSource");
            AssertPackageSource(sources[2], "NuGet official package source", "https://nuget.org/api/v2/");
            AssertPackageSource(sources[3], "theThirdFeed", "theThirdSource");
        }

        [Fact]
        public void LoadPackageSourcesWillNotAddTheWindows8SourceWhenNotRunningWindows8Express()
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
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(3, sources.Count);
            AssertPackageSource(sources[0], "theFirstFeed", "theFirstSource");
            AssertPackageSource(sources[1], "NuGet official package source", "https://nuget.org/api/v2/");
            AssertPackageSource(sources[2], "theThirdFeed", "theThirdSource");
        }

        [Fact]
        public void SavePackageSourcesWillNotSaveTheWindows8ExpressFeedWhenRunningWindows8Express()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            var vsShellInfo = new Mock<IVsShellInfo>();
            vsShellInfo.Setup(_ => _.IsVisualStudioExpressForWindows8).Returns(true);
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider.Object, vsShellInfo.Object);

            IList<PackageSource> savedSources = null;
            packageSourceProvider.Setup(_ => _.SavePackageSources(It.IsAny<IEnumerable<PackageSource>>()))
                                 .Callback((IEnumerable<PackageSource> _) => savedSources = _.ToList())
                                 .Verifiable();

            // Act
            provider.SavePackageSources(new[]
                {
                    new PackageSource("theFirstSource", "theFirstFeed"),
                    new PackageSource("https://nuget.org/api/v2/curated-feeds/windows8-packages/", "Windows 8 Packages"){ IsOfficial = true },
                    new PackageSource("theThirdSource", "theThirdFeed"),
                });

            // Assert
            Assert.NotNull(savedSources);
            Assert.Equal(2, savedSources.Count);
            AssertPackageSource(savedSources[0], "theFirstFeed", "theFirstSource");
            AssertPackageSource(savedSources[1], "theThirdFeed", "theThirdSource");
        }

        [Fact]
        public void SavePackageSourcesWillSaveTheWindows8ExpressFeedWhenNotRunningWindows8Express()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            var vsShellInfo = new Mock<IVsShellInfo>();
            vsShellInfo.Setup(_ => _.IsVisualStudioExpressForWindows8).Returns(false);
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider.Object, vsShellInfo.Object);

            IList<PackageSource> savedSources = null;
            packageSourceProvider.Setup(_ => _.SavePackageSources(It.IsAny<IEnumerable<PackageSource>>()))
                                 .Callback((IEnumerable<PackageSource> _) => savedSources = _.ToList())
                                 .Verifiable();

            // Act
            provider.SavePackageSources(new[]
                {
                    new PackageSource("theFirstSource", "theFirstFeed"),
                    new PackageSource("https://nuget.org/api/v2/curated-feeds/windows8-packages/", "Windows 8 Packages"){ IsOfficial = true },
                    new PackageSource("theThirdSource", "theThirdFeed"),
                });

            // Assert
            Assert.NotNull(savedSources);
            Assert.Equal(3, savedSources.Count);
            AssertPackageSource(savedSources[0], "theFirstFeed", "theFirstSource");
            AssertPackageSource(savedSources[1], "Windows 8 Packages", "https://nuget.org/api/v2/curated-feeds/windows8-packages/");
            AssertPackageSource(savedSources[2], "theThirdFeed", "theThirdSource");
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