namespace NuGet.Test {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NuGet.Test.Mocks;
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
            IDependentsResolver lookup = new DependentsWalker(mockRepository);

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
                                                               new DependentsWalker(localRepository),
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
                                                               new DependentsWalker(localRepository),
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
                                                               new DependentsWalker(localRepository),
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
                                                               new DependentsWalker(localRepository),
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
                                                               new DependentsWalker(localRepository),
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
                                                               new DependentsWalker(localRepository),
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
        public void ResolveDependenciesForUninstallPackageWithRemoveDependenciesExcludesDependencyIfDependencyInUse() {
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
                                                               new DependentsWalker(localRepository),
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
        public void ResolveDependenciesForUninstallPackageWithRemoveDependenciesSetAndForceReturnsAllDependencies() {
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

            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);
            localRepository.AddPackage(packageC);
            localRepository.AddPackage(packageD);

            IPackageOperationResolver resolver = new UninstallWalker(localRepository,
                                                               new DependentsWalker(localRepository),
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

        [TestMethod]
        public void ProjectInstallWalkerIgnoresSolutionLevelPackages() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();

            IPackage projectPackage = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B", version: Version.Parse( "1.5"))
                                                                }, content: new[] { "content" });

            sourceRepository.AddPackage(projectPackage);

            IPackage toolsPackage = PackageUtility.CreatePackage("B", "1.5", tools: new[] { "init.ps1" });
            sourceRepository.AddPackage(toolsPackage);

            IPackageOperationResolver resolver = new ProjectInstallWalker(localRepository,
                                                                          sourceRepository,
                                                                          new DependentsWalker(localRepository),
                                                                          NullLogger.Instance,
                                                                          ignoreDependencies: false);

            // Act
            var packages = resolver.ResolveOperations(projectPackage)
                                   .ToDictionary(p => p.Package.Id);

            // Assert            
            Assert.AreEqual(1, packages.Count);
            Assert.IsNotNull(packages["A"]);
        }

        [TestMethod]
        public void AfterPackageWalkMetaPackageIsClassifiedTheSameAsDependencies() {
            // Arrange
            var mockRepository = new MockPackageRepository();
            var walker = new TestWalker(mockRepository);

            IPackage metaPackage = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B"),
                                                                    PackageDependency.CreateDependency("C")
                                                                });

            IPackage projectPackageA = PackageUtility.CreatePackage("B", "1.0", content: new[] { "contentB" });
            IPackage projectPackageB = PackageUtility.CreatePackage("C", "1.0", content: new[] { "contentC" });

            mockRepository.AddPackage(projectPackageA);
            mockRepository.AddPackage(projectPackageB);

            Assert.AreEqual(PackageTargets.None, walker.GetPackageInfo(metaPackage).Target);

            // Act
            walker.Walk(metaPackage);

            // Assert
            Assert.AreEqual(PackageTargets.Project, walker.GetPackageInfo(metaPackage).Target);
        }

        [TestMethod]
        public void MetaPackageWithMixedTargetsThrows() {
            // Arrange
            var mockRepository = new MockPackageRepository();
            var walker = new TestWalker(mockRepository);

            IPackage metaPackage = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B"),
                                                                    PackageDependency.CreateDependency("C")
                                                                });

            IPackage projectPackageA = PackageUtility.CreatePackage("B", "1.0", content: new[] { "contentB" });
            IPackage solutionPackage = PackageUtility.CreatePackage("C", "1.0", tools: new[] { "tools" });

            mockRepository.AddPackage(projectPackageA);
            mockRepository.AddPackage(solutionPackage);

            // Act && Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => walker.Walk(metaPackage), "Child dependencies of dependency only packages cannot mix external and project packages");
        }

        [TestMethod]
        public void ExternalPackagesThatDepdendOnProjectLevelPackagesThrows() {
            // Arrange
            var mockRepository = new MockPackageRepository();
            var walker = new TestWalker(mockRepository);

            IPackage solutionPackage = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B")
                                                                }, tools: new[] { "install.ps1" });

            IPackage projectPackageA = PackageUtility.CreatePackage("B", "1.0", content: new[] { "contentB" });

            mockRepository.AddPackage(projectPackageA);
            mockRepository.AddPackage(solutionPackage);

            // Act && Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => walker.Walk(solutionPackage), "External packages cannot depend on packages that target projects.");
        }

        private class TestWalker : PackageWalker {
            private readonly IPackageRepository _repository;
            public TestWalker(IPackageRepository repository) {
                _repository = repository;
            }

            protected override IPackage ResolveDependency(PackageDependency dependency) {
                return _repository.FindPackage(dependency);
            }
        }
    }
}
