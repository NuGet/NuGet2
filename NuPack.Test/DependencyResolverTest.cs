using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Test.Mocks;

namespace NuGet.Test {

    [TestClass]
    public class DependencyResolverTest {

        [TestMethod]
        public void PackageWithNoDependencies() {
            
            // Arrange
            var mockRepository = new MockPackageRepository();
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0");
            mockRepository.AddPackage(packageA);

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0");
            mockRepository.AddPackage(packageB);

            var dependencyHelper = new DependencyResolver(mockRepository);

            // Act
            IList<IPackage> dependencies = dependencyHelper.GetDependencies(packageA).ToList();

            // Assert
            Assert.IsTrue(dependencies.Count == 1);
            Assert.IsTrue(dependencies[0] == packageA);
        }

        [TestMethod]
        public void PackageWithNonExistentDependenciesThrows() {

            // Arrange
            var mockRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0");
            mockRepository.AddPackage(packageA);

            IPackage packageC = PackageUtility.CreatePackage("C", "1.0",
                                                        dependencies: new List<PackageDependency> {
                                                            PackageDependency.CreateDependency("B", version: new Version("1.0")),
                                                        });
            mockRepository.AddPackage(packageC);

            var dependencyHelper = new DependencyResolver(mockRepository);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                ()=> dependencyHelper.GetDependencies(packageC),
                "Unable to resolve dependency 'B (= 1.0)'");
        }

        [TestMethod]
        public void PackageWithDependenciesButWrongVersionThrows() {

            // Arrange
            var mockRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0");
            mockRepository.AddPackage(packageA);

            IPackage packageC = PackageUtility.CreatePackage("C", "1.0",
                                                        dependencies: new List<PackageDependency> {
                                                            PackageDependency.CreateDependency("A", version: new Version("2.0")),
                                                        });
            mockRepository.AddPackage(packageC);

            var dependencyHelper = new DependencyResolver(mockRepository);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => dependencyHelper.GetDependencies(packageC),
                "Unable to resolve dependency 'A (= 2.0)'");
        }

        [TestMethod]
        public void PackageWithSimpleDependencies() {

            // C -> {A, B}
            // D -> {}

            // Arrange
            var mockRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0");
            mockRepository.AddPackage(packageA);

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0");
            mockRepository.AddPackage(packageB);

            IPackage packageD = PackageUtility.CreatePackage("D", "6.0");
            mockRepository.AddPackage(packageD);

            IPackage packageC = PackageUtility.CreatePackage("C", "1.0",
                                                        dependencies: new List<PackageDependency> {
                                                            PackageDependency.CreateDependency("B", version: new Version("1.0")),
                                                            PackageDependency.CreateDependency("A", version: new Version("1.0")),
                                                        });
            mockRepository.AddPackage(packageC);

            var dependencyHelper = new DependencyResolver(mockRepository);

            // Act
            IList<IPackage> dependencies = dependencyHelper.GetDependencies(packageC).ToList();

            // Assert
            Assert.IsTrue(dependencies.Count == 3);
            Assert.IsTrue(dependencies.Contains(packageA));
            Assert.IsTrue(dependencies.Contains(packageB));
            Assert.IsTrue(dependencies.Contains(packageC));
        }

        [TestMethod]
        public void PackageWithComplexDependencies() {

            // C -> {A, B}
            // D -> {A}
            // B -> {D,E}
            // F -> {C}
            
            // in the end C -> {A, B, D, E}

            // Arrange
            var mockRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0");
            mockRepository.AddPackage(packageA);

            IPackage packageB = PackageUtility.CreatePackage("B", "2.0",
                                                        dependencies: new List<PackageDependency> {
                                                            PackageDependency.CreateDependency("D", version: new Version("6.0")),
                                                            PackageDependency.CreateDependency("E", version: new Version("4.0"))
                                                        });
            mockRepository.AddPackage(packageB);

            IPackage packageD = PackageUtility.CreatePackage("D", "6.0",
                                                        dependencies: new List<PackageDependency> {
                                                            PackageDependency.CreateDependency("A", version: new Version("1.0"))
                                                        });
            mockRepository.AddPackage(packageD);

            IPackage packageE = PackageUtility.CreatePackage("E", "4.0");
            mockRepository.AddPackage(packageE);

            IPackage packageC = PackageUtility.CreatePackage("C", "3.0",
                                                        dependencies: new List<PackageDependency> {
                                                            PackageDependency.CreateDependency("A", version: new Version("1.0")),
                                                            PackageDependency.CreateDependency("B", version: new Version("2.0"))
                                                        });
            mockRepository.AddPackage(packageC);

            IPackage packageF = PackageUtility.CreatePackage("F", "0.1",
                                                        dependencies: new List<PackageDependency> {
                                                            PackageDependency.CreateDependency("C", version: new Version("2.0"))
                                                        });
            mockRepository.AddPackage(packageF);

            var dependencyHelper = new DependencyResolver(mockRepository);

            // Act
            IList<IPackage> dependencies = dependencyHelper.GetDependencies(packageC).ToList();

            // Assert
            Assert.IsTrue(dependencies.Count == 5);
            Assert.IsTrue(dependencies.Contains(packageA));
            Assert.IsTrue(dependencies.Contains(packageB));
            Assert.IsTrue(dependencies.Contains(packageC));
            Assert.IsTrue(dependencies.Contains(packageD));
            Assert.IsTrue(dependencies.Contains(packageE));
        }
    }
}
