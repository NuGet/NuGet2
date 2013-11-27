using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    using NuGet.Test;

    public class FallbackRepositoryTest
    {
        [Fact]
        public void CreatePackageManagerUsesPrimaryRepositoryAsdependencyResolverIfUseFallbackIsFalse()
        {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();

            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository();

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("A", "1.2"));

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2 });
            mockFileSystemProvider.Setup(f => f.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s =>
            {
                switch (s)
                {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    default: return null;
                }
            });

            var packageManagerFactory = new Mock<VsPackageManagerFactory>(new Mock<ISolutionManager>().Object, mockRepositoryFactory.Object, mockSourceProvider.Object, mockFileSystemProvider.Object, new Mock<IRepositorySettings>().Object, new Mock<VsPackageInstallerEvents>().Object, mockRepository1, null);
            packageManagerFactory.Setup(f => f.GetConfigSettingsFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());

            // Act
            var packageManager = packageManagerFactory.Object.CreatePackageManager(mockRepository1, useFallbackForDependencies: false);

            // Assert
            Assert.Equal(mockRepository1, packageManager.SourceRepository);
        }

        [Fact]
        public void CreatePackageManagerUsesFallbackRepositoryyAsDependencyResolverIfUseFallbackIsTrue()
        {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();

            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository();

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("A", "1.2"));

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2 });
            mockFileSystemProvider.Setup(f => f.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s =>
            {
                switch (s)
                {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    default: return null;
                }
            });

            var packageManagerFactory = new Mock<VsPackageManagerFactory>(new Mock<ISolutionManager>().Object, mockRepositoryFactory.Object, mockSourceProvider.Object, mockFileSystemProvider.Object, new Mock<IRepositorySettings>().Object, new Mock<VsPackageInstallerEvents>().Object, mockRepository1, null);
            packageManagerFactory.Setup(f => f.GetConfigSettingsFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());

            // Act
            var packageManager = packageManagerFactory.Object.CreatePackageManager(mockRepository1, useFallbackForDependencies: true);

            // Assert
            Assert.IsType(typeof(FallbackRepository), packageManager.SourceRepository);
            var fallbackRepo = (FallbackRepository)packageManager.SourceRepository;
            Assert.IsType(typeof(AggregateRepository), fallbackRepo.DependencyResolver);
            var dependencyResolver = (AggregateRepository)fallbackRepo.DependencyResolver;
            Assert.Equal(2, dependencyResolver.Repositories.Count());
            Assert.Equal(mockRepository1, dependencyResolver.Repositories.First());
            Assert.Equal(mockRepository2, dependencyResolver.Repositories.Last());
        }

        [Fact]
        public void ResolveDependencyReturnsPackagesFromAggregateSources()
        {
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
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s =>
            {
                switch (s)
                {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    default: return null;
                }
            });
            var packageManagerFactory = new VsPackageManagerFactory(new Mock<ISolutionManager>().Object, mockRepositoryFactory.Object, mockSourceProvider.Object, new Mock<IFileSystemProvider>().Object, new Mock<IRepositorySettings>().Object, new Mock<VsPackageInstallerEvents>().Object, mockRepository1, machineWideSettings: null);

            // Act
            var repository = packageManagerFactory.CreateFallbackRepository(mockRepository1);

            // Assert
            var dependencyResolver = repository as IDependencyResolver;
            IPackage dependency = dependencyResolver.ResolveDependency(new PackageDependency("A", new VersionSpec { MinVersion = new SemanticVersion("1.0.0.0") }), null, allowPrereleaseVersions: false, preferListedPackages: false, dependencyVersion: DependencyVersion.Lowest);
            List<IPackage> packages = repository.GetPackages().ToList();

            // Assert
            Assert.Equal(1, packages.Count());
            Assert.Equal("A", packages[0].Id);
            Assert.Equal(new SemanticVersion("1.0"), packages[0].Version);

            Assert.NotNull(dependency);
            Assert.Equal("A", dependency.Id);
            Assert.Equal(new SemanticVersion("1.2"), dependency.Version);
        }

        [Fact]
        public void CreateFallbackRepositoryReturnsCurrentIfCurrentIsAggregateRepository()
        {
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
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s =>
            {
                switch (s)
                {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    default: return null;
                }
            });
            var packageManagerFactory = new VsPackageManagerFactory(new Mock<ISolutionManager>().Object, mockRepositoryFactory.Object, mockSourceProvider.Object, new Mock<IFileSystemProvider>().Object, new Mock<IRepositorySettings>().Object, new Mock<VsPackageInstallerEvents>().Object, mockRepository1, machineWideSettings: null);

            // Act
            var repository = packageManagerFactory.CreateFallbackRepository(aggregateRepo);

            // Assert
            Assert.Equal(aggregateRepo, repository);
        }

        [Fact]
        public void CreateFallbackRepositoryUsesResolvedSourceNameWhenEnsuringRepositoryIsNotAlreadyListedInAggregate()
        {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();

            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository("http://redirected");
            var aggregateRepo = new AggregateRepository(new[] { mockRepository1, mockRepository2 });

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");
            var aggregateSource = AggregatePackageSource.Instance;

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("A", "1.2"));

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2, aggregateSource });
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s =>
            {
                switch (s)
                {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    default: return null;
                }
            });
            var packageManagerFactory = new VsPackageManagerFactory(new Mock<ISolutionManager>().Object, mockRepositoryFactory.Object, mockSourceProvider.Object, new Mock<IFileSystemProvider>().Object, new Mock<IRepositorySettings>().Object, new Mock<VsPackageInstallerEvents>().Object, mockRepository1, machineWideSettings: null);

            // Act
            FallbackRepository repository = (FallbackRepository)packageManagerFactory.CreateFallbackRepository(mockRepository2);

            // Assert
            var dependencyResolver = (AggregateRepository)repository.DependencyResolver;
            Assert.Equal(2, dependencyResolver.Repositories.Count());
        }

        [Fact]
        public void CreateFallbackRepositoryDoesNotThrowWhenIteratingOverFailingRepositories()
        {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();

            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository("http://redirected");

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");
            var source3 = new PackageSource("SourceBad");
            var aggregateSource = AggregatePackageSource.Instance;

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source3, source2, aggregateSource });
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s =>
            {
                switch (s)
                {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    case "SourceBad": throw new InvalidOperationException();
                    default: return null;
                }
            });
            var packageManagerFactory = new VsPackageManagerFactory(new Mock<ISolutionManager>().Object, mockRepositoryFactory.Object, mockSourceProvider.Object, new Mock<IFileSystemProvider>().Object, new Mock<IRepositorySettings>().Object, new Mock<VsPackageInstallerEvents>().Object, mockRepository1, machineWideSettings: null);

            // Act
            FallbackRepository repository = (FallbackRepository)packageManagerFactory.CreateFallbackRepository(mockRepository2);

            // Assert
            var dependencyResolver = (AggregateRepository)repository.DependencyResolver;
            Assert.Equal(2, dependencyResolver.Repositories.Count());
        }

        [Fact]
        public void CreateFallbackRepositoryIncludesRepositoryOnceInAggregateDependencyResolver()
        {
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
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s =>
            {
                switch (s)
                {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    default: return null;
                }
            });
            var packageManagerFactory = new VsPackageManagerFactory(new Mock<ISolutionManager>().Object, mockRepositoryFactory.Object, mockSourceProvider.Object, new Mock<IFileSystemProvider>().Object, new Mock<IRepositorySettings>().Object, new Mock<VsPackageInstallerEvents>().Object, mockRepository1, machineWideSettings: null);

            // Act
            var repository = packageManagerFactory.CreateFallbackRepository(mockRepository1);

            // Assert
            var fallbackRepo = repository as FallbackRepository;
            var aggregateRepo = (AggregateRepository)fallbackRepo.DependencyResolver;
            Assert.Equal(2, aggregateRepo.Repositories.Count());
            Assert.Equal(mockRepository1, aggregateRepo.Repositories.First());
            Assert.Equal(mockRepository2, aggregateRepo.Repositories.Last());
        }

        [Fact]
        public void FallbackRepositoryDoesNotQueryDependencyResolverIfPrimaryRepositoryContainsRequiredDependency()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("M1", "1.0");
            var dependencyResolver = new Mock<IPackageRepository>(MockBehavior.Strict);
            dependencyResolver.As<IDependencyResolver>()
                              .Setup(c => c.ResolveDependency(It.IsAny<PackageDependency>(), It.IsAny<IPackageConstraintProvider>(), false, It.IsAny<bool>(), DependencyVersion.Lowest))
                              .Throws(new Exception("This method should not be called."));
            var primaryRepository = new MockPackageRepository();
            primaryRepository.AddPackage(package);
            var fallbackRepository = new FallbackRepository(primaryRepository, dependencyResolver.Object);

            // Act
            var resolvedPackage = fallbackRepository.ResolveDependency(new PackageDependency("M1"), false, false);

            // Assert
            Assert.Same(package, resolvedPackage);
        }

        [Fact]
        public void FallbackRepositoryRetursNullIfPrimaryRepositoryDoesNotHaveDependencyAndDependencyResolverThrows()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("M1", "1.0");
            var dependencyResolver = new Mock<IPackageRepository>(MockBehavior.Strict);
            dependencyResolver.As<IDependencyResolver>()
                              .Setup(c => c.ResolveDependency(It.IsAny<PackageDependency>(), It.IsAny<IPackageConstraintProvider>(), false, It.IsAny<bool>(), DependencyVersion.Lowest))
                              .Throws(new Exception("Connection failure."));
            var aggregateRepository = new AggregateRepository(new[] { dependencyResolver.Object }) { IgnoreFailingRepositories = true };
            var primaryRepository = new MockPackageRepository();
            primaryRepository.AddPackage(package);
            var fallbackRepository = new FallbackRepository(primaryRepository, aggregateRepository);

            // Act
            var resolvedPackage = fallbackRepository.ResolveDependency(new PackageDependency("M2", new VersionSpec { MinVersion = new SemanticVersion("1.0.1") }), false, false);

            // Assert
            Assert.Null(resolvedPackage);
        }

        [Fact]
        public void FallbackRepositoryUsesDependencyResolverIfPrimaryRepositoryDoesNotHaveRequiredDependency()
        {
            // Arrange
            IPackage packageA10 = PackageUtility.CreatePackage("M1", "1.0"), packageA11 = PackageUtility.CreatePackage("M2", "1.1");

            var primaryRepository = new MockPackageRepository();
            primaryRepository.AddPackage(packageA10);
            var dependencyResolver = new MockPackageRepository();
            dependencyResolver.AddPackage(packageA11);
            var fallbackRepository = new FallbackRepository(primaryRepository, dependencyResolver);

            // Act
            var resolvedPackage = fallbackRepository.ResolveDependency(new PackageDependency("M2", new VersionSpec { MinVersion = new SemanticVersion("1.0.1") }), false, false);

            // Assert
            Assert.Same(resolvedPackage, packageA11);
        }

        [Fact]
        public void FallbackRepositoryCallsPackagesFindPackageOnThePrimaryRepository()
        {
            // Arrange
            var primaryRepository = new Mock<IPackageRepository>(MockBehavior.Strict);
            IPackage package = PackageUtility.CreatePackage("A", "1.0");

            primaryRepository.As<IPackageLookup>().Setup(p => p.FindPackage("A", new SemanticVersion("1.0"))).Returns(package).Verifiable();

            var fallbackRepository = new FallbackRepository(primaryRepository.Object, new MockPackageRepository());

            // Act
            IPackage foundPackage = fallbackRepository.FindPackage("A", new SemanticVersion("1.0"));

            // Assert
            primaryRepository.VerifyAll();
            Assert.Same(package, foundPackage);
        }

        [Fact]
        public void ExistsReturnsTrueIfPrimaryRepositoryContainsPackage()
        {
            // Arrange
            IPackage package = PackageUtility.CreatePackage("A", "1.0");
            var primaryRepository = new Mock<IPackageRepository>(MockBehavior.Strict);
            primaryRepository.As<IPackageLookup>()
                             .Setup(s => s.Exists("A", new SemanticVersion("1.0")))
                             .Returns(true)
                             .Verifiable();
            var fallbackRepository = new FallbackRepository(primaryRepository.Object, new MockPackageRepository());

            // Act
            var exists = fallbackRepository.Exists("A", new SemanticVersion("1.0"));

            // Assert
            Assert.True(exists);
            primaryRepository.Verify();
        }
    }
}
