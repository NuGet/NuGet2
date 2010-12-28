using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test;
using NuGet.Test.Mocks;

namespace NuGet.VisualStudio.Test {

    using PackageUtility = NuGet.Test.PackageUtility;

    [TestClass]
    public class VsPackageSourceRepositoryTest {
        [TestMethod]
        public void CtorNullSourceProviderOrRepositoryFactoryThrows() {
            // Assert
            ExceptionAssert.ThrowsArgNull(() => new VsPackageSourceRepository(new Mock<IPackageRepositoryFactory>().Object, null), "packageSourceProvider");
            ExceptionAssert.ThrowsArgNull(() => new VsPackageSourceRepository(null, new Mock<IPackageSourceProvider>().Object), "repositoryFactory");
        }

        [TestMethod]
        public void NullActivePackageSourceThrows() {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IPackageSourceProvider>();
            mockSourceProvider.Setup(m => m.ActivePackageSource).Returns((PackageSource)null);
            mockRepositoryFactory.Setup(m => m.CreateRepository(It.IsAny<PackageSource>())).Returns(new MockPackageRepository());
            var repository = new VsPackageSourceRepository(mockRepositoryFactory.Object, mockSourceProvider.Object);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => repository.GetPackages(), "Unable to retrieve package list because no source was specified.");
            ExceptionAssert.Throws<InvalidOperationException>(() => repository.AddPackage(null), "Unable to retrieve package list because no source was specified.");
            ExceptionAssert.Throws<InvalidOperationException>(() => repository.RemovePackage(null), "Unable to retrieve package list because no source was specified.");
        }

        [TestMethod]
        public void GetPackagesReturnsPackagesFromActiveSource() {
            // Arrange
            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var mockSourceProvider = new Mock<IPackageSourceProvider>();
            var mockRepository = new MockPackageRepository();
            var source = new PackageSource("bar", "foo");
            mockRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            mockSourceProvider.Setup(m => m.ActivePackageSource).Returns(source);
            mockRepositoryFactory.Setup(m => m.CreateRepository(source)).Returns(mockRepository);
            var repository = new VsPackageSourceRepository(mockRepositoryFactory.Object, mockSourceProvider.Object);

            // Act
            List<IPackage> packages = repository.GetPackages().ToList();

            // Assert
            Assert.AreEqual(1, packages.Count);
            Assert.AreEqual("A", packages[0].Id);
            Assert.AreEqual(new Version("1.0"), packages[0].Version);
        }
    }
}
