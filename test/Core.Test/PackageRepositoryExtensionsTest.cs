using System.Globalization;
using System.Linq;
using System.Threading;
using Moq;
using Xunit;
using NuGet.Test.Mocks;

namespace NuGet.Test
{
    public class PackageRepositoryExtensionsTest
    {
        [Fact]
        public void FindPackagesByIdRecognizeIFindPackagesRepositoryInterface()
        {
            // Arrange
            var repository = new Mock<IFindPackagesRepository>();
            repository.Setup(p => p.FindPackagesById("A")).Returns(new IPackage[0]).Verifiable();

            // Act
            PackageRepositoryExtensions.FindPackagesById(repository.As<IPackageRepository>().Object, "A");

            // Assert
            repository.Verify();
        }

        [Fact]
        public void FindPackagesByIdRecognizeICultureAwareRepositoryInterface()
        {
            var turkeyCulture = new CultureInfo("tr-TR");
            string smallPackageName = "YUI".ToLower(turkeyCulture);
            
            // Arrange
            var packages = new IPackage[] 
            { 
                PackageUtility.CreatePackage("YUI"), 
                PackageUtility.CreatePackage("DUI")
            };

            var repository = new Mock<IPackageRepository>();
            repository.Setup(p => p.GetPackages()).Returns(packages.AsQueryable());

            var cultureRepository = repository.As<ICultureAwareRepository>().Setup(p => p.Culture).Returns(turkeyCulture);

            var savedCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                // simulate running on Turkish locale
                Thread.CurrentThread.CurrentCulture = turkeyCulture;
                
                // Act
                // notice the lowercase Turkish I character in the packageId to search for
                var foundPackages = PackageRepositoryExtensions.FindPackagesById(repository.Object, "yuı").ToList();

                // Assert
                Assert.Equal(1, foundPackages.Count);
                Assert.Equal("YUI", foundPackages[0].Id);
            }
            finally
            {
                // restore culture
                Thread.CurrentThread.CurrentCulture = savedCulture;
            }
        }

        [Fact]
        public void GetUpdatesReturnAllPackageVersionsWhenFlagIsSpecified()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0", new string[] { "hello" }));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "2.0", new string[] { "hello" }));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "3.0", new string[] { "hello" }));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "3.0-alpha", new string[] { "hello" }));

            var packages = new IPackage[] 
            {
                PackageUtility.CreatePackage("A", "1.5")
            };

            // Act
            var foundPackages = PackageRepositoryExtensions.GetUpdates(sourceRepository, packages, includePrerelease: true, includeAllVersions: true).ToList();

            // Assert
            Assert.Equal(3, foundPackages.Count);

            Assert.Equal("A", foundPackages[0].Id);
            Assert.Equal(new SemanticVersion("2.0"), foundPackages[0].Version);

            Assert.Equal("A", foundPackages[1].Id);
            Assert.Equal(new SemanticVersion("3.0"), foundPackages[1].Version);

            Assert.Equal("A", foundPackages[2].Id);
            Assert.Equal(new SemanticVersion("3.0-alpha"), foundPackages[2].Version);
        }
    }
}