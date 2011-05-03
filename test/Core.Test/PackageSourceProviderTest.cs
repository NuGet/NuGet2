using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace NuGet.Test {

    [TestClass]
    public class PackageSourceProviderTest {

        [TestMethod]
        public void TestNoPackageSourcesAreReturnedIfUserSettingsIsEmpty() {
            // Arrange
            var provider = CreatePackageSourceProvider();

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.AreEqual(0, values.Count);
        }

        [TestMethod]
        public void CallSaveMethodAndLoadMethodShouldReturnTheSamePackageSet() {
            // Arrange
            var provider = CreatePackageSourceProvider();

            var sources = new[] { new PackageSource("one"), new PackageSource("two"), new PackageSource("three") };
            provider.SavePackageSources(sources);

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.AreEqual(sources.Length, values.Count);
            for (int i = 0; i < sources.Length; i++) {
                AssertPackageSource(values[i], sources[i].Name, sources[i].Source);
            }
        }

        [TestMethod]
        public void LoadPackageSourcesReturnCorrectDataFromSettings() {
            // Arrange
            var settings = new MockUserSettingsManager();
            settings.SetValues(PackageSourceProvider.FileSettingsSectionName,
                new[] {
                    new KeyValuePair<string, string>("one", "onesource"),
                    new KeyValuePair<string, string>("two", "twosource"),
                    new KeyValuePair<string, string>("three", "threesource")
                });

            var provider = CreatePackageSourceProvider(settings);

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.AreEqual(3, values.Count);
            AssertPackageSource(values[0], "one", "onesource");
            AssertPackageSource(values[1], "two", "twosource");
            AssertPackageSource(values[2], "three", "threesource");
        }

        [TestMethod]
        public void SavePackageSourcesSaveCorrectDataToSettings() {
            // Arrange
            var settings = new MockUserSettingsManager();
            var provider = CreatePackageSourceProvider(settings);

            var sources = new[] { new PackageSource("one"), new PackageSource("two"), new PackageSource("three") };

            // Act
            provider.SavePackageSources(sources);

            // Assert
            var values = settings.GetValues(PackageSourceProvider.FileSettingsSectionName);
            Assert.AreEqual(3, values.Count);
            Assert.AreEqual("one", values[0].Key);
            Assert.AreEqual("two", values[1].Key);
            Assert.AreEqual("three", values[2].Key);
        }

        [TestMethod]
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
            Assert.AreEqual(2, repo.Repositories.Count());
            Assert.AreEqual(repositoryA.Object, repo.Repositories.First());
            Assert.AreEqual(repositoryB.Object, repo.Repositories.Last());
        }

        [TestMethod]
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
            Assert.AreEqual(2, repo.Repositories.Count());
            Assert.AreEqual(repositoryA.Object, repo.Repositories.First());
            Assert.AreEqual(repositoryC.Object, repo.Repositories.Last());
        }

        [TestMethod]
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
            Assert.IsTrue(repo.IgnoreFailingRepositories);
        }

        [TestMethod]
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

        [TestMethod]
        public void ResolveSourceLooksUpNameAndSource() {
            // Arrange
            var sources = new Mock<IPackageSourceProvider>();
            PackageSource source1 = new PackageSource("Source", "SourceName"), source2 = new PackageSource("http://www.test.com", "Baz");
            sources.Setup(c => c.LoadPackageSources()).Returns(new[] {  source1, source2 });

            // Act
            var result1 = sources.Object.ResolveSource("http://www.test.com");
            var result2 = sources.Object.ResolveSource("Baz");
            var result3 = sources.Object.ResolveSource("SourceName");

            // Assert
            Assert.AreEqual(source2.Source, result1);
            Assert.AreEqual(source2.Source, result2);
            Assert.AreEqual(source1.Source, result3);
        }

        [TestMethod]
        public void ResolveSourceReturnsOriginalValueIfNotFoundInSources() {
            // Arrange
            var sources = new Mock<IPackageSourceProvider>();
            PackageSource source1 = new PackageSource("Source", "SourceName"), source2 = new PackageSource("http://www.test.com", "Baz");
            sources.Setup(c => c.LoadPackageSources()).Returns(new[] { source1, source2 });
            var source = "http://www.does-not-exist.com";

            // Act
            var result = sources.Object.ResolveSource(source);
            
            // Assert
            Assert.AreEqual(source, result);
        }

        private void AssertPackageSource(PackageSource ps, string name, string source) {
            Assert.AreEqual(name, ps.Name);
            Assert.AreEqual(source, ps.Source);
        }

        private IPackageSourceProvider CreatePackageSourceProvider(ISettings settings = null) {
            settings = settings ?? new MockUserSettingsManager();
            return new PackageSourceProvider(settings);
        }
    }
}