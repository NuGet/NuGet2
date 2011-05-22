using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test.Mocks;

namespace NuGet.VisualStudio.Test {
    using NuGet.Test;

    [TestClass]
    public class FallbackRepositoryTest {
        [TestMethod]
        public void GetDependenciesReturnsPackagesFromAggregateSources() {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();

            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository();

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("A", "1.2"));

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2 });
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s => {
                switch (s) {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    default: return null;
                }
            });
            var packageManagerFactory = new VsPackageManagerFactory(new Mock<ISolutionManager>().Object, mockRepositoryFactory.Object, mockSourceProvider.Object, new Mock<IFileSystemProvider>().Object, new Mock<IRepositorySettings>().Object, null);

            // Act
            var repository = packageManagerFactory.CreateFallBackRepository(mockRepository1);

            // Assert
            var dependencyProvider = repository as IDependencyProvider;
            List<IPackage> dependencies = dependencyProvider.GetDependencies("A").ToList();
            List<IPackage> packages = repository.GetPackages().ToList();

            // Assert
            Assert.AreEqual(1, packages.Count());
            Assert.AreEqual("A", dependencies[0].Id);
            Assert.AreEqual(new Version("1.0"), dependencies[0].Version);

            Assert.AreEqual(2, dependencies.Count);
            Assert.AreEqual("A", dependencies[0].Id);
            Assert.AreEqual(new Version("1.0"), dependencies[0].Version);

            Assert.AreEqual("A", dependencies[1].Id);
            Assert.AreEqual(new Version("1.2"), dependencies[1].Version);
        }

        [TestMethod]
        public void CreateFallbackRepositoryReturnsCurrentIfCurrentIsAggregateRepository() {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();

            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository();
            var aggregateRepo = new AggregateRepository(new[] { mockRepository1, mockRepository2 });

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");
            var aggregateSource = AggregatePackageSource.Instance;

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("A", "1.2"));

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2, aggregateSource });
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s => {
                switch (s) {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    default: return null;
                }
            });
            var packageManagerFactory = new VsPackageManagerFactory(new Mock<ISolutionManager>().Object, mockRepositoryFactory.Object, mockSourceProvider.Object, new Mock<IFileSystemProvider>().Object, new Mock<IRepositorySettings>().Object, null);

            // Act
            var repository = packageManagerFactory.CreateFallBackRepository(aggregateRepo);

            // Assert
            Assert.AreEqual(aggregateRepo, repository);
        }

        [TestMethod]
        public void CreateFallbackRepositoryIncludesRepositoryOnceInAggregateDependencyResolver() {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();

            var mockRepository1 = new MockPackageRepository("Source1");
            var mockRepository2 = new MockPackageRepository("Source2");

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("A", "1.2"));

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2 });
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s => {
                switch (s) {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    default: return null;
                }
            });
            var packageManagerFactory = new VsPackageManagerFactory(new Mock<ISolutionManager>().Object, mockRepositoryFactory.Object, mockSourceProvider.Object, new Mock<IFileSystemProvider>().Object, new Mock<IRepositorySettings>().Object, null);

            // Act
            var repository = packageManagerFactory.CreateFallBackRepository(mockRepository1);

            // Assert
            var fallbackRepo = repository as FallbackRepository;
            var aggregateRepo = (AggregateRepository)fallbackRepo.DependencyResolver;
            Assert.AreEqual(2, aggregateRepo.Repositories.Count());
            Assert.AreEqual(mockRepository1, aggregateRepo.Repositories.First());
            Assert.AreEqual(mockRepository2, aggregateRepo.Repositories.Last());
        }
    }
}
