using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Moq;
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
            var provider = new PackageSourceProvider(settings.Object, defaultSources: null);

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
            var provider = new PackageSourceProvider(settings.Object, defaultSources: new PackageSource[] { });

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
            var provider = new PackageSourceProvider(settings, defaultSources: new[] { new PackageSource("A"), new PackageSource("B") });

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(2, values.Count);
            Assert.Equal("A", values.First().Source);
            Assert.Equal("B", values.Last().Source);
        }

        [Fact]
        public void LoadPackageSourcesPerformMigrationIfSpecified()
        {
            // Arrange
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValues("packageSources")).Returns(
                new[] { 
                    new KeyValuePair<string, string>("one", "onesource"),
                    new KeyValuePair<string, string>("two", "twosource"),
                    new KeyValuePair<string, string>("three", "threesource"),
                }
            );

            // disable package "three"
            settings.Setup(s => s.GetValues("disabledPackageSources")).Returns(new[] { new KeyValuePair<string, string>("three", "true" ) });

            IList<KeyValuePair<string, string>> savedSettingValues = null;
            settings.Setup(s => s.SetValues("packageSources", It.IsAny<IList<KeyValuePair<string, string>>>()))
                    .Callback<string, IList<KeyValuePair<string, string>>>((_, savedVals) => { savedSettingValues = savedVals; })
                    .Verifiable();

            var provider = new PackageSourceProvider(settings.Object,
                null,
                new Dictionary<PackageSource, PackageSource> {
                    { new PackageSource("onesource", "one"), new PackageSource("goodsource", "good") },
                    { new PackageSource("foo", "bar"), new PackageSource("foo", "bar") },
                    { new PackageSource("threesource", "three"), new PackageSource("awesomesource", "awesome") }
                }
            );

            // Act
            var values = provider.LoadPackageSources().ToList();

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
            settings.Setup(s => s.GetValues("packageSources"))
                    .Returns(new[] { new KeyValuePair<string, string>("one", "one"), 
                                     new KeyValuePair<string, string>("two", "two"), 
                                     new KeyValuePair<string, string>("three", "three")
                                })
                    .Verifiable();
            settings.Setup(s => s.GetValues("disabledPackageSources")).Returns(new KeyValuePair<string, string>[0]);
            settings.Setup(s => s.GetNestedValues("packageSourceCredentials", It.IsAny<string>())).Returns(new KeyValuePair<string, string>[0]);
            settings.Setup(s => s.DeleteSection("packageSources")).Returns(true).Verifiable();
            settings.Setup(s => s.DeleteSection("disabledPackageSources")).Returns(true).Verifiable();
            settings.Setup(s => s.DeleteSection("packageSourceCredentials")).Returns(true).Verifiable();
            settings.Setup(s => s.SetValues("packageSources", It.IsAny<IList<KeyValuePair<string, string>>>()))
                    .Callback((string section, IList<KeyValuePair<string, string>> values) =>
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

            settings.Setup(s => s.SetValues("disabledPackageSources", It.IsAny<IList<KeyValuePair<string, string>>>()))
                .Callback((string section, IList<KeyValuePair<string, string>> values) =>
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
        public void LoadPackageSourcesReturnCorrectDataFromSettings()
        {
            // Arrange
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetValues("packageSources"))
                    .Returns(new[] { new KeyValuePair<string, string>("one", "onesource"), 
                                     new KeyValuePair<string, string>("two", "twosource"), 
                                     new KeyValuePair<string, string>("three", "threesource")
                                })
                    .Verifiable();
            settings.Setup(s => s.GetValues("disabledPackageSources")).Returns(new KeyValuePair<string, string>[0]);
            settings.Setup(s => s.GetNestedValues("packageSourceCredentials", It.IsAny<string>())).Returns(new KeyValuePair<string, string>[0]);

            var provider = CreatePackageSourceProvider(settings.Object);

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(3, values.Count);
            AssertPackageSource(values[0], "one", "onesource", true);
            AssertPackageSource(values[1], "two", "twosource", true);
            AssertPackageSource(values[2], "three", "threesource", true);
        }

        [Fact]
        public void LoadPackageSourcesReturnCorrectDataFromSettingsWhenSomePackageSourceIsDisabled()
        {
            // Arrange
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetValues("packageSources"))
                    .Returns(new[] { new KeyValuePair<string, string>("one", "onesource"), 
                                     new KeyValuePair<string, string>("two", "twosource"), 
                                     new KeyValuePair<string, string>("three", "threesource")
                                });

            settings.Setup(s => s.GetValues("disabledPackageSources")).Returns(new[] { new KeyValuePair<string, string>("two", "true") });
            settings.Setup(s => s.GetNestedValues("packageSourceCredentials", It.IsAny<string>())).Returns(new KeyValuePair<string, string>[0]);

            var provider = CreatePackageSourceProvider(settings.Object);

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(3, values.Count);
            AssertPackageSource(values[0], "one", "onesource", true);
            AssertPackageSource(values[1], "two", "twosource", false);
            AssertPackageSource(values[2], "three", "threesource", true);
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
            settings.Setup(s => s.GetValue("disabledPackageSources", "A")).Returns("sdfds");
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
            settings.Setup(s => s.GetValue("disabledPackageSources", "A")).Returns(returnValue);
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
            settings.Setup(s => s.GetValues("packageSources"))
                    .Returns(new[] { new KeyValuePair<string, string>("one", "onesource"), 
                                     new KeyValuePair<string, string>("two", "twosource"), 
                                     new KeyValuePair<string, string>("three", "threesource")
                                });

            settings.Setup(s => s.GetNestedValues("packageSourceCredentials", "two"))
                    .Returns(new [] { new KeyValuePair<string, string>("Username", userName), new KeyValuePair<string, string>("Password", password) });

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
            settings.Setup(s => s.GetValues("packageSources"))
                    .Returns(new[] { new KeyValuePair<string, string>("one", "onesource"), 
                                     new KeyValuePair<string, string>("two", "twosource"), 
                                     new KeyValuePair<string, string>("three", "threesource")
                                });

            settings.Setup(s => s.GetNestedValues("packageSourceCredentials", "two"))
                    .Returns(new[] { new KeyValuePair<string, string>("Username", "user1"), new KeyValuePair<string, string>("Password", encryptedPassword) });

            var provider = CreatePackageSourceProvider(settings.Object);

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(3, values.Count);
            AssertPackageSource(values[1], "two", "twosource", true);
            Assert.Equal("user1", values[1].UserName);
            Assert.Equal("topsecret", values[1].Password);
        }

        [Fact]
        public void SavePackageSourcesSaveCorrectDataToSettings()
        {
            // Arrange
            var sources = new[] { new PackageSource("one"), new PackageSource("two"), new PackageSource("three") };
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.DeleteSection("packageSources")).Returns(true).Verifiable();
            settings.Setup(s => s.DeleteSection("disabledPackageSources")).Returns(true).Verifiable();
            settings.Setup(s => s.DeleteSection("packageSourceCredentials")).Returns(true).Verifiable();

            settings.Setup(s => s.SetValues("packageSources", It.IsAny<IList<KeyValuePair<string, string>>>()))
                    .Callback((string section, IList<KeyValuePair<string, string>> values) =>
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

            settings.Setup(s => s.SetValues("disabledPackageSources", It.IsAny<IList<KeyValuePair<string, string>>>()))
                    .Callback((string section, IList<KeyValuePair<string, string>> values) =>
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
            settings.Setup(s => s.DeleteSection("disabledPackageSources")).Returns(true).Verifiable();
            settings.Setup(s => s.SetValues("disabledPackageSources", It.IsAny<IList<KeyValuePair<string, string>>>()))
                    .Callback((string section, IList<KeyValuePair<string, string>> values) =>
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
            settings.Setup(s => s.DeleteSection("packageSources")).Returns(true).Verifiable();
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
            var repo = (AggregateRepository)sources.Object.GetAggregate(factory.Object);

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
            var repo = (AggregateRepository)sources.Object.GetAggregate(factory.Object, ignoreFailingRepositories: true);

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
            var repo = (AggregateRepository)sources.Object.GetAggregate(factory.Object);

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
            var repo = (AggregateRepository)sources.Object.GetAggregate(factory, ignoreFailingRepositories: true);

            // Assert
            Assert.False(repo.Repositories.Any());
        }

        [Fact]
        public void GetAggregateSetsIgnoreInvalidRepositoryProperty()
        {
            // Arrange
            var repositoryA = new Mock<IPackageRepository>();
            var repositoryC = new Mock<IPackageRepository>();
            var factory = new Mock<IPackageRepositoryFactory>();
            bool ignoreRepository = true;

            var sources = new Mock<IPackageSourceProvider>();
            sources.Setup(c => c.LoadPackageSources()).Returns(Enumerable.Empty<PackageSource>());

            // Act
            var repo = (AggregateRepository)sources.Object.GetAggregate(factory.Object, ignoreFailingRepositories: ignoreRepository);

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
            ExceptionAssert.Throws<InvalidOperationException>(() => sources.Object.GetAggregate(factory.Object, ignoreFailingRepositories: false));
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

        private void AssertPackageSource(PackageSource ps, string name, string source, bool isEnabled)
        {
            Assert.Equal(name, ps.Name);
            Assert.Equal(source, ps.Source);
            Assert.True(ps.IsEnabled == isEnabled);
        }

        private IPackageSourceProvider CreatePackageSourceProvider(ISettings settings = null)
        {
            settings = settings ?? new Mock<ISettings>().Object;
            return new PackageSourceProvider(settings);
        }

        private static void AssertKVP(KeyValuePair<string, string> expected, KeyValuePair<string, string> actual)
        {
            Assert.Equal(expected.Key, actual.Key);
            Assert.Equal(expected.Value, actual.Value);
        }
    }
}