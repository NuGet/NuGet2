namespace NuPack.Test {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NuPack.Test.Mocks;
    using Moq;

    [TestClass]
    public class DependencyManagerTest {
        [TestMethod]
        public void ReverseDependencyWalkerUsersVersionAndIdToDetermineVisited() {
            // Arrange
            // A 1.0 -> B 1.0
            IPackage packageA1 = PackageUtility.CreatePackage("A",
                                                            "1.0",
                                                             dependencies: new List<PackageDependency> {
                                                                 PackageDependency.CreateDependency("B", version:new Version("1.0"))
                                                             });
            // A 2.0 -> B 2.0
            IPackage packageA2 = PackageUtility.CreatePackage("A",
                                                            "2.0",
                                                             dependencies: new List<PackageDependency> {
                                                                 PackageDependency.CreateDependency("B", version:new Version("2.0"))
                                                             });

            IPackage packageB1 = PackageUtility.CreatePackage("B", "1.0");
            IPackage packageB2 = PackageUtility.CreatePackage("B", "2.0");

            var mockRepository = new MockPackageRepository();
            mockRepository.AddPackage(packageA1);
            mockRepository.AddPackage(packageA2);
            mockRepository.AddPackage(packageB1);
            mockRepository.AddPackage(packageB2);

            // Act 
            IDependentsResolver lookup = new ReverseDependencyWalker(mockRepository);

            // Assert
            Assert.AreEqual(0, lookup.GetDependents(packageA1).Count());
            Assert.AreEqual(0, lookup.GetDependents(packageA2).Count());
            Assert.AreEqual(1, lookup.GetDependents(packageB1).Count());
            Assert.AreEqual(1, lookup.GetDependents(packageB2).Count());
        }

        [TestMethod]
        public void ResolveDependenciesForInstallPackageWithUnknownDependencyThrows() {
            // Arrange            
            IPackage package = PackageUtility.CreatePackage("A",
                                                            "1.0",
                                                             dependencies: new List<PackageDependency> {
                                                                 PackageDependency.CreateDependency("B")
                                                             });

            IPackageOperationResolver resolver = new InstallWalker(new MockPackageRepository(),
                                                             new MockPackageRepository(),
                                                             NullLogger.Instance,
                                                             ignoreDependencies: false);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveOperations(package), "Unable to resolve dependency 'B'");
        }

        [TestMethod]
        public void ResolveDependenciesForInstallCircularReferenceThrows() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B")
                                                                });


            IPackage packageB = PackageUtility.CreatePackage("B", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("A")
                                                                });

            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);

            IPackageOperationResolver resolver = new InstallWalker(localRepository,
                                                             sourceRepository,
                                                             NullLogger.Instance,
                                                             ignoreDependencies: false);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveOperations(packageA), "Circular dependency detected 'A 1.0 => B 1.0 => A 1.0'");
        }

        [TestMethod]
        public void ResolveDependenciesForInstallDiamondDependencyGraph() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            // A -> [B, C]
            // B -> [D]
            // C -> [D]
            //    A
            //   / \
            //  B   C
            //   \ /
            //    D 

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B"),
                                                                    PackageDependency.CreateDependency("C")
                                                                });


            IPackage packageB = PackageUtility.CreatePackage("B", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("D")
                                                                });

            IPackage packageC = PackageUtility.CreatePackage("C", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("D")
                                                                });

            IPackage packageD = PackageUtility.CreatePackage("D", "1.0");

            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageC);
            sourceRepository.AddPackage(packageD);

            IPackageOperationResolver resolver = new InstallWalker(localRepository,
                                                             sourceRepository,
                                                             NullLogger.Instance,
                                                             ignoreDependencies: false);

            // Act
            var packages = resolver.ResolveOperations(packageA).ToList();

            // Assert
            var dict = packages.ToDictionary(p => p.Package.Id);
            Assert.AreEqual(4, packages.Count);
            Assert.IsNotNull(dict["A"]);
            Assert.IsNotNull(dict["B"]);
            Assert.IsNotNull(dict["C"]);
            Assert.IsNotNull(dict["D"]);
        }

        [TestMethod]
        public void ResolveDependenciesForUninstallDiamondDependencyGraph() {
            // Arrange
            var localRepository = new MockPackageRepository();
            // A -> [B, C]
            // B -> [D]
            // C -> [D]
            //    A
            //   / \
            //  B   C
            //   \ /
            //    D 

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B"),
                                                                    PackageDependency.CreateDependency("C")
                                                                });


            IPackage packageB = PackageUtility.CreatePackage("B", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("D")
                                                                });

            IPackage packageC = PackageUtility.CreatePackage("C", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("D")
                                                                });

            IPackage packageD = PackageUtility.CreatePackage("D", "1.0");

            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);
            localRepository.AddPackage(packageC);
            localRepository.AddPackage(packageD);

            IPackageOperationResolver resolver = new UninstallWalker(localRepository,
                                                               new ReverseDependencyWalker(localRepository),
                                                               NullLogger.Instance,
                                                               removeDependencies: true,
                                                               forceRemove: false);



            // Act
            var packages = resolver.ResolveOperations(packageA)
                                   .ToDictionary(p => p.Package.Id);

            // Assert
            Assert.AreEqual(4, packages.Count);
            Assert.IsNotNull(packages["A"]);
            Assert.IsNotNull(packages["B"]);
            Assert.IsNotNull(packages["C"]);
            Assert.IsNotNull(packages["D"]);
        }


        [TestMethod]
        public void ResolveDependencyForInstallCircularReferenceWithDifferentVersionOfPackageReferenceThrows() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();

            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B")
                                                                });

            IPackage packageA15 = PackageUtility.CreatePackage("A", "1.5",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B")
                                                                });


            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("A", version: Version.Parse("1.5"))
                                                                });

            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA15);
            sourceRepository.AddPackage(packageB10);

            IPackageOperationResolver resolver = new InstallWalker(localRepository,
                                                             sourceRepository,
                                                             NullLogger.Instance,
                                                             ignoreDependencies: false);


            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveOperations(packageA10), "Circular dependency detected 'A 1.0 => B 1.0 => A 1.5'");
        }

        [TestMethod]
        public void ResolveDependencyForInstallPackageWithDependencyThatDoesntMeetMinimumVersionThrows() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B", minVersion: Version.Parse( "1.5"))
                                                                });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.4");
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);

            IPackageOperationResolver resolver = new InstallWalker(localRepository,
                                                             sourceRepository,
                                                             NullLogger.Instance,
                                                             ignoreDependencies: false);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveOperations(packageA), "Unable to resolve dependency 'B (>= 1.5)'");
        }

        [TestMethod]
        public void ResolveDependencyForInstallPackageWithDependencyThatDoesntMeetExactVersionThrows() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B", version: Version.Parse( "1.5"))
                                                                });

            sourceRepository.AddPackage(packageA);

            IPackage packageB = PackageUtility.CreatePackage("B", "1.4");
            sourceRepository.AddPackage(packageB);

            IPackageOperationResolver resolver = new InstallWalker(localRepository,
                                                             sourceRepository,
                                                             NullLogger.Instance,
                                                             ignoreDependencies: false);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveOperations(packageA), "Unable to resolve dependency 'B (= 1.5)'");
        }

        [TestMethod]
        public void ResolveDependenciesForInstallPackageWithDependencyReturnsPackageAndDependency() {
            // Arrange            
            var localRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B")
                                                                });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0");

            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);

            IPackageOperationResolver resolver = new UninstallWalker(localRepository,
                                                               new ReverseDependencyWalker(localRepository),
                                                               NullLogger.Instance,
                                                               removeDependencies: true,
                                                               forceRemove: false);

            // Act
            var packages = resolver.ResolveOperations(packageA)
                                            .ToDictionary(p => p.Package.Id);

            // Assert
            Assert.AreEqual(2, packages.Count);
            Assert.IsNotNull(packages["A"]);
            Assert.IsNotNull(packages["B"]);
        }

        [TestMethod]
        public void ResolveDependenciesForUninstallPackageWithMissingDependencyAndRemoveDependenciesThrows() {
            // Arrange
            var localRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B")
                                                                });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0");

            localRepository.AddPackage(packageA);

            IPackageOperationResolver resolver = new UninstallWalker(localRepository,
                                                               new ReverseDependencyWalker(localRepository),
                                                               NullLogger.Instance,
                                                               removeDependencies: true,
                                                               forceRemove: false);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveOperations(packageA), "Unable to locate dependency 'B'. It may have been uninstalled");
        }

        [TestMethod]
        public void ResolveDependenciesForUninstallPackageWithDependentThrows() {
            // Arrange
            var localRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B")
                                                                });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0");

            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);

            IPackageOperationResolver resolver = new UninstallWalker(localRepository,
                                                               new ReverseDependencyWalker(localRepository),
                                                               NullLogger.Instance,
                                                               removeDependencies: false,
                                                               forceRemove: false);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveOperations(packageB), "Unable to uninstall 'B 1.0' because 'A 1.0' depends on it");
        }

        [TestMethod]
        public void ResolveDependenciesForUninstallPackageWithDependentAndRemoveDependenciesThrows() {
            // Arrange
            var localRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B")
                                                                });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0");

            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);

            IPackageOperationResolver resolver = new UninstallWalker(localRepository,
                                                               new ReverseDependencyWalker(localRepository),
                                                               NullLogger.Instance,
                                                               removeDependencies: true,
                                                               forceRemove: false);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveOperations(packageB), "Unable to uninstall 'B 1.0' because 'A 1.0' depends on it");
        }

        [TestMethod]
        public void ResolveDependenciesForUninstallPackageWithDependentAndForceReturnsPackage() {
            // Arrange
            var localRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B")
                                                                });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0");

            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);

            IPackageOperationResolver resolver = new UninstallWalker(localRepository,
                                                               new ReverseDependencyWalker(localRepository),
                                                               NullLogger.Instance,
                                                               removeDependencies: false,
                                                               forceRemove: true);

            // Act
            var packages = resolver.ResolveOperations(packageB)
                             .ToDictionary(p => p.Package.Id);

            // Assert
            Assert.AreEqual(1, packages.Count);
            Assert.IsNotNull(packages["B"]);
        }

        [TestMethod]
        public void ResolveDependenciesForUninstallPackageWithRemoveDependenciesThrowsIfDependencyInUse() {
            // Arrange
            var localRepository = new MockPackageRepository();

            // A 1.0 -> [B, C] 
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                PackageDependency.CreateDependency("B"),
                                                                PackageDependency.CreateDependency("C")
                                                            });


            IPackage packageB = PackageUtility.CreatePackage("B", "1.0");
            IPackage packageC = PackageUtility.CreatePackage("C", "1.0");

            // D -> [C]
            IPackage packageD = PackageUtility.CreatePackage("D", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                PackageDependency.CreateDependency("C"),
                                                            });

            localRepository.AddPackage(packageD);
            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);
            localRepository.AddPackage(packageC);

            IPackageOperationResolver resolver = new UninstallWalker(localRepository,
                                                               new ReverseDependencyWalker(localRepository),
                                                               NullLogger.Instance,
                                                               removeDependencies: true,
                                                               forceRemove: false);

            // Act
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveOperations(packageA), "Unable to uninstall 'C 1.0' because 'D 1.0' depends on it");
        }

        [TestMethod]
        public void ResolveDependenciesForUninstallPackageWithRemoveDependenciesSetAndForceReturnsAllDependencies() {
            // Arrange
            var localRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B"),
                                                                    PackageDependency.CreateDependency("C")
                                                            });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0");
            IPackage packageC = PackageUtility.CreatePackage("C", "1.0");
            IPackage packageD = PackageUtility.CreatePackage("D", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                PackageDependency.CreateDependency("C"),
                                                            });

            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);
            localRepository.AddPackage(packageC);
            localRepository.AddPackage(packageD);

            IPackageOperationResolver resolver = new UninstallWalker(localRepository,
                                                               new ReverseDependencyWalker(localRepository),
                                                               NullLogger.Instance,
                                                               removeDependencies: true,
                                                               forceRemove: true);

            // Act
            var packages = resolver.ResolveOperations(packageA)
                                   .ToDictionary(p => p.Package.Id);

            // Assert            
            Assert.IsNotNull(packages["A"]);
            Assert.IsNotNull(packages["B"]);
            Assert.IsNotNull(packages["C"]);
        }
    }
}
