using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Moq;

namespace NuGet.Test {
    
    public class PackageSourceProviderTest {
        [Fact]
        public void TestNoPackageSourcesAreReturnedIfUserSettingsIsEmpty() {
            // Arrange
            var provider = CreatePackageSourceProvider();

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(0, values.Count);
        }

        [Fact]
        public void LoadPackageSourcesReturnsEmptySequenceIfDefaultPackageSourceIsNull() {
            // Arrange
            var settings = new MockUserSettingsManager();
            var provider = new PackageSourceProvider(settings, defaultSources: null);

            // Act
            var values = provider.LoadPackageSources();

            // Assert
            Assert.False(values.Any());
        }

        [Fact]
        public void LoadPackageSourcesReturnsEmptySequenceIfDefaultPackageSourceIsEmpty() {
            // Arrange
            var settings = new MockUserSettingsManager();
            var provider = new PackageSourceProvider(settings, defaultSources: new PackageSource[] { });

            // Act
            var values = provider.LoadPackageSources();

            // Assert
            Assert.False(values.Any());
        }

        [Fact]
        public void LoadPackageSourcesReturnsDefaultSourcesIfSpecified() {
            // Arrange
            var settings = new MockUserSettingsManager();
            var provider = new PackageSourceProvider(settings, defaultSources: new[] { new PackageSource("A"), new PackageSource("B") });

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(2, values.Count);
            Assert.Equal("A", values.First().Source);
            Assert.Equal("B", values.Last().Source);
        }

        [Fact]
        public void LoadPackageSourcesPerformMigrationIfSpecified() {
            // Arrange
            var settings = new MockUserSettingsManager();
            settings.SetValues(
                PackageSourceProvider.PackageSourcesSectionName,
                new KeyValuePair<string, string>[] { 
                    new KeyValuePair<string, string>("one", "onesource"),
                    new KeyValuePair<string, string>("two", "twosource"),
                    new KeyValuePair<string, string>("three", "threesource"),
                }
            );

            // disable package "three"
            settings.SetValue(
                PackageSourceProvider.DisabledPackageSourcesSectionName,
                "three",
                "threesource");

            var provider = new PackageSourceProvider(
                settings,
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

            var savedSettingValues = settings.GetValues(PackageSourceProvider.PackageSourcesSectionName);
            Assert.Equal(3, savedSettingValues.Count);
            Assert.Equal("good", savedSettingValues[0].Key);
            Assert.Equal("goodsource", savedSettingValues[0].Value);
            Assert.Equal("two", savedSettingValues[1].Key);
            Assert.Equal("twosource", savedSettingValues[1].Value);
            Assert.Equal("awesome", savedSettingValues[2].Key);
            Assert.Equal("awesomesource", savedSettingValues[2].Value);
        }

        [Fact]
        public void CallSaveMethodAndLoadMethodShouldReturnTheSamePackageSet() {
            // Arrange
            var provider = CreatePackageSourceProvider();

            var sources = new[] { new PackageSource("one"), new PackageSource("two"), new PackageSource("three") };
            provider.SavePackageSources(sources);

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(sources.Length, values.Count);
            for (int i = 0; i < sources.Length; i++) {
                AssertPackageSource(values[i], sources[i].Name, sources[i].Source, true);
            }
        }

        [Fact]
        public void LoadPackageSourcesReturnCorrectDataFromSettings() {
            // Arrange
            var settings = new MockUserSettingsManager();
            settings.SetValues(PackageSourceProvider.PackageSourcesSectionName,
                new[] {
                    new KeyValuePair<string, string>("one", "onesource"),
                    new KeyValuePair<string, string>("two", "twosource"),
                    new KeyValuePair<string, string>("three", "threesource")
                });

            var provider = CreatePackageSourceProvider(settings);

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(3, values.Count);
            AssertPackageSource(values[0], "one", "onesource", true);
            AssertPackageSource(values[1], "two", "twosource", true);
            AssertPackageSource(values[2], "three", "threesource", true);
        }

        [Fact]
        public void LoadPackageSourcesReturnCorrectDataFromSettingsWhenSomePackageSourceIsDisabled() {
            // Arrange
            var settings = new MockUserSettingsManager();
            settings.SetValues(PackageSourceProvider.PackageSourcesSectionName,
                new[] {
                    new KeyValuePair<string, string>("one", "onesource"),
                    new KeyValuePair<string, string>("two", "twosource"),
                    new KeyValuePair<string, string>("three", "threesource")
                });
            settings.SetValues(PackageSourceProvider.DisabledPackageSourcesSectionName,
                new[] {
                    new KeyValuePair<string, string>("two", "true")
                });

            var provider = CreatePackageSourceProvider(settings);

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(3, values.Count);
            AssertPackageSource(values[0], "one", "onesource", true);
            AssertPackageSource(values[1], "two", "twosource", false);
            AssertPackageSource(values[2], "three", "threesource", true);
        }

        [Fact]
        public void SavePackageSourcesSaveCorrectDataToSettings() {
            // Arrange
            var settings = new MockUserSettingsManager();
            var provider = CreatePackageSourceProvider(settings);

            var sources = new[] { new PackageSource("one"), new PackageSource("two"), new PackageSource("three") };

            // Act
            provider.SavePackageSources(sources);

            // Assert
            var values = settings.GetValues(PackageSourceProvider.PackageSourcesSectionName);
            Assert.Equal(3, values.Count);
            Assert.Equal("one", values[0].Key);
            Assert.Equal("two", values[1].Key);
            Assert.Equal("three", values[2].Key);
        }

        [Fact]
        public void SavePackageSourcesSaveCorrectDataToSettingsWhenSomePackgeSourceIsDisabled() {
            // Arrange
            var settings = new MockUserSettingsManager();
            var provider = CreatePackageSourceProvider(settings);

            var sources = new[] { new PackageSource("one"), new PackageSource("two"), new PackageSource("three", "three", isEnabled: false) };

            // Act
            provider.SavePackageSources(sources);

            // Assert
            var values = settings.GetValues(PackageSourceProvider.PackageSourcesSectionName);
            Assert.Equal(3, values.Count);
            Assert.Equal("one", values[0].Key);
            Assert.Equal("two", values[1].Key);
            Assert.Equal("three", values[2].Key);

            var disabledValues = settings.GetValues(PackageSourceProvider.DisabledPackageSourcesSectionName);
            Assert.NotNull(disabledValues);
            Assert.Equal(1, disabledValues.Count);
            Assert.Equal("three", disabledValues[0].Key);
        }

        [Fact]
        public void GetAggregateReturnsAggregateRepositoryForAllSources() {
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
        public void GetAggregateSkipsInvalidSources() {
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
        public void GetAggregateSkipsDisabledSources() {
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
        public void GetAggregateHandlesInvalidUriSources() {
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
        public void GetAggregateSetsIgnoreInvalidRepositoryProperty() {
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
        public void GetAggregateWithInvalidSourcesThrows() {
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
        public void ResolveSourceLooksUpNameAndSource() {
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
        public void ResolveSourceIgnoreDisabledSources() {
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
        public void ResolveSourceReturnsOriginalValueIfNotFoundInSources() {
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

        private void AssertPackageSource(PackageSource ps, string name, string source, bool isEnabled) {
            Assert.Equal(name, ps.Name);
            Assert.Equal(source, ps.Source);
            Assert.True(ps.IsEnabled == isEnabled);
        }

        private IPackageSourceProvider CreatePackageSourceProvider(ISettings settings = null) {
            settings = settings ?? new MockUserSettingsManager();
            return new PackageSourceProvider(settings);
        }
    }
}