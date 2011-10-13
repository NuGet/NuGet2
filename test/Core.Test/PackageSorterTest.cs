using System.Linq;
using Moq;
using Xunit;

namespace NuGet.Test
{

    public class PackageSorterTest
    {

        [Fact]
        public void TestSortByDependencySimple()
        {

            //  A  --> B --> C

            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0", dependencies: new PackageDependency[] { new PackageDependency("B") });
            var packageB = PackageUtility.CreatePackage("B", "1.0", dependencies: new PackageDependency[] { new PackageDependency("C") });
            var packageC = PackageUtility.CreatePackage("C", "1.0");

            var list = new IPackage[] { packageB, packageC, packageA };
            var mockRepository = new Mock<IPackageRepository>();
            mockRepository.Setup(p => p.GetPackages()).Returns(list.AsQueryable());

            var sorter = new PackageSorter();

            // Act
            var sortedPackages = sorter.GetPackagesByDependencyOrder(mockRepository.Object).ToList();

            // Assert
            Assert.Equal(3, sortedPackages.Count);
            Assert.Same(packageC, sortedPackages[0]);
            Assert.Same(packageB, sortedPackages[1]);
            Assert.Same(packageA, sortedPackages[2]);
        }

        [Fact]
        public void TestSortByDependencyComplex()
        {

            //      A    
            //    /   \  
            //   B     C 
            //    \   / 
            //      D    

            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0", dependencies: new PackageDependency[] { new PackageDependency("B") });
            var packageB = PackageUtility.CreatePackage("B", "1.0", dependencies: new PackageDependency[] { new PackageDependency("D") });
            var packageC = PackageUtility.CreatePackage("C", "1.0", dependencies: new PackageDependency[] { new PackageDependency("D") });
            var packageD = PackageUtility.CreatePackage("D", "1.0");

            var list = new IPackage[] { packageD, packageB, packageC, packageA };
            var mockRepository = new Mock<IPackageRepository>();
            mockRepository.Setup(p => p.GetPackages()).Returns(list.AsQueryable());
            var sorter = new PackageSorter();

            // Act
            var sortedPackages = sorter.GetPackagesByDependencyOrder(mockRepository.Object).ToList();

            // Assert
            Assert.Equal(4, sortedPackages.Count);
            Assert.Same(packageD, sortedPackages[0]);
            Assert.True((sortedPackages[1] == packageB && sortedPackages[2] == packageC) ||
                          (sortedPackages[1] == packageC && sortedPackages[2] == packageB));
            Assert.Same(packageA, sortedPackages[3]);
        }
    }
}
