using System.Linq;
using Moq;
using NuGet.Test;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    public class PackageRestoreRepositoryTest
    {
        [Fact]
        public void CreatePackageMananagerForPackageRestoreUsesPackageRestoreRepositoryWithAggregateSecondary()
        {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();

            var mockRepository1 = new MockPackageRepository("Source1");
            var mockRepository2 = new MockPackageRepository("Source2");
            var mockRepository3 = new MockPackageRepository("Source3");

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");
            var source3 = new PackageSource("Source3");

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("A", "1.2"));
            mockRepository3.AddPackage(PackageUtility.CreatePackage("A", "1.4"));

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2, source3 });
            mockFileSystemProvider.Setup(f => f.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s =>
            {
                switch (s)
                {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    case "Source3": return mockRepository3;
                    default: return null;
                }
            });

            var packageManagerFactory = new Mock<VsPackageManagerFactory>(new Mock<ISolutionManager>().Object, mockRepositoryFactory.Object, mockSourceProvider.Object, mockFileSystemProvider.Object, new Mock<IRepositorySettings>().Object, new Mock<VsPackageInstallerEvents>().Object, mockRepository1, null);
            packageManagerFactory.Setup(f => f.GetConfigSettingsFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());

            // Act
            var packageManager = packageManagerFactory.Object.CreatePackageManagerWithAllPackageSources(mockRepository1);

            // Assert
            Assert.IsType(typeof(PriorityPackageRepository), packageManager.SourceRepository);
            var packageRestoreRepository = (PriorityPackageRepository)packageManager.SourceRepository;
            Assert.Equal(mockRepository1, packageRestoreRepository.PrimaryRepository);
            Assert.IsType(typeof(AggregateRepository), packageRestoreRepository.SecondaryRepository);
            var secondaryRepository = (AggregateRepository)packageRestoreRepository.SecondaryRepository;
            var secondaryRepositories = secondaryRepository.Repositories.ToList();
            Assert.Equal(2, secondaryRepositories.Count);
            Assert.Equal(mockRepository2, secondaryRepositories[0]);
            Assert.Equal(mockRepository3, secondaryRepositories[1]);
        }

        [Fact]
        public void CreatePackageMananagerForPackageRestoreUsesPackageRestoreRepositoryWithNonAggregateSecondary()
        {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();

            var mockRepository1 = new MockPackageRepository("Source1");
            var mockRepository2 = new MockPackageRepository("Source2");

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
            var packageManager = packageManagerFactory.Object.CreatePackageManagerWithAllPackageSources(mockRepository1);

            // Assert
            Assert.IsType(typeof(PriorityPackageRepository), packageManager.SourceRepository);
            var packageRestoreRepository = (PriorityPackageRepository)packageManager.SourceRepository;
            Assert.Equal(mockRepository1, packageRestoreRepository.PrimaryRepository);
            Assert.IsType(typeof(MockPackageRepository), packageRestoreRepository.SecondaryRepository);
            var secondaryRepository = (MockPackageRepository)packageRestoreRepository.SecondaryRepository;
            Assert.Equal(mockRepository2, secondaryRepository);
        }

        [Fact]
        public void CreatePackageManagerForPackageRestoreUsesAggregateRepository()
        {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();

            var mockRepository1 = new MockPackageRepository("Source1");
            var mockRepository2 = new MockPackageRepository("Source2");

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("A", "1.2"));

            var aggregateRepository = new AggregateRepository(new [] { mockRepository1, mockRepository2 });

            //mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2 });
            mockFileSystemProvider.Setup(f => f.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());

            var packageManagerFactory = new Mock<VsPackageManagerFactory>(new Mock<ISolutionManager>().Object, mockRepositoryFactory.Object, mockSourceProvider.Object, mockFileSystemProvider.Object, new Mock<IRepositorySettings>().Object, new Mock<VsPackageInstallerEvents>().Object, mockRepository1, null);
            packageManagerFactory.Setup(f => f.GetConfigSettingsFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());

            // Act
            var packageManager = packageManagerFactory.Object.CreatePackageManagerWithAllPackageSources(aggregateRepository);

            // Assert
            Assert.IsType(typeof(AggregateRepository), packageManager.SourceRepository);
            var sourceAggregateRepository = (AggregateRepository)packageManager.SourceRepository;
            var repositories = sourceAggregateRepository.Repositories.ToList();
            Assert.Equal(2, repositories.Count);
            Assert.Equal(mockRepository1, repositories[0]);
            Assert.Equal(mockRepository2, repositories[1]);
        }

        [Fact]
        public void CreatePackageManagerForPackageRestoreReturnsActivePackageRepository()
        {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();

            var mockRepository1 = new MockPackageRepository("Source1");

            var source1 = new PackageSource("Source1");

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));

            //mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2 });
            mockFileSystemProvider.Setup(f => f.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());

            var packageManagerFactory = new Mock<VsPackageManagerFactory>(new Mock<ISolutionManager>().Object, mockRepositoryFactory.Object, mockSourceProvider.Object, mockFileSystemProvider.Object, new Mock<IRepositorySettings>().Object, new Mock<VsPackageInstallerEvents>().Object, mockRepository1, null);
            packageManagerFactory.Setup(f => f.GetConfigSettingsFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());

            // Act
            var packageManager = packageManagerFactory.Object.CreatePackageManagerWithAllPackageSources(mockRepository1);

            // Assert
            Assert.IsType(typeof(MockPackageRepository), packageManager.SourceRepository);
            var mockPackageRepository = (MockPackageRepository)packageManager.SourceRepository;
            Assert.Equal(mockRepository1, mockPackageRepository);
        }

        [Fact]
        public void PackageRestoreRepositoryFindPackageReturnsPackageFromPrimaryRepository()
        {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();

            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository();
            var mockRepository3 = new MockPackageRepository();

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");
            var source3 = new PackageSource("Source3");

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("B", "1.4"));
            mockRepository3.AddPackage(PackageUtility.CreatePackage("C", "1.8"));

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2, source3 });
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s =>
            {
                switch (s)
                {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    case "Source3": return mockRepository3;
                    default: return null;
                }
            });

            // Act
            var packageRestoreRepository = mockSourceProvider.Object.CreatePriorityPackageRepository(mockRepositoryFactory.Object, mockRepository1);
            var package = packageRestoreRepository.FindPackage("A", new SemanticVersion("1.0"));

            Assert.NotNull(package);
            Assert.Equal("A", package.Id);
        }

        [Fact]
        public void PackageRestoreRepositoryFindPackageReturnsPackageFromNonActiveRepository()
        {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();

            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository();
            var mockRepository3 = new MockPackageRepository();

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");
            var source3 = new PackageSource("Source3");

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("B", "1.3"));
            mockRepository3.AddPackage(PackageUtility.CreatePackage("C", "1.6"));

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2, source3 });
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s =>
            {
                switch (s)
                {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    case "Source3": return mockRepository3;
                    default: return null;
                }
            });

            // Act
            var packageRestoreRepository = mockSourceProvider.Object.CreatePriorityPackageRepository(mockRepositoryFactory.Object, mockRepository1);
            var package = packageRestoreRepository.FindPackage("B", new SemanticVersion("1.3"));

            Assert.NotNull(package);
            Assert.Equal("B", package.Id);
        }

        [Fact]
        public void PackageRestoreRepositoryFindPackageReturnsNull()
        {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();

            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository();
            var mockRepository3 = new MockPackageRepository();

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");
            var source3 = new PackageSource("Source3");

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("B", "2.0"));
            mockRepository3.AddPackage(PackageUtility.CreatePackage("C", "3.0"));

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2, source3 });
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s =>
            {
                switch (s)
                {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    case "Source3": return mockRepository3;
                    default: return null;
                }
            });

            // Act
            var packageRestoreRepository = mockSourceProvider.Object.CreatePriorityPackageRepository(mockRepositoryFactory.Object, mockRepository1);
            var package = packageRestoreRepository.FindPackage("D", new SemanticVersion("1.0"));

            Assert.Null(package);
        }

        [Fact]
        public void PackageRestoreRepositoryExistsReturnsTrueForPackageFromPrimaryRepository()
        {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();

            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository();
            var mockRepository3 = new MockPackageRepository();

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");
            var source3 = new PackageSource("Source3");

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("B", "2.0"));
            mockRepository3.AddPackage(PackageUtility.CreatePackage("C", "3.0"));

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2, source3 });
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s =>
            {
                switch (s)
                {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    case "Source3": return mockRepository3;
                    default: return null;
                }
            });

            // Act
            var packageRestoreRepository = mockSourceProvider.Object.CreatePriorityPackageRepository(mockRepositoryFactory.Object, mockRepository1);
            Assert.True(packageRestoreRepository.Exists("A", new SemanticVersion("1.0")));
        }

        [Fact]
        public void PackageRestoreRepositoryExistsReturnsTrueForPackageFromNonActiveRepository()
        {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();

            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository();
            var mockRepository3 = new MockPackageRepository();

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");
            var source3 = new PackageSource("Source3");

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("B", "2.0"));
            mockRepository3.AddPackage(PackageUtility.CreatePackage("C", "3.0"));

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2, source3 });
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s =>
            {
                switch (s)
                {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    case "Source3": return mockRepository3;
                    default: return null;
                }
            });

            // Act
            var packageRestoreRepository = mockSourceProvider.Object.CreatePriorityPackageRepository(mockRepositoryFactory.Object, mockRepository1);
            Assert.True(packageRestoreRepository.Exists("B", new SemanticVersion("2.0")));
        }

        [Fact]
        public void PackageRestoreRepositoryExistsReturnsFalse()
        {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();

            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository();
            var mockRepository3 = new MockPackageRepository();

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");
            var source3 = new PackageSource("Source3");

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("B", "2.0"));
            mockRepository3.AddPackage(PackageUtility.CreatePackage("C", "3.0"));

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2, source3 });
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s =>
            {
                switch (s)
                {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    case "Source3": return mockRepository3;
                    default: return null;
                }
            });

            // Act
            var packageRestoreRepository = mockSourceProvider.Object.CreatePriorityPackageRepository(mockRepositoryFactory.Object, mockRepository1);
            Assert.False(packageRestoreRepository.Exists("D", new SemanticVersion("1.0")));
        }

        [Fact]
        public void PackageRestoreRepositoryFindPackagesByIdReturnsPackagesFromPrimaryRepository()
        {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();

            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository();
            var mockRepository3 = new MockPackageRepository();

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");
            var source3 = new PackageSource("Source3");

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "2.0"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("B", "3.0"));
            mockRepository3.AddPackage(PackageUtility.CreatePackage("C", "4.0"));

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2, source3 });
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s =>
            {
                switch (s)
                {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    case "Source3": return mockRepository3;
                    default: return null;
                }
            });

            // Act
            var packageRestoreRepository = mockSourceProvider.Object.CreatePriorityPackageRepository(mockRepositoryFactory.Object, mockRepository1);
            var packages = packageRestoreRepository.FindPackagesById("A").ToList();

            Assert.Equal(2, packages.Count);
            Assert.Equal("A", packages[0].Id);
            Assert.Equal(new SemanticVersion("1.0"), packages[0].Version);
            Assert.Equal("A", packages[1].Id);
            Assert.Equal(new SemanticVersion("2.0"), packages[1].Version);
        }

        [Fact]
        public void PackageRestoreRepositoryFindPackagesByIdReturnsPackagesFromNonActiveRepository()
        {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();

            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository();
            var mockRepository3 = new MockPackageRepository();

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");
            var source3 = new PackageSource("Source3");

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.9"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("B", "1.0"));
            mockRepository3.AddPackage(PackageUtility.CreatePackage("B", "2.0"));

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2, source3 });
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s =>
            {
                switch (s)
                {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    case "Source3": return mockRepository3;
                    default: return null;
                }
            });

            // Act
            var packageRestoreRepository = mockSourceProvider.Object.CreatePriorityPackageRepository(mockRepositoryFactory.Object, mockRepository1);
            var packages = packageRestoreRepository.FindPackagesById("B").ToList();

            Assert.Equal(2, packages.Count);
            Assert.Equal("B", packages[0].Id);
            Assert.Equal(new SemanticVersion("1.0"), packages[0].Version);
            Assert.Equal("B", packages[1].Id);
            Assert.Equal(new SemanticVersion("2.0"), packages[1].Version);
        }

        [Fact]
        public void PackageRestoreRepositoryFindPackagesByIdReturnsUniquePackagesFromNonActiveRepository()
        {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();

            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository();
            var mockRepository3 = new MockPackageRepository();

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");
            var source3 = new PackageSource("Source3");

            IPackage packageA = PackageUtility.CreatePackage("A", "1.86");
            IPackage packageB = PackageUtility.CreatePackage("B", "2.5");

            mockRepository1.AddPackage(packageA);
            mockRepository2.AddPackage(packageB);
            mockRepository3.AddPackage(packageB);

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2, source3 });
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s =>
            {
                switch (s)
                {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    case "Source3": return mockRepository3;
                    default: return null;
                }
            });

            // Act
            var packageRestoreRepository = mockSourceProvider.Object.CreatePriorityPackageRepository(mockRepositoryFactory.Object, mockRepository1);
            var packages = packageRestoreRepository.FindPackagesById("B").ToList();

            Assert.Equal(1, packages.Count);
            Assert.Equal("B", packages[0].Id);
            Assert.Equal(new SemanticVersion("2.5"), packages[0].Version);
        }

        [Fact]
        public void PackageRestoreRepositoryFindPackagesByIdReturnsEmptyList()
        {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();

            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository();
            var mockRepository3 = new MockPackageRepository();

            var source1 = new PackageSource("Source1");
            var source2 = new PackageSource("Source2");
            var source3 = new PackageSource("Source3");

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("B", "1.75"));
            mockRepository3.AddPackage(PackageUtility.CreatePackage("C", "1.98"));

            mockSourceProvider.Setup(m => m.LoadPackageSources()).Returns(new[] { source1, source2, source3 });
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns<string>(s =>
            {
                switch (s)
                {
                    case "Source1": return mockRepository1;
                    case "Source2": return mockRepository2;
                    case "Source3": return mockRepository3;
                    default: return null;
                }
            });

            // Act
            var packageRestoreRepository = mockSourceProvider.Object.CreatePriorityPackageRepository(mockRepositoryFactory.Object, mockRepository1);
            var packages = packageRestoreRepository.FindPackagesById("D");

            Assert.True(packages.IsEmpty());
        }

        [Fact]
        public void PackageRestoreRepositorySourceReturnsPrimaryRepository()
        {
            var mockRepository1 = new MockPackageRepository("Source1");
            var mockRepository2 = new MockPackageRepository("Source2");

            var packageRestoreRepository = new PriorityPackageRepository(mockRepository1, mockRepository2);

            Assert.Equal(packageRestoreRepository.Source, mockRepository1.Source);
        }

        [Fact]
        public void PackageRestoreRepositorySourceReturnsPrimaryRepositorySource()
        {
            var mockRepository1 = new MockPackageRepository("Source1");
            var mockRepository2 = new MockPackageRepository("Source2");

            var packageRestoreRepository = new PriorityPackageRepository(mockRepository1, mockRepository2);

            Assert.Equal("Source1", packageRestoreRepository.Source);
        }

        [Fact]
        public void PackageRestoreRepositorySupportsPrereleasePackagesReturnsValueOfPrimaryRepository()
        {
            var mockRepository1 = new Mock<IPackageRepository>();
            var mockRepository2 = new Mock<IPackageRepository>();

            mockRepository1.Setup(p => p.SupportsPrereleasePackages).Returns(false);
            mockRepository2.Setup(p => p.SupportsPrereleasePackages).Returns(true);

            var packageRestoreRepository = new PriorityPackageRepository(mockRepository1.Object, mockRepository2.Object);

            Assert.Equal(false, packageRestoreRepository.SupportsPrereleasePackages);
        }

        [Fact]
        public void PackageRestoreRepositoryGetPackagesReturnsPrimaryRepositoryPackages()
        {
            var mockRepository1 = new MockPackageRepository();
            var mockRepository2 = new MockPackageRepository();

            mockRepository1.AddPackage(PackageUtility.CreatePackage("A"));
            mockRepository1.AddPackage(PackageUtility.CreatePackage("B"));
            mockRepository2.AddPackage(PackageUtility.CreatePackage("C"));

            var packageRestoreRepository = new PriorityPackageRepository(mockRepository1, mockRepository2);

            var packages = packageRestoreRepository.GetPackages().ToList();

            Assert.Equal(2, packages.Count);
            Assert.Equal("A", packages[0].Id);
            Assert.Equal("B", packages[1].Id);
        }
    }
}
