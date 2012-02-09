using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NuGet.Test;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    using PackageUtility = NuGet.Test.PackageUtility;

    public class VsPackageSourceRepositoryTest
    {
        [Fact]
        public void CtorNullSourceProviderOrRepositoryFactoryThrows()
        {
            // Assert
            ExceptionAssert.ThrowsArgNull(() => new VsPackageSourceRepository(new Mock<IPackageRepositoryFactory>().Object, null), "packageSourceProvider");
            ExceptionAssert.ThrowsArgNull(() => new VsPackageSourceRepository(null, new Mock<IVsPackageSourceProvider>().Object), "repositoryFactory");
        }

        [Fact]
        public void NullActivePackageSourceThrowsForAddPackageAndRemovePackage()
        {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();
            mockSourceProvider.Setup(m => m.ActivePackageSource).Returns((PackageSource)null);
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns(new MockPackageRepository());
            var repository = new VsPackageSourceRepository(mockRepositoryFactory.Object, mockSourceProvider.Object);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => repository.AddPackage(null), "Unable to retrieve package list because no source was specified.");
            ExceptionAssert.Throws<InvalidOperationException>(() => repository.RemovePackage(null), "Unable to retrieve package list because no source was specified.");
        }

        [Fact]
        public void NullActivePackageSourceDoesNotThrowForOtherMethods()
        {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();
            mockSourceProvider.Setup(m => m.ActivePackageSource).Returns((PackageSource)null);
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns(new MockPackageRepository());
            var repository = new VsPackageSourceRepository(mockRepositoryFactory.Object, mockSourceProvider.Object);

            // Act & Assert
            Assert.NotNull(repository.Clone());
            Assert.Empty(repository.GetPackages());
            Assert.False(repository.SupportsPrereleasePackages);
            Assert.Empty(repository.FindPackagesById("A"));
            Assert.Null(repository.FindPackage("A", new SemanticVersion("1.0")));
            Assert.Empty(repository.Search("web", new string[] { "net40" }, true));
        }

        [Fact]
        public void GetPackagesReturnsPackagesFromActiveSource()
        {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();
            var mockRepository = new MockPackageRepository();
            var source = new PackageSource("bar", "foo");
            mockRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockSourceProvider.Setup(m => m.ActivePackageSource).Returns(source);
            mockRepositoryFactory.Setup(m => m.CreateRepository(source.Source)).Returns(mockRepository);
            var repository = new VsPackageSourceRepository(mockRepositoryFactory.Object, mockSourceProvider.Object);

            // Act
            List<IPackage> packages = repository.GetPackages().ToList();

            // Assert
            Assert.Equal(1, packages.Count);
            Assert.Equal("A", packages[0].Id);
            Assert.Equal(new SemanticVersion("1.0"), packages[0].Version);
        }

        [Fact]
        public void FindPackageCallsFindPackageFromActiveSource()
        {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IVsPackageSourceProvider>();
            
            var mockRepository = new Mock<IPackageRepository>(MockBehavior.Strict);
            var source = new PackageSource("bar", "foo");
            mockRepository.As<IPackageLookup>()
                          .Setup(p => p.FindPackage("A", new SemanticVersion("1.0")))
                          .Returns(PackageUtility.CreatePackage("A", "1.0"))
                          .Verifiable();

            mockSourceProvider.Setup(m => m.ActivePackageSource).Returns(source);
            mockRepositoryFactory.Setup(m => m.CreateRepository(source.Source)).Returns(mockRepository.Object);
            var repository = new VsPackageSourceRepository(mockRepositoryFactory.Object, mockSourceProvider.Object);

            // Act
            IPackage package = repository.FindPackage("A", new SemanticVersion("1.0"));

            // Assert
            mockRepository.VerifyAll();
            Assert.Equal("A", package.Id);
            Assert.Equal(new SemanticVersion("1.0"), package.Version);
        }
    }
}
