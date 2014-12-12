using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NuGet.Test;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    public class VsPackageSourceProviderTest
    {
        private const string NuGetOfficialFeedUrl = "https://www.nuget.org/api/v2/";
        private const string NuGetOfficialFeedName = "nuget.org";
        private const string NuGetLegacyOfficialFeedName = "NuGet official package source";
        
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
            Assert.Equal(NuGetOfficialFeedUrl, sources[0].Source);
        }

        [Fact]
        public void CtorMigrateV1FeedToV2Feed()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            userSettings.Setup(s => s.GetSettingValues("packageSources", true))
                    .Returns(new[] { new SettingValue(NuGetLegacyOfficialFeedName, "https://go.microsoft.com/fwlink/?LinkID=206669", false) });
            var sourceProvider = CreateDefaultSourceProvider(userSettings.Object);
            var provider = new VsPackageSourceProvider(userSettings.Object, sourceProvider, new Mock<IVsShellInfo>().Object);

            // Act
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(1, sources.Count);
            Assert.Equal(NuGetOfficialFeedUrl, sources[0].Source);
        }

        [Fact]
        public void CtorMigrateV2LegacyFeedToV2Feed()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            userSettings.Setup(s => s.GetSettingValues("packageSources", true))
                        .Returns(new[] { new SettingValue(NuGetLegacyOfficialFeedName, "https://go.microsoft.com/fwlink/?LinkID=230477", false) });
            var sourceProvider = CreateDefaultSourceProvider(userSettings.Object);
            var provider = new VsPackageSourceProvider(userSettings.Object, sourceProvider, new Mock<IVsShellInfo>().Object);

            // Act
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(1, sources.Count);
            Assert.Equal(NuGetOfficialFeedUrl, sources[0].Source);
        }

        [Fact]
        public void CtorMigrateV2LegacyFeedNameToV2Feed()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            userSettings.Setup(s => s.GetSettingValues("packageSources", true))
                        .Returns(new[] { new SettingValue(NuGetLegacyOfficialFeedName, "https://nuget.org/api/v2/", false) });
            var sourceProvider = CreateDefaultSourceProvider(userSettings.Object);
            var provider = new VsPackageSourceProvider(userSettings.Object, sourceProvider, new Mock<IVsShellInfo>().Object);

            // Act
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(1, sources.Count);
            Assert.Equal(NuGetOfficialFeedUrl, sources[0].Source);
            Assert.Equal(NuGetOfficialFeedName, sources[0].Name);
        }

        [Fact]
        public void CtorMigratesEvenCaseDoesNotMatch()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            userSettings.Setup(s => s.GetSettingValues("packageSources", true))
                        .Returns(new[] { new SettingValue("NuGET oFFIcial PACKAGE souRCe", "HTTPS://nUGet.org/ApI/V2/", false) });
            var sourceProvider = CreateDefaultSourceProvider(userSettings.Object);
            var provider = new VsPackageSourceProvider(userSettings.Object, sourceProvider, new Mock<IVsShellInfo>().Object);

            // Act
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(1, sources.Count);
            Assert.Equal(NuGetOfficialFeedUrl, sources[0].Source);
            Assert.Equal(NuGetOfficialFeedName, sources[0].Name);
        }


        // Test that when there are non-machine wide user specified sources, the
        // official source is added but disabled.
        [Fact]
        public void DefaultSourceAddedButDisabled()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            userSettings.Setup(s => s.GetSettingValues("packageSources", true))
                        .Returns(new[] { 
                            new SettingValue("Test1", "https://test1", true),
                            new SettingValue("Test2", "https://test2", false) 
                        });
            var sourceProvider = CreateDefaultSourceProvider(userSettings.Object);
            var provider = new VsPackageSourceProvider(userSettings.Object, sourceProvider, new Mock<IVsShellInfo>().Object);

            // Act
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(3, sources.Count);

            Assert.Equal("https://test2", sources[0].Source);

            Assert.Equal(NuGetOfficialFeedUrl, sources[1].Source);
            Assert.False(sources[1].IsEnabled);

            Assert.Equal("https://test1", sources[2].Source);
        }

        // Test that when there are machine wide user specified sources, but no non-machine
        // wide user specified sources, then the official source is added and ENABLED.
        [Fact]
        public void DefaultSourceAddedAndEnabled()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            userSettings.Setup(s => s.GetSettingValues("packageSources", true))
                        .Returns(new[] { 
                            new SettingValue("Test1", "https://test1", true),
                            new SettingValue("Test2", "https://test2", true) 
                        });
            var sourceProvider = CreateDefaultSourceProvider(userSettings.Object);
            var provider = new VsPackageSourceProvider(userSettings.Object, sourceProvider, new Mock<IVsShellInfo>().Object);

            // Act
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(3, sources.Count);
            Assert.Equal(NuGetOfficialFeedUrl, sources[0].Source);
            Assert.True(sources[0].IsEnabled);
        }

        [Fact]
        public void LoadPackageSourcesAddOfficialSourceIfMissing()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            userSettings.Setup(s => s.GetSettingValues("packageSources", true))
                        .Returns(new[] { new SettingValue("my source", "http://www.nuget.org", false) });
            var sourceProvider = CreateDefaultSourceProvider(userSettings.Object);
            var provider = new VsPackageSourceProvider(userSettings.Object, sourceProvider, new Mock<IVsShellInfo>().Object);

            // Act
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(2, sources.Count);
            AssertPackageSource(sources[0], "my source", "http://www.nuget.org");
            AssertPackageSource(sources[1], NuGetOfficialFeedName, NuGetOfficialFeedUrl);
            Assert.False(sources[1].IsEnabled);
            Assert.True(sources[1].IsOfficial);
        }     

        [Fact]
        public void CtorMigrateV1FeedToV2FeedAndPreserveIsEnabledProperty()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            userSettings.Setup(s => s.GetSettingValues("packageSources", true))
                        .Returns(new[] { new SettingValue(NuGetLegacyOfficialFeedName, "https://go.microsoft.com/fwlink/?LinkID=206669", false) });

            // disable the official source
            userSettings.Setup(s => s.GetSettingValues("disabledPackageSources", false))
                        .Returns(new[] { new  SettingValue(NuGetLegacyOfficialFeedName, "true", false) });

            var provider = new VsPackageSourceProvider(userSettings.Object, CreateDefaultSourceProvider(userSettings.Object), new Mock<IVsShellInfo>().Object);

            // Act
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(1, sources.Count);
            Assert.Equal(NuGetOfficialFeedUrl, sources[0].Source);
            Assert.Equal(NuGetOfficialFeedName, sources[0].Name);
            Assert.False(sources[0].IsEnabled);
        }

        [Fact]
        public void PreserveActiveSourceWhileMigratingNuGetFeed()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            userSettings.Setup(s => s.GetSettingValues("packageSources", true)).Returns(new[]
            {
                new SettingValue(NuGetLegacyOfficialFeedName, "https://go.microsoft.com/fwlink/?LinkID=206669", false),
                new SettingValue("one", "onesource", false),
            });

            userSettings.Setup(s => s.GetValues("activePackageSource"))
                        .Returns(new[] { new KeyValuePair<string, string>("one", "onesource") });

            var provider = new VsPackageSourceProvider(userSettings.Object, CreateDefaultSourceProvider(userSettings.Object), new Mock<IVsShellInfo>().Object);

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
            Assert.Equal(NuGetOfficialFeedName, sources[0].Name);
        }

        [Fact]
        public void MigrateActivePackageSourceToV2()
        {
            // Arrange
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValue("activePackageSource", NuGetLegacyOfficialFeedName))
                    .Returns("https://go.microsoft.com/fwlink/?LinkID=206669");
            var provider = new VsPackageSourceProvider(settings.Object, CreateDefaultSourceProvider(settings.Object), new Mock<IVsShellInfo>().Object);

            // Act
            PackageSource activePackageSource = provider.ActivePackageSource;

            // Assert
            AssertPackageSource(activePackageSource, NuGetOfficialFeedName, NuGetOfficialFeedUrl);
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
        public void ActivePackageSourceShouldBeEnabled()
        {
            // Arrange
            var userSettings = new Mock<ISettings>(MockBehavior.Strict);            
            userSettings.Setup(_ => _.GetValues("activePackageSource")).Returns(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("s1", "http://s1"),
                new KeyValuePair<string, string>("s2", "http://s2")
            });

            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            packageSourceProvider.Setup(p => p.LoadPackageSources()).Returns(
                new[] { 
                    new PackageSource("http://s1", "s1", isEnabled: false),
                    new PackageSource("http://s2", "s2", isEnabled: true)
                });
            var vsShellInfo = new Mock<IVsShellInfo>();
            var provider = new VsPackageSourceProvider(userSettings.Object, packageSourceProvider.Object, vsShellInfo.Object);

            // Act
            var source = provider.ActivePackageSource;

            // Assert
            AssertPackageSource(source, "s2", "http://s2");
        }

        /* !!!
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
        } */

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
            Assert.Equal(NuGetOfficialFeedName, sources[0].Name);
        }

        [Fact]
        public void GetActivePackageSourceWillPreserveWindows8ExpressSourceWhenRunningWindows8Express()
        {
            // Arrange
            var userSettings = new Mock<ISettings>();
            userSettings.Setup(_ => _.GetSettingValues("packageSources", true)).Returns(new[]
            {
                new SettingValue(NuGetOfficialFeedName, NuGetOfficialFeedUrl, false)
            });
            userSettings.Setup(_ => _.GetValues("activePackageSource")).Returns(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Windows 8 Packages", NuGetConstants.VSExpressForWindows8FeedUrl)
            });

            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            packageSourceProvider.Setup(p => p.IsPackageSourceEnabled(
                It.Is<PackageSource>(s => s.Name == "Windows 8 Packages"))).Returns(true);
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
                new PackageSource(NuGetOfficialFeedUrl, NuGetOfficialFeedName),
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
            var mockFileSystem = new MockFileSystem();
            var settings = Settings.LoadDefaultSettings(
                mockFileSystem,
                configFileName: null,
                machineWideSettings: null);
            var packageSourceProvider = new PackageSourceProvider(settings);
            var vsShellInfo = new Mock<IVsShellInfo>();
            vsShellInfo.Setup(_ => _.IsVisualStudioExpressForWindows8).Returns(true);
            var provider = new VsPackageSourceProvider(settings, packageSourceProvider, vsShellInfo.Object);
            var packageSources = provider.LoadPackageSources().ToList();

            Assert.Equal(1, packageSources.Count);
            Assert.Equal(NuGetConstants.VSExpressForWindows8FeedUrl, packageSources[0].Source);
            Assert.True(packageSources[0].IsEnabled);

            // Act
            packageSources[0].IsEnabled = false;
            provider.SavePackageSources(packageSources);

            // Assert
            // the Win8ExpressFeed is not saved in <disabledPackageSources>.
            Assert.Equal(1, mockFileSystem.Paths.Count);
            var configFile = mockFileSystem.Paths.First().Key;
            var configFileContent = mockFileSystem.ReadAllText(configFile);

            Assert.Equal(
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources />
  <disabledPackageSources>
    <add key=""Windows 8 Packages"" value=""true"" />
  </disabledPackageSources>
</configuration>",
                  configFileContent);
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
            Assert.Equal(3, packageSources.Count);
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
        public void PackageSourcesNotChanged()
        {
            bool r = VsPackageSourceProvider.PackageSourcesEqual(
                new List<PackageSource>() {
                    new PackageSource("a", "http://a", isEnabled: true),
                    new PackageSource("b", "http://b", isEnabled: false)
                },
                new List<PackageSource>() {
                    new PackageSource("a", "http://a", isEnabled: true),
                    new PackageSource("b", "http://b", isEnabled: false)
                });
            Assert.True(r);
        }

        [Fact]
        public void PackageSourcesChanged()
        {
            // Assert: names differ
            bool r = VsPackageSourceProvider.PackageSourcesEqual(
                new List<PackageSource>() {
                    new PackageSource("a", "http://a", isEnabled: true),
                    new PackageSource("b", "http://b", isEnabled: false)
                },
                new List<PackageSource>() {
                    new PackageSource("a1", "http://a", isEnabled: true),
                    new PackageSource("b", "http://b", isEnabled: false)
                });
            Assert.False(r);

            // Assert: sources differ
            r = VsPackageSourceProvider.PackageSourcesEqual(
                new List<PackageSource>() {
                    new PackageSource("a", "http://a", isEnabled: true),
                    new PackageSource("b", "http://b", isEnabled: false)
                },
                new List<PackageSource>() {
                    new PackageSource("a", "http://a1", isEnabled: true),
                    new PackageSource("b", "http://b", isEnabled: false)
                });
            Assert.False(r);

            // Assert: isEnabled differ
            r = VsPackageSourceProvider.PackageSourcesEqual(
                new List<PackageSource>() {
                    new PackageSource("a", "http://a", isEnabled: true),
                    new PackageSource("b", "http://b", isEnabled: false)
                },
                new List<PackageSource>() {
                    new PackageSource("a", "http://a", isEnabled: false),
                    new PackageSource("b", "http://b", isEnabled: false)
                });
            Assert.False(r);
        }

        [Fact]
        public void LoadPackageSourcesWillAddTheWindows8SourceAtTheFrontWhenRunningWindows8Express()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile("nuget.config",
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""a"" value=""http://a"" />
  </packageSources>
</configuration>");

            var settings = Settings.LoadDefaultSettings(
                mockFileSystem,
                configFileName: null,
                machineWideSettings: null);
            var packageSourceProvider = new PackageSourceProvider(settings);
            var vsShellInfo = new Mock<IVsShellInfo>();
            vsShellInfo.Setup(_ => _.IsVisualStudioExpressForWindows8).Returns(true);
            var provider = new VsPackageSourceProvider(settings, packageSourceProvider, vsShellInfo.Object);

            // Act
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(2, sources.Count);
            AssertPackageSource(sources[0], "Windows 8 Packages", NuGetConstants.VSExpressForWindows8FeedUrl);
            AssertPackageSource(sources[1], "a", "http://a");
        }

        [Fact]
        public void LoadPackageSourcesWillNotAddTheWindows8SourceWhenNotRunningWindows8Express()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var settings = Settings.LoadDefaultSettings(
                mockFileSystem,
                configFileName: null,
                machineWideSettings: null);
            var packageSourceProvider = new PackageSourceProvider(settings);
            var vsShellInfo = new Mock<IVsShellInfo>();
            vsShellInfo.Setup(_ => _.IsVisualStudioExpressForWindows8).Returns(false);
            var provider = new VsPackageSourceProvider(settings, packageSourceProvider, vsShellInfo.Object);

            // Act
            var sources = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(0, sources.Count);
        }

        [Fact]
        public void SavePackageSourcesWillNotSaveTheWindows8ExpressFeedWhenRunningWindows8Express()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();            
            var settings = Settings.LoadDefaultSettings(
                mockFileSystem,
                configFileName: null,
                machineWideSettings: null);
            var packageSourceProvider = new PackageSourceProvider(settings);
            var vsShellInfo = new Mock<IVsShellInfo>();
            vsShellInfo.Setup(_ => _.IsVisualStudioExpressForWindows8).Returns(true);
            var provider = new VsPackageSourceProvider(settings, packageSourceProvider, vsShellInfo.Object);
            var packageSources = provider.LoadPackageSources().ToList();

            Assert.Equal(1, packageSources.Count);
            Assert.Equal(NuGetConstants.VSExpressForWindows8FeedUrl, packageSources[0].Source);
            Assert.True(packageSources[0].IsEnabled);

            // Act
            provider.SavePackageSources(packageSources);

            // Assert
            // the Win8ExpressFeed is saved as active source, but not saved
            // in <packageSources>.
            Assert.Equal(1, mockFileSystem.Paths.Count);
            var configFile = mockFileSystem.Paths.First().Key;
            var configFileContent = mockFileSystem.ReadAllText(configFile);

            Assert.Equal(
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <activePackageSource>
    <add key=""Windows 8 Packages"" value=""https://www.nuget.org/api/v2/curated-feeds/windows8-packages/"" />
  </activePackageSource>
</configuration>",
                  configFileContent);
        }

        private static void AssertPackageSource(PackageSource ps, string name, string source)
        {
            Assert.Equal(name, ps.Name);
            Assert.Equal(source, ps.Source);
        }

        private static PackageSourceProvider CreateDefaultSourceProvider(ISettings settings)
        {
            return new PackageSourceProvider(settings, VsPackageSourceProvider.DefaultSources, VsPackageSourceProvider.FeedsToMigrate, configurationDefaultSources: null, environment: new Mock<IEnvironmentVariableReader>().Object);
        }              
    }
}