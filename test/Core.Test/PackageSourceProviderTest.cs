using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Moq;
using NuGet.Test.Mocks;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test
{
    public class PackageSourceProviderTest
    {
        [Fact]
        public void TestNoPackageSourcesAreReturnedIfUserSettingsIsEmpty()
        {
            // Arrange
            var provider = CreatePackageSourceProvider();

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(0, values.Count);
        }

        [Fact]
        public void LoadPackageSourcesReturnsEmptySequenceIfDefaultPackageSourceIsNull()
        {
            // Arrange
            var settings = new Mock<ISettings>();
            var provider = CreatePackageSourceProvider(settings.Object, providerDefaultSources: null);

            // Act
            var values = provider.LoadPackageSources();

            // Assert
            Assert.False(values.Any());
        }

        [Fact]
        public void LoadPackageSourcesReturnsEmptySequenceIfDefaultPackageSourceIsEmpty()
        {
            // Arrange
            var settings = new Mock<ISettings>();
            var provider = CreatePackageSourceProvider(settings.Object, providerDefaultSources: new PackageSource[] { });

            // Act
            var values = provider.LoadPackageSources();

            // Assert
            Assert.False(values.Any());
        }

        [Fact]
        public void LoadPackageSourcesReturnsDefaultSourcesIfSpecified()
        {
            // Arrange
            var settings = new Mock<ISettings>().Object;
            var provider = CreatePackageSourceProvider(settings, providerDefaultSources: new[] { new PackageSource("A"), new PackageSource("B") });

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(2, values.Count);
            Assert.Equal("A", values.First().Source);
            Assert.Equal("B", values.Last().Source);
        }

        [Fact]
        public void LoadPackageSourcesWhereAMigratedSourceIsAlsoADefaultSource()
        {
            // Arrange
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValues("packageSources", true))
                    .Returns(new[] { new SettingValue("AOld", "urlA", false), new SettingValue("userDefinedSource", "userDefinedSourceUrl", false) });
            settings.Setup(s => s.GetValues("disabledPackageSources", false)).Returns(new SettingValue[0]);
            settings.Setup(s => s.GetNestedValues("packageSourceCredentials", It.IsAny<string>())).Returns(new SettingValue[0]);

            var defaultPackageSourceA = new PackageSource("urlA", "ANew");
            var defaultPackageSourceB = new PackageSource("urlB", "B");

            var provider = CreatePackageSourceProvider(settings.Object, providerDefaultSources: new[] { defaultPackageSourceA, defaultPackageSourceB },
                migratePackageSources: new Dictionary<PackageSource, PackageSource>
                                        {
                                            { new PackageSource("urlA", "AOld"), defaultPackageSourceA },
                                        });

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            // Package Source AOld will be migrated to ANew. B will simply get added
            // Since default source B got added when there are other package sources it will be disabled
            // However, package source ANew must stay enabled
            // PackageSource userDefinedSource is a user package source and is untouched
            Assert.Equal(3, values.Count);
            Assert.Equal("urlA", values[0].Source);
            Assert.Equal("ANew", values[0].Name);
            Assert.True(values[0].IsEnabled);
            Assert.Equal("userDefinedSourceUrl", values[1].Source);
            Assert.Equal("userDefinedSource", values[1].Name);
            Assert.True(values[1].IsEnabled);
            Assert.Equal("urlB", values[2].Source);
            Assert.Equal("B", values[2].Name);
            Assert.False(values[2].IsEnabled);
        }

        [Fact]
        public void LoadPackageSourcesPerformMigrationIfSpecified()
        {
            // Arrange
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValues("packageSources", true)).Returns(
                new[] { 
                    new SettingValue("one", "onesource", false),
                    new SettingValue("two", "twosource", false),
                    new SettingValue("three", "threesource", false),
                }
            );

            // disable package "three"
            settings.Setup(s => s.GetValues("disabledPackageSources", false)).Returns(new[] { new SettingValue("three", "true", false) });

            IReadOnlyList<SettingValue> savedSettingValues = null;
            settings.Setup(s => s.UpdateSections("packageSources", It.IsAny<IReadOnlyList<SettingValue>>()))
                    .Callback<string, IReadOnlyList<SettingValue>>((_, savedVals) => { savedSettingValues = savedVals; })
                    .Verifiable();

            var provider = CreatePackageSourceProvider(settings.Object,
                null,
                new Dictionary<PackageSource, PackageSource> {
                    { new PackageSource("onesource", "one"), new PackageSource("goodsource", "good") },
                    { new PackageSource("foo", "bar"), new PackageSource("foo", "bar") },
                    { new PackageSource("threesource", "three"), new PackageSource("awesomesource", "awesome") }
                }
            );

            // Act
            var values = provider.LoadPackageSources().ToList();
            savedSettingValues = savedSettingValues.ToList();

            // Assert
            Assert.Equal(3, values.Count);
            AssertPackageSource(values[0], "good", "goodsource", true);
            AssertPackageSource(values[1], "two", "twosource", true);
            AssertPackageSource(values[2], "awesome", "awesomesource", false);

            Assert.Equal(3, savedSettingValues.Count);
            Assert.Equal("good", savedSettingValues[0].Key);
            Assert.Equal("goodsource", savedSettingValues[0].Value);
            Assert.Equal("two", savedSettingValues[1].Key);
            Assert.Equal("twosource", savedSettingValues[1].Value);
            Assert.Equal("awesome", savedSettingValues[2].Key);
            Assert.Equal("awesomesource", savedSettingValues[2].Value);
        }

        [Fact]
        public void CallSaveMethodAndLoadMethodShouldReturnTheSamePackageSet()
        {
            // Arrange
            var expectedSources = new[] { new PackageSource("one", "one"), new PackageSource("two", "two"), new PackageSource("three", "three") };
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetValues("packageSources", true))
                    .Returns(new[] { new SettingValue("one", "one", false),
                                     new SettingValue("two", "two", false),
                                     new SettingValue("three", "three", false)
                                })
                    .Verifiable();
            settings.Setup(s => s.GetValues("disabledPackageSources", false)).Returns(new SettingValue[0]);
            settings.Setup(s => s.GetNestedValues("packageSourceCredentials", It.IsAny<string>())).Returns(new SettingValue[0]);
            settings.Setup(s => s.DeleteSection("packageSourceCredentials")).Returns(true).Verifiable();
            settings.Setup(s => s.UpdateSections("packageSources", It.IsAny<IReadOnlyList<SettingValue>>()))
                    .Callback((string section, IReadOnlyList<SettingValue> values) =>
                    {
                        Assert.Equal(3, values.Count);
                        Assert.Equal("one", values[0].Key);
                        Assert.Equal("one", values[0].Value);
                        Assert.Equal("two", values[1].Key);
                        Assert.Equal("two", values[1].Value);
                        Assert.Equal("three", values[2].Key);
                        Assert.Equal("three", values[2].Value);
                    })
                    .Verifiable();

            settings.Setup(s => s.UpdateSections("disabledPackageSources", It.IsAny<IReadOnlyList<SettingValue>>()))
                .Callback((string section, IReadOnlyList<SettingValue> values) =>
                {
                    Assert.Empty(values);
                })
                .Verifiable();

            var provider = CreatePackageSourceProvider(settings.Object);

            // Act
            var sources = provider.LoadPackageSources().ToList();
            provider.SavePackageSources(sources);

            // Assert
            settings.Verify();
            Assert.Equal(3, sources.Count);
            for (int i = 0; i < sources.Count; i++)
            {
                AssertPackageSource(expectedSources[i], sources[i].Name, sources[i].Source, true);
            }
        }

        [Fact]
        public void WithMachineWideSources()
        {
            // Arrange           
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValues("packageSources", true))
                    .Returns(new[] { new SettingValue("one", "one", true) { Priority = 1 },
                                     new SettingValue("two", "two", false) { Priority = 2 },
                                     new SettingValue("three", "three", false) { Priority = 2 }
                                });

            settings.Setup(s => s.UpdateSections("packageSources", It.IsAny<IReadOnlyList<SettingValue>>()))
                    .Callback((string section, IReadOnlyList<SettingValue> values) =>
                    {
                        // verifies that only sources "two" and "three" are passed.
                        // the machine wide source "one" is not.
                        Assert.Equal(2, values.Count);
                        Assert.Equal("two", values[0].Key);
                        Assert.Equal("two", values[0].Value);
                        Assert.Equal("three", values[1].Key);
                        Assert.Equal("three", values[1].Value);
                    })
                    .Verifiable();

            settings.Setup(s => s.UpdateSections("disabledPackageSources", It.IsAny<IReadOnlyList<SettingValue>>()))
                .Callback((string section, IReadOnlyList<SettingValue> values) =>
                {
                    // verifies that the machine wide source "one" is passed here
                    // since it is disabled.                    
                    Assert.Equal(1, values.Count);
                    Assert.Equal("one", values[0].Key);
                    Assert.Equal("true", values[0].Value);
                })
                .Verifiable();

            var provider = CreatePackageSourceProvider(settings.Object);

            // Act
            var sources = provider.LoadPackageSources().ToList();

            // disable the machine wide source "one", and save the result in provider.
            Assert.Equal("one", sources[2].Name);
            sources[2].IsEnabled = false;
            provider.SavePackageSources(sources);

            // Assert
            // all assertions are done inside Callback()'s
        }

        [Fact]
        public void LoadPackageSourcesReturnCorrectDataFromSettings()
        {
            // Arrange
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetValues("packageSources", true))
                    .Returns(new[] { new SettingValue("one", "onesource", true) { Priority = 1 }, 
                                     new SettingValue("two", "twosource", false) { Priority = 2 }, 
                                     new SettingValue("three", "threesource", false) { Priority = 2 }
                                })
                    .Verifiable();
            settings.Setup(s => s.GetValues("disabledPackageSources", false)).Returns(new SettingValue[0]);
            settings.Setup(s => s.GetNestedValues("packageSourceCredentials", It.IsAny<string>())).Returns(new SettingValue[0]);

            var provider = CreatePackageSourceProvider(settings.Object);

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(3, values.Count);
            AssertPackageSource(values[0], "two", "twosource", true);
            AssertPackageSource(values[1], "three", "threesource", true);
            AssertPackageSource(values[2], "one", "onesource", true, true);
        }

        [Fact]
        public void LoadPackageSourcesReturnCorrectDataFromSettingsWhenSomePackageSourceIsDisabled()
        {
            // Arrange
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetValues("packageSources", true))
                    .Returns(new[] { new SettingValue("one", "onesource", false), 
                                     new SettingValue("two", "twosource", false), 
                                     new SettingValue("three", "threesource", false)
                                });

            settings.Setup(s => s.GetValues("disabledPackageSources", false)).Returns(new[] { new SettingValue("two", "true", false) });
            settings.Setup(s => s.GetNestedValues("packageSourceCredentials", It.IsAny<string>())).Returns(new SettingValue[0]);

            var provider = CreatePackageSourceProvider(settings.Object);

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(3, values.Count);
            AssertPackageSource(values[0], "one", "onesource", true);
            AssertPackageSource(values[1], "two", "twosource", false);
            AssertPackageSource(values[2], "three", "threesource", true);
        }

        /// <summary>
        /// The following test tests case 1 listed in PackageSourceProvider.SetDefaultPackageSources(...)
        /// Case 1. Default Package Source is already present matching both feed source and the feed name
        /// </summary>
        [Fact]
        public void LoadPackageSourcesWhereALoadedSourceMatchesDefaultSourceInNameAndSource()
        {
            // Arrange
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetValues("packageSources", true))
                    .Returns(new[] { new SettingValue("one", "onesource", false) });

            // Disable package source one
            settings.Setup(s => s.GetValues("disabledPackageSources", false)).Returns(new[] { new SettingValue("one", "true", false) });
            settings.Setup(s => s.GetNestedValues("packageSourceCredentials", It.IsAny<string>())).Returns(new SettingValue[0]);

            string configurationDefaultsFileContent = @"
<configuration>
    <packageSources>
        <add key='one' value='onesource' />
    </packageSources>
</configuration>";

            var mockFileSystem = new MockFileSystem();
            var configurationDefaultsPath = "NuGetDefaults.config";
            mockFileSystem.AddFile(configurationDefaultsPath, configurationDefaultsFileContent);
            ConfigurationDefaults configurationDefaults = new ConfigurationDefaults(mockFileSystem, configurationDefaultsPath);

            var provider = CreatePackageSourceProvider(settings.Object, providerDefaultSources: null, migratePackageSources: null, configurationDefaultSources: configurationDefaults.DefaultPackageSources);

            // Act
            var values = provider.LoadPackageSources();

            // Assert
            Assert.Equal(1, values.Count());
            // Package source 'one' represents case 1. No real change takes place. IsOfficial will become true though. IsEnabled remains false as it is ISettings
            AssertPackageSource(values.First(), "one", "onesource", false, false, true);
        }

        /// <summary>
        /// The following test tests case 2 listed in PackageSourceProvider.SetDefaultPackageSources(...)
        /// Case 2. Default Package Source is already present matching feed source but with a different feed name. DO NOTHING
        /// </summary>
        [Fact]
        public void LoadPackageSourcesWhereALoadedSourceMatchesDefaultSourceInSourceButNotInName()
        {
            // Arrange
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetValues("packageSources", true))
                    .Returns(new[] { new SettingValue("two", "twosource", false) });
            settings.Setup(s => s.GetNestedValues("packageSourceCredentials", It.IsAny<string>())).Returns(new SettingValue[0]);
            settings.Setup(s => s.GetValues("disabledPackageSources", false)).Returns(new SettingValue[0]);

            string configurationDefaultsFileContent = @"
<configuration>
    <packageSources>
        <add key='twodefault' value='twosource' />
    </packageSources>
    <disabledPackageSources>
        <add key='twodefault' value='true' />
    </disabledPackageSources>
</configuration>";

            var mockFileSystem = new MockFileSystem();
            var configurationDefaultsPath = "NuGetDefaults.config";
            mockFileSystem.AddFile(configurationDefaultsPath, configurationDefaultsFileContent);
            ConfigurationDefaults configurationDefaults = new ConfigurationDefaults(mockFileSystem, configurationDefaultsPath);

            var provider = CreatePackageSourceProvider(settings.Object, providerDefaultSources: null, migratePackageSources: null, configurationDefaultSources: configurationDefaults.DefaultPackageSources);

            // Act
            var values = provider.LoadPackageSources();

            // Assert
            Assert.Equal(1, values.Count());
            // Package source 'two' represents case 2. No Change effected. The existing feed will not be official
            AssertPackageSource(values.First(), "two", "twosource", true, false, false);
        }

        /// <summary>
        /// The following test tests case 3 listed in PackageSourceProvider.SetDefaultPackageSources(...)
        /// Case 3. Default Package Source is not present, but there is another feed source with the same feed name. Override that feed entirely
        /// </summary>
        [Fact]
        public void LoadPackageSourcesWhereALoadedSourceMatchesDefaultSourceInNameButNotInSource()
        {
            // Arrange
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetValues("packageSources", true))
                    .Returns(new[] { new SettingValue("three", "threesource", false) });
            settings.Setup(s => s.GetNestedValues("packageSourceCredentials", It.IsAny<string>())).Returns(new SettingValue[0]);
            settings.Setup(s => s.GetValues("disabledPackageSources", false)).Returns(new SettingValue[0]);

            string configurationDefaultsFileContent = @"
<configuration>
    <packageSources>
        <add key='three' value='threedefaultsource' />
    </packageSources>
</configuration>";

            var mockFileSystem = new MockFileSystem();
            var configurationDefaultsPath = "NuGetDefaults.config";
            mockFileSystem.AddFile(configurationDefaultsPath, configurationDefaultsFileContent);
            ConfigurationDefaults configurationDefaults = new ConfigurationDefaults(mockFileSystem, configurationDefaultsPath);

            var provider = CreatePackageSourceProvider(settings.Object, providerDefaultSources: null, migratePackageSources: null, configurationDefaultSources: configurationDefaults.DefaultPackageSources);

            // Act
            var values = provider.LoadPackageSources();

            // Assert
            Assert.Equal(1, values.Count());
            // Package source 'three' represents case 3. Completely overwritten. Noticeably, Feed Source will match Configuration Default settings
            AssertPackageSource(values.First(), "three", "threedefaultsource", true, false, true);
        }

        /// <summary>
        /// The following test tests case 3 listed in PackageSourceProvider.SetDefaultPackageSources(...)
        /// Case 4. Default Package Source is not present, simply, add it
        /// </summary>
        [Fact]
        public void LoadPackageSourcesWhereNoLoadedSourceMatchesADefaultSource()
        {
            // Arrange
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetValues("packageSources", true))
                    .Returns(new List<SettingValue>());
            settings.Setup(s => s.GetNestedValues("packageSourceCredentials", It.IsAny<string>())).Returns(new SettingValue[0]);
            settings.Setup(s => s.GetValues("disabledPackageSources", false)).Returns(new SettingValue[0]);

            string configurationDefaultsFileContent = @"
<configuration>
    <packageSources>
        <add key='four' value='foursource' />
    </packageSources>
</configuration>";

            var mockFileSystem = new MockFileSystem();
            var configurationDefaultsPath = "NuGetDefaults.config";
            mockFileSystem.AddFile(configurationDefaultsPath, configurationDefaultsFileContent);
            ConfigurationDefaults configurationDefaults = new ConfigurationDefaults(mockFileSystem, configurationDefaultsPath);

            var provider = CreatePackageSourceProvider(settings.Object, providerDefaultSources: null, migratePackageSources: null, configurationDefaultSources: configurationDefaults.DefaultPackageSources);

            // Act
            var values = provider.LoadPackageSources();


            // Assert
            Assert.Equal(1, values.Count());
            // Package source 'four' represents case 4. Simply Added to the list increasing the count by 1. ISettings only has 3 package sources. But, LoadPackageSources returns 4
            AssertPackageSource(values.First(), "four", "foursource", true, false, true);
        }

        [Fact]
        public void LoadPackageSourcesDoesNotReturnProviderDefaultsWhenConfigurationDefaultPackageSourcesIsNotEmpty()
        {
            // Arrange
            var settings = new Mock<ISettings>().Object;

            string configurationDefaultsFileContent = @"
<configuration>
    <packageSources>
        <add key='configurationDefaultOne' value='configurationDefaultOneSource' />
        <add key='configurationDefaultTwo' value='configurationDefaultTwoSource' />
    </packageSources>
</configuration>";

            var mockFileSystem = new MockFileSystem();
            var configurationDefaultsPath = "NuGetDefaults.config";
            mockFileSystem.AddFile(configurationDefaultsPath, configurationDefaultsFileContent);
            ConfigurationDefaults configurationDefaults = new ConfigurationDefaults(mockFileSystem, configurationDefaultsPath);

            var provider = CreatePackageSourceProvider(settings,
                providerDefaultSources: new[] { new PackageSource("providerDefaultA"), new PackageSource("providerDefaultB") },
                migratePackageSources: null,
                configurationDefaultSources: configurationDefaults.DefaultPackageSources);

            // Act
            var values = provider.LoadPackageSources();

            // Assert
            Assert.Equal(2, values.Count());
            Assert.Equal("configurationDefaultOneSource", values.First().Source);
            Assert.Equal("configurationDefaultTwoSource", values.Last().Source);
        }

        [Fact]
        public void LoadPackageSourcesAddsAConfigurationDefaultBackEvenAfterMigration()
        {
            // Arrange
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValues("packageSources", true))
                    .Returns(new List<SettingValue>() { new SettingValue("NuGet official package source", "https://nuget.org/api/v2", false) });
            settings.Setup(s => s.GetNestedValues("packageSourceCredentials", It.IsAny<string>())).Returns(new SettingValue[0]);
            settings.Setup(s => s.GetValues("disabledPackageSources", false)).Returns(new SettingValue[0]);

            string configurationDefaultsFileContent = @"
<configuration>
    <packageSources>
        <add key='NuGet official package source' value='https://nuget.org/api/v2' />
    </packageSources>
</configuration>";

            var mockFileSystem = new MockFileSystem();
            var configurationDefaultsPath = "NuGetDefaults.config";
            mockFileSystem.AddFile(configurationDefaultsPath, configurationDefaultsFileContent);
            ConfigurationDefaults configurationDefaults = new ConfigurationDefaults(mockFileSystem, configurationDefaultsPath);

            var provider = CreatePackageSourceProvider(settings.Object, providerDefaultSources: null,
                migratePackageSources: new Dictionary<PackageSource, PackageSource>
                                       {
                                           { new PackageSource("https://nuget.org/api/v2", "NuGet official package source"), new PackageSource("https://www.nuget.org/api/v2", "nuget.org")  }
                                       },
                configurationDefaultSources: configurationDefaults.DefaultPackageSources);

            // Act
            var values = provider.LoadPackageSources().ToList();


            // Assert
            Assert.Equal(2, values.Count);
            Assert.Equal("nuget.org", values[0].Name);
            Assert.Equal("https://www.nuget.org/api/v2", values[0].Source);
            Assert.Equal("NuGet official package source", values[1].Name);
            Assert.Equal("https://nuget.org/api/v2", values[1].Source);
        }

        [Fact]
        public void LoadPackageSourcesDoesNotDuplicateFeedsOnMigration()
        {
            // Arrange
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValues("packageSources", true))
                    .Returns(new List<SettingValue>() { new SettingValue("NuGet official package source", "https://nuget.org/api/v2", false),
                    new SettingValue("nuget.org", "https://www.nuget.org/api/v2", false) });
            settings.Setup(s => s.GetNestedValues("packageSourceCredentials", It.IsAny<string>())).Returns(new SettingValue[0]);
            settings.Setup(s => s.GetValues("disabledPackageSources", false)).Returns(new SettingValue[0]);

            var provider = CreatePackageSourceProvider(settings.Object, providerDefaultSources: null,
                migratePackageSources: new Dictionary<PackageSource, PackageSource>
                                       {
                                           { new PackageSource("https://nuget.org/api/v2", "NuGet official package source"), new PackageSource("https://www.nuget.org/api/v2", "nuget.org")  }
                                       });

            // Act
            var values = provider.LoadPackageSources().ToList();


            // Assert
            Assert.Equal(1, values.Count);
            Assert.Equal("nuget.org", values[0].Name);
            Assert.Equal("https://www.nuget.org/api/v2", values[0].Source);
        }

        [Fact]
        public void LoadPackageSourcesDoesNotDuplicateFeedsOnMigrationAndSavesIt()
        {
            // Arrange
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValues("packageSources", true))
                    .Returns(new List<SettingValue>() { new SettingValue("NuGet official package source", "https://nuget.org/api/v2", false),
                    new SettingValue("nuget.org", "https://www.nuget.org/api/v2", false) });
            settings.Setup(s => s.GetNestedValues("packageSourceCredentials", It.IsAny<string>())).Returns(new SettingValue[0]);
            settings.Setup(s => s.GetValues("disabledPackageSources", false)).Returns(new SettingValue[0]);
            settings.Setup(s => s.DeleteSection("packageSourceCredentials")).Returns(true).Verifiable();

            settings.Setup(s => s.UpdateSections("packageSources", It.IsAny<IReadOnlyList<SettingValue>>()))
                    .Callback((string section, IReadOnlyList<SettingValue> valuePairs) =>
                    {
                        Assert.Equal(1, valuePairs.Count);
                        Assert.Equal("nuget.org", valuePairs[0].Key);
                        Assert.Equal("https://www.nuget.org/api/v2", valuePairs[0].Value);
                    })
                    .Verifiable();

            settings.Setup(s => s.UpdateSections("disabledPackageSources", It.IsAny<IReadOnlyList<SettingValue>>()))
                .Callback((string section, IReadOnlyList<SettingValue> valuePairs) =>
                {
                    Assert.Empty(valuePairs);
                })
                .Verifiable();

            var provider = CreatePackageSourceProvider(settings.Object, providerDefaultSources: null,
                migratePackageSources: new Dictionary<PackageSource, PackageSource>
                                       {
                                           { new PackageSource("https://nuget.org/api/v2", "NuGet official package source"), new PackageSource("https://www.nuget.org/api/v2", "nuget.org")  }
                                       });

            // Act
            var values = provider.LoadPackageSources().ToList();


            // Assert
            Assert.Equal(1, values.Count);
            Assert.Equal("nuget.org", values[0].Name);
            Assert.Equal("https://www.nuget.org/api/v2", values[0].Source);
            settings.Verify();
        }

        [Fact]
        public void DisablePackageSourceAddEntryToSettings()
        {
            // Arrange
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.SetValue("disabledPackageSources", "A", "true")).Verifiable();
            var provider = CreatePackageSourceProvider(settings.Object);

            // Act
            provider.DisablePackageSource(new PackageSource("source", "A"));

            // Assert
            settings.Verify();
        }

        [Fact]
        public void IsPackageSourceEnabledReturnsFalseIfTheSourceIsDisabled()
        {
            // Arrange
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetValue("disabledPackageSources", "A", false)).Returns("sdfds");
            var provider = CreatePackageSourceProvider(settings.Object);

            // Act
            bool isEnabled = provider.IsPackageSourceEnabled(new PackageSource("source", "A"));

            // Assert
            Assert.False(isEnabled);
        }

        [Theory]
        [InlineData((string)null)]
        [InlineData("")]
        public void IsPackageSourceEnabledReturnsTrueIfTheSourceIsNotDisabled(string returnValue)
        {
            // Arrange
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetValue("disabledPackageSources", "A", false)).Returns(returnValue);
            var provider = CreatePackageSourceProvider(settings.Object);

            // Act
            bool isEnabled = provider.IsPackageSourceEnabled(new PackageSource("source", "A"));

            // Assert
            Assert.True(isEnabled);
        }

        [Theory]
        [InlineData(new object[] { null, "abcd" })]
        [InlineData(new object[] { "", "abcd" })]
        [InlineData(new object[] { "abcd", null })]
        [InlineData(new object[] { "abcd", "" })]
        public void LoadPackageSourcesIgnoresInvalidCredentialPairs(string userName, string password)
        {
            // Arrange
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValues("packageSources", true))
                    .Returns(new[] { new SettingValue("one", "onesource", false), 
                                     new SettingValue("two", "twosource", false), 
                                     new SettingValue("three", "threesource", false)
                                });

            settings.Setup(s => s.GetNestedValues("packageSourceCredentials", "two"))
                    .Returns(new[] { 
                        new SettingValue("Username", userName, false), 
                        new SettingValue("Password", password, false) });

            var provider = CreatePackageSourceProvider(settings.Object);

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(3, values.Count);
            AssertPackageSource(values[1], "two", "twosource", true);
            Assert.Null(values[1].UserName);
            Assert.Null(values[1].Password);
        }

        [Fact]
        public void LoadPackageSourcesReadsCredentialPairs()
        {
            // Arrange
            string encryptedPassword = EncryptionUtility.EncryptString("topsecret");

            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValues("packageSources", true))
                    .Returns(new[] { new SettingValue("one", "onesource", false), 
                                     new SettingValue("two", "twosource", false), 
                                     new SettingValue("three", "threesource", false)
                                });

            settings.Setup(s => s.GetNestedValues("packageSourceCredentials", "two"))
                    .Returns(new[] { new SettingValue("Username", "user1", false), new SettingValue("Password", encryptedPassword, false) });

            var provider = CreatePackageSourceProvider(settings.Object);

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(3, values.Count);
            AssertPackageSource(values[1], "two", "twosource", true);
            Assert.Equal("user1", values[1].UserName);
            Assert.Equal("topsecret", values[1].Password);
            Assert.False(values[1].IsPasswordClearText);
        }

        [Fact]
        public void LoadPackageSourcesReadsClearTextCredentialPairs()
        {
            // Arrange
            const string clearTextPassword = "topsecret";

            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValues("packageSources", true))
                    .Returns(new[] { new SettingValue("one", "onesource", false), 
                                     new SettingValue("two", "twosource", false), 
                                     new SettingValue("three", "threesource", false)
                                });

            settings.Setup(s => s.GetNestedValues("packageSourceCredentials", "two"))
                    .Returns(new[] { new SettingValue("Username", "user1", false), new SettingValue("ClearTextPassword", clearTextPassword, false) });

            var provider = CreatePackageSourceProvider(settings.Object);

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(3, values.Count);
            AssertPackageSource(values[1], "two", "twosource", true);
            Assert.Equal("user1", values[1].UserName);
            Assert.True(values[1].IsPasswordClearText);
            Assert.Equal("topsecret", values[1].Password);
        }

        // Test that when there are duplicate sources, i.e. sources with the same name,
        // then the source specified in one Settings with the highest priority is used.
        [Fact]
        public void DuplicatePackageSources()
        {
            // Arrange
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValues("packageSources", true))
                    .Returns(new[] { new SettingValue("one", "onesource", false), 
                                     new SettingValue("two", "twosource", false) { Priority = 2 }, 
                                     new SettingValue("one", "threesource", false) { Priority = 2}
                                });

            var provider = CreatePackageSourceProvider(settings.Object);

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(2, values.Count);
            AssertPackageSource(values[0], "two", "twosource", true);
            AssertPackageSource(values[1], "one", "threesource", true);
        }

        [Fact]
        public void SavePackageSourcesSaveCorrectDataToSettings()
        {
            // Arrange
            var sources = new[] { new PackageSource("one"), new PackageSource("two"), new PackageSource("three") };
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetValues("packageSources", true))
                .Returns(new SettingValue[0])
                .Verifiable();
            settings.Setup(s => s.GetValues("disabledPackageSources", false))
                .Returns(new SettingValue[0])
                .Verifiable();
            settings.Setup(s => s.DeleteSection("packageSourceCredentials")).Returns(true).Verifiable();

            settings.Setup(s => s.UpdateSections("packageSources", It.IsAny<IReadOnlyList<SettingValue>>()))
                    .Callback((string section, IReadOnlyList<SettingValue> values) =>
                    {
                        Assert.Equal(3, values.Count);
                        Assert.Equal("one", values[0].Key);
                        Assert.Equal("one", values[0].Value);
                        Assert.Equal("two", values[1].Key);
                        Assert.Equal("two", values[1].Value);
                        Assert.Equal("three", values[2].Key);
                        Assert.Equal("three", values[2].Value);
                    })
                    .Verifiable();

            settings.Setup(s => s.UpdateSections("disabledPackageSources", It.IsAny<IReadOnlyList<SettingValue>>()))
                    .Callback((string section, IReadOnlyList<SettingValue> values) =>
                    {
                        Assert.Empty(values);
                    })
                    .Verifiable();

            var provider = CreatePackageSourceProvider(settings.Object);


            // Act
            provider.SavePackageSources(sources);

            // Assert
            settings.Verify();
        }

        [Fact]
        public void SavePackageSourcesSaveCorrectDataToSettingsWhenSomePackageSourceIsDisabled()
        {
            // Arrange
            var sources = new[] { new PackageSource("one"), new PackageSource("two", "two", isEnabled: false), new PackageSource("three") };
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.UpdateSections("disabledPackageSources", It.IsAny<IReadOnlyList<SettingValue>>()))
                    .Callback((string section, IReadOnlyList<SettingValue> values) =>
                    {
                        Assert.Equal(1, values.Count);
                        Assert.Equal("two", values[0].Key);
                        Assert.Equal("true", values[0].Value, StringComparer.OrdinalIgnoreCase);
                    })
                    .Verifiable();

            var provider = CreatePackageSourceProvider(settings.Object);

            // Act
            provider.SavePackageSources(sources);

            // Assert
            settings.Verify();
        }

        [Fact]
        public void SavePackageSourcesSavesCredentials()
        {
            // Arrange
            var entropyBytes = Encoding.UTF8.GetBytes("NuGet");
            var sources = new[] { new PackageSource("one"), 
                                  new PackageSource("twosource", "twoname") { UserName = "User", Password = "password" }, 
                                  new PackageSource("three") 
            };
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.DeleteSection("packageSourceCredentials")).Returns(true).Verifiable();

            settings.Setup(s => s.SetNestedValues("packageSourceCredentials", It.IsAny<string>(), It.IsAny<IList<KeyValuePair<string, string>>>()))
                    .Callback((string section, string key, IList<KeyValuePair<string, string>> values) =>
                    {
                        Assert.Equal("twoname", key);
                        Assert.Equal(2, values.Count);
                        AssertKVP(new KeyValuePair<string, string>("Username", "User"), values[0]);
                        Assert.Equal("Password", values[1].Key);
                        string decryptedPassword = Encoding.UTF8.GetString(
                            ProtectedData.Unprotect(Convert.FromBase64String(values[1].Value), entropyBytes, DataProtectionScope.CurrentUser));
                        Assert.Equal("Password", values[1].Key);
                        Assert.Equal("password", decryptedPassword);
                    })
                    .Verifiable();

            var provider = CreatePackageSourceProvider(settings.Object);

            // Act
            provider.SavePackageSources(sources);

            // Assert
            settings.Verify();
        }

        [Fact]
        public void SavePackageSourcesSavesClearTextCredentials()
        {
            // Arrange
            var sources = new[] { new PackageSource("one"), 
                                  new PackageSource("twosource", "twoname") { UserName = "User", Password = "password", IsPasswordClearText = true}, 
                                  new PackageSource("three") 
            };
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.DeleteSection("packageSourceCredentials")).Returns(true).Verifiable();

            settings.Setup(s => s.SetNestedValues("packageSourceCredentials", It.IsAny<string>(), It.IsAny<IList<KeyValuePair<string, string>>>()))
                    .Callback((string section, string key, IList<KeyValuePair<string, string>> values) =>
                    {
                        Assert.Equal("twoname", key);
                        Assert.Equal(2, values.Count);
                        AssertKVP(new KeyValuePair<string, string>("Username", "User"), values[0]);
                        AssertKVP(new KeyValuePair<string, string>("ClearTextPassword", "password"), values[1]);
                    })
                    .Verifiable();

            var provider = CreatePackageSourceProvider(settings.Object);

            // Act
            provider.SavePackageSources(sources);

            // Assert
            settings.Verify();
        }

        [Fact]
        public void GetAggregateReturnsAggregateRepositoryForAllSources()
        {
            // Arrange
            var repositoryA = new Mock<IPackageRepository>();
            var repositoryB = new Mock<IPackageRepository>();
            var factory = new Mock<IPackageRepositoryFactory>();
            factory.Setup(c => c.CreateRepository(It.Is<string>(a => a.Equals("A")))).Returns(repositoryA.Object);
            factory.Setup(c => c.CreateRepository(It.Is<string>(a => a.Equals("B")))).Returns(repositoryB.Object);
            var sources = new Mock<IPackageSourceProvider>();
            sources.Setup(c => c.LoadPackageSources()).Returns(new[] { new PackageSource("A"), new PackageSource("B") });

            // Act
            var repo = (AggregateRepository)sources.Object.CreateAggregateRepository(factory.Object, ignoreFailingRepositories: false);

            // Assert
            Assert.Equal(2, repo.Repositories.Count());
            Assert.Equal(repositoryA.Object, repo.Repositories.First());
            Assert.Equal(repositoryB.Object, repo.Repositories.Last());
        }

        [Fact]
        public void GetAggregateSkipsInvalidSources()
        {
            // Arrange
            var repositoryA = new Mock<IPackageRepository>();
            var repositoryC = new Mock<IPackageRepository>();
            var factory = new Mock<IPackageRepositoryFactory>();
            factory.Setup(c => c.CreateRepository(It.Is<string>(a => a.Equals("A")))).Returns(repositoryA.Object);
            factory.Setup(c => c.CreateRepository(It.Is<string>(a => a.Equals("B")))).Throws(new InvalidOperationException());
            factory.Setup(c => c.CreateRepository(It.Is<string>(a => a.Equals("C")))).Returns(repositoryC.Object);

            var sources = new Mock<IPackageSourceProvider>();
            sources.Setup(c => c.LoadPackageSources()).Returns(new[] { new PackageSource("A"), new PackageSource("B"), new PackageSource("C") });

            // Act
            var repo = (AggregateRepository)sources.Object.CreateAggregateRepository(factory.Object, ignoreFailingRepositories: true);

            // Assert
            Assert.Equal(2, repo.Repositories.Count());
            Assert.Equal(repositoryA.Object, repo.Repositories.First());
            Assert.Equal(repositoryC.Object, repo.Repositories.Last());
        }

        [Fact]
        public void GetAggregateSkipsDisabledSources()
        {
            // Arrange
            var repositoryA = new Mock<IPackageRepository>();
            var repositoryB = new Mock<IPackageRepository>();
            var factory = new Mock<IPackageRepositoryFactory>();
            factory.Setup(c => c.CreateRepository(It.Is<string>(a => a.Equals("A")))).Returns(repositoryA.Object);
            factory.Setup(c => c.CreateRepository(It.Is<string>(a => a.Equals("B")))).Returns(repositoryB.Object);
            factory.Setup(c => c.CreateRepository(It.Is<string>(a => a.Equals("C")))).Throws(new Exception());
            var sources = new Mock<IPackageSourceProvider>();
            sources.Setup(c => c.LoadPackageSources()).Returns(new[] { 
                new PackageSource("A"), new PackageSource("B", "B", isEnabled: false), new PackageSource("C", "C", isEnabled: false) });

            // Act
            var repo = (AggregateRepository)sources.Object.CreateAggregateRepository(factory.Object, ignoreFailingRepositories: false);

            // Assert
            Assert.Equal(1, repo.Repositories.Count());
            Assert.Equal(repositoryA.Object, repo.Repositories.First());
        }

        [Fact]
        public void GetAggregateHandlesInvalidUriSources()
        {
            // Arrange
            var factory = PackageRepositoryFactory.Default;
            var sources = new Mock<IPackageSourceProvider>();
            sources.Setup(c => c.LoadPackageSources()).Returns(new[] { 
                new PackageSource("Bad 1"), 
                new PackageSource(@"x:sjdkfjhsdjhfgjdsgjglhjk"), 
                new PackageSource(@"http:\\//") 
            });

            // Act
            var repo = (AggregateRepository)sources.Object.CreateAggregateRepository(factory, ignoreFailingRepositories: true);

            // Assert
            Assert.False(repo.Repositories.Any());
        }

        [Fact]
        public void GetAggregateSetsIgnoreInvalidRepositoryProperty()
        {
            // Arrange
            var factory = new Mock<IPackageRepositoryFactory>();
            bool ignoreRepository = true;

            var sources = new Mock<IPackageSourceProvider>();
            sources.Setup(c => c.LoadPackageSources()).Returns(Enumerable.Empty<PackageSource>());

            // Act
            var repo = (AggregateRepository)sources.Object.CreateAggregateRepository(factory.Object, ignoreFailingRepositories: ignoreRepository);

            // Assert
            Assert.True(repo.IgnoreFailingRepositories);
        }

        [Fact]
        public void GetAggregateWithInvalidSourcesThrows()
        {
            // Arrange
            var repositoryA = new Mock<IPackageRepository>();
            var repositoryC = new Mock<IPackageRepository>();
            var factory = new Mock<IPackageRepositoryFactory>();
            factory.Setup(c => c.CreateRepository(It.Is<string>(a => a.Equals("A")))).Returns(repositoryA.Object);
            factory.Setup(c => c.CreateRepository(It.Is<string>(a => a.Equals("B")))).Throws(new InvalidOperationException());
            factory.Setup(c => c.CreateRepository(It.Is<string>(a => a.Equals("C")))).Returns(repositoryC.Object);

            var sources = new Mock<IPackageSourceProvider>();
            sources.Setup(c => c.LoadPackageSources()).Returns(new[] { new PackageSource("A"), new PackageSource("B"), new PackageSource("C") });

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => sources.Object.CreateAggregateRepository(factory.Object, ignoreFailingRepositories: false));
        }

        [Fact]
        public void ResolveSourceLooksUpNameAndSource()
        {
            // Arrange
            var sources = new Mock<IPackageSourceProvider>();
            PackageSource source1 = new PackageSource("Source", "SourceName"), source2 = new PackageSource("http://www.test.com", "Baz");
            sources.Setup(c => c.LoadPackageSources()).Returns(new[] { source1, source2 });

            // Act
            var result1 = sources.Object.ResolveSource("http://www.test.com");
            var result2 = sources.Object.ResolveSource("Baz");
            var result3 = sources.Object.ResolveSource("SourceName");

            // Assert
            Assert.Equal(source2.Source, result1);
            Assert.Equal(source2.Source, result2);
            Assert.Equal(source1.Source, result3);
        }

        [Fact]
        public void ResolveSourceIgnoreDisabledSources()
        {
            // Arrange
            var sources = new Mock<IPackageSourceProvider>();
            PackageSource source1 = new PackageSource("Source", "SourceName");
            PackageSource source2 = new PackageSource("http://www.test.com", "Baz", isEnabled: false);
            PackageSource source3 = new PackageSource("http://www.bing.com", "Foo", isEnabled: false);
            sources.Setup(c => c.LoadPackageSources()).Returns(new[] { source1, source2, source3 });

            // Act
            var result1 = sources.Object.ResolveSource("http://www.test.com");
            var result2 = sources.Object.ResolveSource("Baz");
            var result3 = sources.Object.ResolveSource("Foo");
            var result4 = sources.Object.ResolveSource("SourceName");

            // Assert
            Assert.Equal("http://www.test.com", result1);
            Assert.Equal("Baz", result2);
            Assert.Equal("Foo", result3);
            Assert.Equal("Source", result4);
        }

        [Fact]
        public void ResolveSourceReturnsOriginalValueIfNotFoundInSources()
        {
            // Arrange
            var sources = new Mock<IPackageSourceProvider>();
            PackageSource source1 = new PackageSource("Source", "SourceName"), source2 = new PackageSource("http://www.test.com", "Baz");
            sources.Setup(c => c.LoadPackageSources()).Returns(new[] { source1, source2 });
            var source = "http://www.does-not-exist.com";

            // Act
            var result = sources.Object.ResolveSource(source);

            // Assert
            Assert.Equal(source, result);
        }

        [Fact]
        public void HighPriortySourceIsNotDisabledByLowPriorityConfigSetting()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem(@"c:\a\b");

            mockFileSystem.AddFile(
                @"c:\a\NuGet.Config",
                @"
<configuration>
    <packageSources>
        <add key='s1' value='\\server\s1' />
        <add key='s2' value='\\server\s2' />
    </packageSources>
    <disabledPackageSources>
        <add key='s1' value='true' />
        <add key='s3' value='true' />
    </disabledPackageSources>
</configuration>");

            mockFileSystem.AddFile(
                @"NuGet.Config",
                @"
<configuration>
    <packageSources>
        <add key='s3' value='\\server\s3' />
    </packageSources>
    <disabledPackageSources>
        <add key='s2' value='true' />
    </disabledPackageSources>
</configuration>");
            var settings = Settings.LoadDefaultSettings(mockFileSystem, null, null);

            var packageSourceProvider = new PackageSourceProvider(settings);

            // Act
            var sources = packageSourceProvider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(3, sources.Count);
            Assert.Equal("s3", sources[0].Name);
            // the disable source s3 setting in c:\a\nuget.Config has lower priority
            // than c:\a\b\nuget.config, so source s3 is enabled.
            Assert.True(sources[0].IsEnabled);

            Assert.Equal("s1", sources[1].Name);
            // source s1 is disabled in c:\a\Nuget.Config
            Assert.False(sources[1].IsEnabled);

            Assert.Equal("s2", sources[2].Name);
            // source s2 is disabled in c:\a\b\Nuget.Config
            Assert.False(sources[2].IsEnabled);
        }

        [Fact]
        public void LoadPackageSource_IgnoresSourcesWithProtocolVersionHigherThan2()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem(@"c:\a\b");
            mockFileSystem.AddFile(
                @"c:\a\NuGet.Config",
                @"
<configuration>
    <packageSources>
        <add key='source1' value='https://api.nuget.org/v3' protocolVersion='3' />
        <add key='source1' value='https://nuget.org/v2' />
        <add key='source2' value='https://my-new-source' />
        <add key='source2' value='https://v3.my-new-source' protocolVersion='3' />
        <add key='source1' value='https://nuget.org/packages.txt' protocolVersion='4' />
    </packageSources>
</configuration>");

            var settings = Settings.LoadDefaultSettings(mockFileSystem, configFileName: null, machineWideSettings: null);
            var packageSourceProvider = new PackageSourceProvider(settings);

            // Act
            var sources = packageSourceProvider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(2, sources.Count);
            Assert.Equal("source1", sources[0].Name);
            Assert.Equal("https://nuget.org/v2", sources[0].Source);
            Assert.Equal(2, sources[0].ProtocolVersion);

            Assert.Equal("source2", sources[1].Name);
            Assert.Equal("https://my-new-source", sources[1].Source);
            Assert.Equal(2, sources[1].ProtocolVersion);
        }

        [Fact]
        public void SavePackageSource_PreservesPackageSourceOrder()
        {
            // Arrange
            var expected =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""source1"" value=""https://api.nuget.org/v3"" protocolVersion=""3"" />
    <add key=""source1"" value=""https://www.nuget.org/v2"" />
    <add key=""source1"" value=""https://nuget.org/packages.txt"" protocolVersion=""4"" />
    <add key=""source2"" value=""https://my-new-source"" />
    <add key=""source2"" value=""https://v3.my-new-source"" protocolVersion=""3"" />
  </packageSources>
</configuration>";
            var mockFileSystem = new MockFileSystem(@"c:\a\b");
            mockFileSystem.AddFile(
                @"c:\a\NuGet.Config",
                @"
<configuration>
    <packageSources>
        <add key='source1' value='https://api.nuget.org/v3' protocolVersion='3' />
        <add key='source1' value='https://nuget.org/v2' />
        <add key='source2' value='https://my-new-source' />
        <add key='source2' value='https://v3.my-new-source' protocolVersion='3' />
        <add key='source1' value='https://nuget.org/packages.txt' protocolVersion='4' />
    </packageSources>
</configuration>");

            var settings = Settings.LoadDefaultSettings(mockFileSystem, configFileName: null, machineWideSettings: null);
            var packageSourceProvider = new PackageSourceProvider(settings);

            // Act - 1
            var sources = packageSourceProvider.LoadPackageSources().ToList();

            // Assert - 2
            Assert.Equal(2, sources.Count);
            Assert.Equal("source1", sources[0].Name);
            Assert.Equal("https://nuget.org/v2", sources[0].Source);
            Assert.Equal(2, sources[0].ProtocolVersion);

            Assert.Equal("source2", sources[1].Name);
            Assert.Equal("https://my-new-source", sources[1].Source);
            Assert.Equal(2, sources[1].ProtocolVersion);

            // Act - 2
            sources[0] = new PackageSource("https://www.nuget.org/v2", "source1");
            packageSourceProvider.SavePackageSources(sources);

            // Assert
            Assert.Equal(expected, mockFileSystem.ReadAllText(@"c:\a\NuGet.config"));
        }

        [Fact]
        public void SavePackage_UpdatesSourceInAllNuGetConfigsInFileSystem()
        {
            // Arrange
            var expected1 =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""source1"" value=""https://www.nuget.org/v2"" />
    <add key=""source2"" value=""https://my-new-source"" />
  </packageSources>
</configuration>";
            var expected2 =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""source2"" value=""https://my-new-source"" />
  </packageSources>
</configuration>";
            var mockFileSystem = new MockFileSystem(@"c:\a\b");
            mockFileSystem.AddFile(
                @"c:\a\NuGet.Config",
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""source2"" value=""https://some-source"" />
  </packageSources>
</configuration>");
            mockFileSystem.AddFile(@"NuGet.Config",
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""source1"" value=""https://current.nuget.org/v2"" />
    <add key=""source2"" value=""https://my-old-source"" />
  </packageSources>
</configuration>");


            var settings = Settings.LoadDefaultSettings(mockFileSystem, configFileName: null, machineWideSettings: null);
            var packageSourceProvider = new PackageSourceProvider(settings);

            // Act - 1
            var sources = packageSourceProvider.LoadPackageSources().ToList();

            // Assert - 2
            Assert.Equal(2, sources.Count);
            Assert.Equal("source1", sources[0].Name);
            Assert.Equal("https://current.nuget.org/v2", sources[0].Source);
            Assert.Equal(2, sources[0].ProtocolVersion);

            Assert.Equal("source2", sources[1].Name);
            Assert.Equal("https://my-old-source", sources[1].Source);
            Assert.Equal(2, sources[1].ProtocolVersion);

            // Act - 2
            sources[0] = new PackageSource("https://www.nuget.org/v2", "source1");
            sources[1] = new PackageSource("https://my-new-source", "source2");
            packageSourceProvider.SavePackageSources(sources);

            // Assert
            Assert.Equal(expected1, mockFileSystem.ReadAllText(@"NuGet.config"));
            Assert.Equal(expected2, mockFileSystem.ReadAllText(@"c:\a\NuGet.config"));
        }

        [Fact]
        public void SavePackage_UpdatesDisabledInConfigWithHighestPriority()
        {
            // Arrange
            var expected1 =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""source2"" value=""https://my-old-source"" />
  </packageSources>
  <disabledPackageSources>
    <add key=""source2"" value=""true"" />
  </disabledPackageSources>
</configuration>";
            var expected2 =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""source2"" value=""https://my-old-source"" />
    <add key=""source1"" value=""https://current.nuget.org/v2"" />
  </packageSources>
  <disabledPackageSources>
    <add key=""source1"" value=""true"" />
  </disabledPackageSources>
</configuration>";
            var mockFileSystem = new MockFileSystem(@"c:\a\b");
            mockFileSystem.AddFile(
                @"c:\a\NuGet.Config",
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""source2"" value=""https://some-source"" />
    <add key=""source1"" value=""https://current.nuget.org/v2"" />
  </packageSources>
</configuration>");
            mockFileSystem.AddFile(@"NuGet.Config",
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""source2"" value=""https://my-old-source"" />
  </packageSources>
</configuration>");

            var settings = Settings.LoadDefaultSettings(mockFileSystem, configFileName: null, machineWideSettings: null);
            var packageSourceProvider = new PackageSourceProvider(settings);

            // Act - 1
            var sources = packageSourceProvider.LoadPackageSources().ToList();

            // Assert - 2
            Assert.Equal(2, sources.Count);
            Assert.Equal("source2", sources[0].Name);
            Assert.Equal("https://my-old-source", sources[0].Source);
            Assert.Equal(2, sources[0].ProtocolVersion);

            Assert.Equal("source1", sources[1].Name);
            Assert.Equal("https://current.nuget.org/v2", sources[1].Source);
            Assert.Equal(2, sources[1].ProtocolVersion);

            // Act - 2
            sources[0].IsEnabled = false;
            sources[1].IsEnabled = false;
            packageSourceProvider.SavePackageSources(sources);

            // Assert
            Assert.Equal(expected1, mockFileSystem.ReadAllText(@"NuGet.config"));
            Assert.Equal(expected2, mockFileSystem.ReadAllText(@"c:\a\NuGet.config"));
        }

        [Fact]
        public void SavePackage_PreservesHigherProtocolSourcesThatDoNotHaveACorrespondingSupporedProtocolSource()
        {
            // Arrange
            var expected =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""source2"" value=""https://my-old-source"" />
    <add key=""source1"" value=""https://new.nuget.org/v2"" />
    <add key=""source4"" value=""https://fast.custom.source"" protocolVersion=""3"" />
  </packageSources>
</configuration>";
            var mockFileSystem = new MockFileSystem(@"c:\a\b");
            mockFileSystem.AddFile(
                @"c:\a\NuGet.Config",
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""source1"" value=""https://current.nuget.org/v2"" />
    <add key=""source2"" value=""https://my-old-source"" />
    <add key=""source3"" value=""https://mget.org/v2"" />
    <add key=""source3"" value=""https://mget.org/v3"" protocolVersion=""3"" />
    <add key=""source4"" value=""https://fast.custom.source"" protocolVersion=""3"" />
  </packageSources>
</configuration>");

            var settings = Settings.LoadDefaultSettings(mockFileSystem, configFileName: null, machineWideSettings: null);
            var packageSourceProvider = new PackageSourceProvider(settings);

            // Act - 1
            var sources = packageSourceProvider.LoadPackageSources().ToList();

            // Assert - 2
            Assert.Equal(3, sources.Count);
            Assert.Equal("source1", sources[0].Name);
            Assert.Equal("https://current.nuget.org/v2", sources[0].Source);
            Assert.Equal(2, sources[0].ProtocolVersion);

            Assert.Equal("source2", sources[1].Name);
            Assert.Equal("https://my-old-source", sources[1].Source);
            Assert.Equal(2, sources[1].ProtocolVersion);

            Assert.Equal("source3", sources[2].Name);
            Assert.Equal("https://mget.org/v2", sources[2].Source);
            Assert.Equal(2, sources[2].ProtocolVersion);

            // Act - 2
            var sourcesToSave = new[]
            {
                sources[1],
                new PackageSource("https://new.nuget.org/v2", sources[0].Name)
            };
            packageSourceProvider.SavePackageSources(sourcesToSave);

            // Assert
            Assert.Equal(expected, mockFileSystem.ReadAllText(@"c:\a\NuGet.config"));
        }

        private void AssertPackageSource(PackageSource ps, string name, string source, bool isEnabled, bool isMachineWide = false, bool isOfficial = false)
        {
            Assert.Equal(name, ps.Name);
            Assert.Equal(source, ps.Source);
            Assert.True(ps.IsEnabled == isEnabled);
            Assert.True(ps.IsMachineWide == isMachineWide);
            Assert.True(ps.IsOfficial == isOfficial);
        }

        private IPackageSourceProvider CreatePackageSourceProvider(
            ISettings settings = null,
            IEnumerable<PackageSource> providerDefaultSources = null,
            IDictionary<PackageSource, PackageSource> migratePackageSources = null,
            IEnumerable<PackageSource> configurationDefaultSources = null)
        {
            settings = settings ?? new Mock<ISettings>().Object;
            return new PackageSourceProvider(settings, providerDefaultSources, migratePackageSources, configurationDefaultSources);
        }

        private static void AssertKVP(KeyValuePair<string, string> expected, KeyValuePair<string, string> actual)
        {
            Assert.Equal(expected.Key, actual.Key);
            Assert.Equal(expected.Value, actual.Value);
        }
    }
}