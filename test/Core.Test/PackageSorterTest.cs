using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace NuGet.Test {
    [TestClass]
    public class PackageSorterTest {

        [TestMethod]
        public void TestSortByDependencySimple() {

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
            Assert.AreEqual(3, sortedPackages.Count);
            Assert.AreSame(packageC, sortedPackages[0]);
            Assert.AreSame(packageB, sortedPackages[1]);
            Assert.AreSame(packageA, sortedPackages[2]);
        }
        
        [TestMethod]
        public void TestSortByDependencyComplex() {

            //      A    
            //    /   \  
            //   B     C 
            //    \   / 
            //      D    

            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0", dependencies: new PackageDependency[] { new PackageDependency("B") } );
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
            Assert.AreEqual(4, sortedPackages.Count);
            Assert.AreSame(packageD, sortedPackages[0]);
            Assert.IsTrue((sortedPackages[1] == packageB && sortedPackages[2] == packageC) ||
                          (sortedPackages[1] == packageC && sortedPackages[2] == packageB));
            Assert.AreSame(packageA, sortedPackages[3]);
        }
    }
}
