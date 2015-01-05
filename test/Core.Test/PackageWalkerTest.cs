using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{

    public class PackageWalkerTest
    {
        [Fact]
        public void ReverseDependencyWalkerUsersVersionAndIdToDetermineVisited()
        {
            // Arrange
            // A 1.0 -> B 1.0
            IPackage packageA1 = PackageUtility.CreatePackage("A",
                                                            "1.0",
                                                             dependencies: new List<PackageDependency> {
                                                                 PackageDependency.CreateDependency("B", "[1.0]")
                                                             });
            // A 2.0 -> B 2.0
            IPackage packageA2 = PackageUtility.CreatePackage("A",
                                                            "2.0",
                                                             dependencies: new List<PackageDependency> {
                                                                 PackageDependency.CreateDependency("B", "[2.0]")
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
            Assert.Equal(0, lookup.GetDependents(packageA1).Count());
            Assert.Equal(0, lookup.GetDependents(packageA2).Count());
            Assert.Equal(1, lookup.GetDependents(packageB1).Count());
            Assert.Equal(1, lookup.GetDependents(packageB2).Count());
        }

        [Fact]
        public void ResolveDependenciesForInstallPackageWithUnknownDependencyThrows()
        {
            // Arrange            
            IPackage package = PackageUtility.CreatePackage("A",
                                                            "1.0",
                                                             dependencies: new List<PackageDependency> {
                                                                 new PackageDependency("B")
                                                             });

            IPackageOperationResolver resolver = new InstallWalker(new MockPackageRepository(),
                                                             new DependencyResolverFromRepo(new MockPackageRepository()),
                                                             NullLogger.Instance,
                                                             ignoreDependencies: false,
                                                             allowPrereleaseVersions: false,
                                                             dependencyVersion: DependencyVersion.Lowest);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveOperations(package), "Unable to resolve dependency 'B'.");
        }

        [Fact]
        public void ResolveDependenciesForInstallPackageResolvesDependencyUsingDependencyProvider()
        {
            // Arrange            
            IPackage packageA = PackageUtility.CreatePackage(
                "A",
                "1.0",
                dependencies: new List<PackageDependency> {
                    new PackageDependency("B")
                });
            IPackage packageB = PackageUtility.CreatePackage("B");
            var repository = new Mock<PackageRepositoryBase>();
            repository.Setup(c => c.GetPackages()).Returns(new[] { packageA }.AsQueryable());
            var dependencyProvider = repository.As<IDependencyResolver>();
            dependencyProvider.Setup(c => c.ResolveDependency(It.Is<PackageDependency>(p => p.Id == "B"), It.IsAny<IPackageConstraintProvider>(), false, true, DependencyVersion.Lowest))
                              .Returns(packageB).Verifiable();
            var localRepository = new MockPackageRepository();

            IPackageOperationResolver resolver = new InstallWalker(
                localRepository,
                new DependencyResolverFromRepo(repository.Object),
                NullLogger.Instance,
                ignoreDependencies: false,
                allowPrereleaseVersions: false,
                dependencyVersion: DependencyVersion.Lowest);


            // Act
            var operations = resolver.ResolveOperations(packageA).ToList();

            // Assert
            Assert.Equal(2, operations.Count);
            Assert.Equal(PackageAction.Install, operations.First().Action);
            Assert.Equal(packageB, operations.First().Package);
            Assert.Equal(PackageAction.Install, operations.Last().Action);
            Assert.Equal(packageA, operations.Last().Package);

            dependencyProvider.Verify();
        }

        [Fact]
        public void ResolveDependenciesForInstallPackageResolvesDependencyWithConstraintsUsingDependencyResolver()
        {
            // Arrange            
            var packageDependency = new PackageDependency("B", new VersionSpec { MinVersion = new SemanticVersion("1.1") });
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> { packageDependency });
            IPackage packageB12 = PackageUtility.CreatePackage("B", "1.2");
            var repository = new Mock<PackageRepositoryBase>(MockBehavior.Strict);
            repository.Setup(c => c.GetPackages()).Returns(new[] { packageA }.AsQueryable());
            var dependencyProvider = repository.As<IDependencyResolver>();
            dependencyProvider.Setup(c => c.ResolveDependency(packageDependency, It.IsAny<IPackageConstraintProvider>(), false, true, DependencyVersion.Lowest))
                              .Returns(packageB12).Verifiable();
            var localRepository = new MockPackageRepository();

            IPackageOperationResolver resolver = new InstallWalker(localRepository,
                                                             new DependencyResolverFromRepo(repository.Object),
                                                             NullLogger.Instance,
                                                             ignoreDependencies: false,
                                                             allowPrereleaseVersions: false,
                                                             dependencyVersion: DependencyVersion.Lowest);


            // Act
            var operations = resolver.ResolveOperations(packageA).ToList();

            // Assert
            Assert.Equal(2, operations.Count);
            Assert.Equal(PackageAction.Install, operations.First().Action);
            Assert.Equal(packageB12, operations.First().Package);
            Assert.Equal(PackageAction.Install, operations.Last().Action);
            Assert.Equal(packageA, operations.Last().Package);

            dependencyProvider.Verify();
        }

        [Fact]
        public void ResolveDependenciesForInstallCircularReferenceThrows()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    new PackageDependency("B")
                                                                });


            IPackage packageB = PackageUtility.CreatePackage("B", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    new PackageDependency("A")
                                                                });

            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);

            IPackageOperationResolver resolver = new InstallWalker(
                localRepository,
                new DependencyResolverFromRepo(sourceRepository),
                NullLogger.Instance,
                ignoreDependencies: false,
                allowPrereleaseVersions: false,
                dependencyVersion: DependencyVersion.Lowest);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveOperations(packageA), "Circular dependency detected 'A 1.0 => B 1.0 => A 1.0'.");
        }

        [Fact]
        public void ResolveDependenciesForInstallDiamondDependencyGraph()
        {
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
                                                                    new PackageDependency("B"),
                                                                    new PackageDependency("C")
                                                                });


            IPackage packageB = PackageUtility.CreatePackage("B", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    new PackageDependency("D")
                                                                });

            IPackage packageC = PackageUtility.CreatePackage("C", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    new PackageDependency("D")
                                                                });

            IPackage packageD = PackageUtility.CreatePackage("D", "1.0");

            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageC);
            sourceRepository.AddPackage(packageD);

            IPackageOperationResolver resolver = new InstallWalker(localRepository,
                                                             new DependencyResolverFromRepo(sourceRepository),
                                                             NullLogger.Instance,
                                                             ignoreDependencies: false,
                                                             allowPrereleaseVersions: false,
                                                             dependencyVersion: DependencyVersion.Lowest);

            // Act
            var packages = resolver.ResolveOperations(packageA).ToList();

            // Assert
            var dict = packages.ToDictionary(p => p.Package.Id);
            Assert.Equal(4, packages.Count);
            Assert.NotNull(dict["A"]);
            Assert.NotNull(dict["B"]);
            Assert.NotNull(dict["C"]);
            Assert.NotNull(dict["D"]);
        }

        [Fact]
        public void ResolveDependenciesForInstallDiamondDependencyGraphWithDifferntVersionOfSamePackage()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            // A -> [B, C]
            // B -> [D >= 1, E >= 2]
            // C -> [D >= 2, E >= 1]
            //     A
            //   /   \
            //  B     C
            //  | \   | \
            //  D1 E2 D2 E1

            IPackage packageA = PackageUtility.CreateProjectLevelPackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    new PackageDependency("B"),
                                                                    new PackageDependency("C")
                                                                });


            IPackage packageB = PackageUtility.CreateProjectLevelPackage("B", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("D", "1.0"),
                                                                    PackageDependency.CreateDependency("E", "2.0")
                                                                });

            IPackage packageC = PackageUtility.CreateProjectLevelPackage("C", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("D", "2.0"),
                                                                    PackageDependency.CreateDependency("E", "1.0")
                                                                });

            IPackage packageD10 = PackageUtility.CreateProjectLevelPackage("D", "1.0");
            IPackage packageD20 = PackageUtility.CreateProjectLevelPackage("D", "2.0");
            IPackage packageE10 = PackageUtility.CreateProjectLevelPackage("E", "1.0");
            IPackage packageE20 = PackageUtility.CreateProjectLevelPackage("E", "2.0");

            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageC);
            sourceRepository.AddPackage(packageD20);
            sourceRepository.AddPackage(packageD10);
            sourceRepository.AddPackage(packageE20);
            sourceRepository.AddPackage(packageE10);


            IPackageOperationResolver resolver = new InstallWalker(localRepository,
                                                                   new DependencyResolverFromRepo(sourceRepository),
                                                                   NullLogger.Instance,
                                                                   ignoreDependencies: false,
                                                                   allowPrereleaseVersions: false,
                                                                   dependencyVersion: DependencyVersion.Lowest);

            // Act
            var operations = resolver.ResolveOperations(packageA).ToList();
            var projectOperations = resolver.ResolveOperations(packageA).ToList();

            // Assert
            Assert.Equal(5, operations.Count);
            Assert.Equal("E", operations[0].Package.Id);
            Assert.Equal(new SemanticVersion("2.0"), operations[0].Package.Version);
            Assert.Equal("B", operations[1].Package.Id);
            Assert.Equal("D", operations[2].Package.Id);
            Assert.Equal(new SemanticVersion("2.0"), operations[2].Package.Version);
            Assert.Equal("C", operations[3].Package.Id);
            Assert.Equal("A", operations[4].Package.Id);

            Assert.Equal(5, projectOperations.Count);
            Assert.Equal("E", projectOperations[0].Package.Id);
            Assert.Equal(new SemanticVersion("2.0"), projectOperations[0].Package.Version);
            Assert.Equal("B", projectOperations[1].Package.Id);
            Assert.Equal("D", projectOperations[2].Package.Id);
            Assert.Equal(new SemanticVersion("2.0"), projectOperations[2].Package.Version);
            Assert.Equal("C", projectOperations[3].Package.Id);
            Assert.Equal("A", projectOperations[4].Package.Id);
        }

        [Fact]
        public void UninstallWalkerIgnoresMissingDependencies()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            // A -> [B, C]
            // B -> [D]
            // C -> [D]

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    new PackageDependency("B"),
                                                                    new PackageDependency("C")
                                                                });

            IPackage packageC = PackageUtility.CreatePackage("C", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    new PackageDependency("D")
                                                                });

            IPackage packageD = PackageUtility.CreatePackage("D", "1.0");

            localRepository.AddPackage(packageA);
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
            Assert.Equal(3, packages.Count);
            Assert.NotNull(packages["A"]);
            Assert.NotNull(packages["C"]);
            Assert.NotNull(packages["D"]);
        }

        [Fact]
        public void ResolveDependenciesForUninstallDiamondDependencyGraph()
        {
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
                                                                    new PackageDependency("B"),
                                                                    new PackageDependency("C")
                                                                });


            IPackage packageB = PackageUtility.CreatePackage("B", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    new PackageDependency("D")
                                                                });

            IPackage packageC = PackageUtility.CreatePackage("C", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    new PackageDependency("D")
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
            Assert.Equal(4, packages.Count);
            Assert.NotNull(packages["A"]);
            Assert.NotNull(packages["B"]);
            Assert.NotNull(packages["C"]);
            Assert.NotNull(packages["D"]);
        }

        [Fact]
        public void ResolveDependencyForInstallCircularReferenceWithDifferentVersionOfPackageReferenceThrows()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();

            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    new PackageDependency("B")
                                                                });

            IPackage packageA15 = PackageUtility.CreatePackage("A", "1.5",
                                                            dependencies: new List<PackageDependency> {
                                                                    new PackageDependency("B")
                                                                });


            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("A", "[1.5]")
                                                                });

            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA15);
            sourceRepository.AddPackage(packageB10);

            IPackageOperationResolver resolver = new InstallWalker(localRepository,
                                                             new DependencyResolverFromRepo(sourceRepository),
                                                             NullLogger.Instance,
                                                             ignoreDependencies: false,
                                                             allowPrereleaseVersions: false,
                                                             dependencyVersion: DependencyVersion.Lowest);


            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveOperations(packageA10), "Circular dependency detected 'A 1.0 => B 1.0 => A 1.5'.");
        }

        [Fact]
        public void ResolvingDependencyForUpdateWithConflictingDependents()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();

            // A 1.0 -> B [1.0]
            IPackage A10 = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B", "[1.0]")
                                                                }, content: new[] { "a1" });

            // A 2.0 -> B (any version)
            IPackage A20 = PackageUtility.CreatePackage("A", "2.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    new PackageDependency("B")
                                                                }, content: new[] { "a2" });

            IPackage B10 = PackageUtility.CreatePackage("B", "1.0", content: new[] { "b1" });
            IPackage B101 = PackageUtility.CreatePackage("B", "1.0.1", content: new[] { "b101" });
            IPackage B20 = PackageUtility.CreatePackage("B", "2.0", content: new[] { "a2" });
            localRepository.Add(A10);
            localRepository.Add(B10);
            sourceRepository.AddPackage(A10);
            sourceRepository.AddPackage(A20);
            sourceRepository.AddPackage(B10);
            sourceRepository.AddPackage(B101);
            sourceRepository.AddPackage(B20);

            IPackageOperationResolver resolver = new UpdateWalker(localRepository,
                                                                  new DependencyResolverFromRepo(sourceRepository),
                                                                  new DependentsWalker(localRepository),
                                                                  NullConstraintProvider.Instance,
                                                                  NullLogger.Instance,
                                                                  updateDependencies: true,
                                                                  allowPrereleaseVersions: false) { AcceptedTargets = PackageTargets.Project };

            // Act
            var packages = resolver.ResolveOperations(B101).ToList();

            // Assert
            Assert.Equal(4, packages.Count);
            AssertOperation("A", "1.0", PackageAction.Uninstall, packages[0]);
            AssertOperation("B", "1.0", PackageAction.Uninstall, packages[1]);
            AssertOperation("A", "2.0", PackageAction.Install, packages[2]);
            AssertOperation("B", "1.0.1", PackageAction.Install, packages[3]);
        }

        [Fact]
        public void ResolvingDependencyForUpdateThatHasAnUnsatisfiedConstraint()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var constraintProvider = new Mock<IPackageConstraintProvider>();
            constraintProvider.Setup(m => m.GetConstraint("B")).Returns(VersionUtility.ParseVersionSpec("[1.4]"));
            constraintProvider.Setup(m => m.Source).Returns("foo");

            IPackage A10 = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B", "1.5")
                                                                });
            IPackage A20 = PackageUtility.CreatePackage("A", "2.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B", "2.0")
                                                                });

            IPackage B15 = PackageUtility.CreatePackage("B", "1.5");
            IPackage B20 = PackageUtility.CreatePackage("B", "2.0");
            localRepository.Add(A10);
            localRepository.Add(B15);
            sourceRepository.AddPackage(A10);
            sourceRepository.AddPackage(A20);
            sourceRepository.AddPackage(B15);
            sourceRepository.AddPackage(B20);

            IPackageOperationResolver resolver = new InstallWalker(localRepository,
                                                                   new DependencyResolverFromRepo(sourceRepository),
                                                                   constraintProvider.Object,
                                                                   null,
                                                                   NullLogger.Instance,
                                                                   ignoreDependencies: false,
                                                                   allowPrereleaseVersions: false,
                                                                   dependencyVersion: DependencyVersion.Lowest);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveOperations(A20), "Unable to resolve dependency 'B (\u2265 2.0)'.'B' has an additional constraint (= 1.4) defined in foo.");
        }

        [Fact]
        public void ResolveDependencyForInstallPackageWithDependencyThatDoesntMeetMinimumVersionThrows()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B", "1.5")
                                                                });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.4");
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);

            IPackageOperationResolver resolver = new InstallWalker(localRepository,
                                                             new DependencyResolverFromRepo(sourceRepository),
                                                             NullLogger.Instance,
                                                             ignoreDependencies: false,
                                                             allowPrereleaseVersions: false,
                                                             dependencyVersion: DependencyVersion.Lowest);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveOperations(packageA), "Unable to resolve dependency 'B (\u2265 1.5)'.");
        }

        [Fact]
        public void ResolveDependencyForInstallPackageWithDependencyThatDoesntMeetExactVersionThrows()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B", "[1.5]")
                                                                });

            sourceRepository.AddPackage(packageA);

            IPackage packageB = PackageUtility.CreatePackage("B", "1.4");
            sourceRepository.AddPackage(packageB);

            IPackageOperationResolver resolver = new InstallWalker(localRepository,
                                                             new DependencyResolverFromRepo(sourceRepository),
                                                             NullLogger.Instance,
                                                             ignoreDependencies: false,
                                                             allowPrereleaseVersions: false,
                                                             dependencyVersion: DependencyVersion.Lowest);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveOperations(packageA), "Unable to resolve dependency 'B (= 1.5)'.");
        }

        [Fact]
        public void ResolveOperationsForInstallSameDependencyAtDifferentLevelsInGraph()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();

            // A1 -> B1, C1
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B", "1.0"),
                                                                    PackageDependency.CreateDependency("C", "1.0")
                                                                });
            // B1
            IPackage packageB = PackageUtility.CreatePackage("B", "1.0");

            // C1 -> B1, D1
            IPackage packageC = PackageUtility.CreatePackage("C", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B", "1.0"),
                                                                    PackageDependency.CreateDependency("D", "1.0")
                                                                });

            // D1 -> B1
            IPackage packageD = PackageUtility.CreatePackage("D", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B", "1.0")
                                                                });


            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageC);
            sourceRepository.AddPackage(packageD);



            IPackageOperationResolver resolver = new InstallWalker(localRepository,
                                                                   new DependencyResolverFromRepo(sourceRepository),
                                                                   NullLogger.Instance,
                                                                   ignoreDependencies: false,
                                                                   allowPrereleaseVersions: false,
                                                                   dependencyVersion: DependencyVersion.Lowest);

            // Act & Assert
            var packages = resolver.ResolveOperations(packageA).ToList();
            Assert.Equal(4, packages.Count);
            Assert.Equal("B", packages[0].Package.Id);
            Assert.Equal("D", packages[1].Package.Id);
            Assert.Equal("C", packages[2].Package.Id);
            Assert.Equal("A", packages[3].Package.Id);
        }

        [Fact]
        public void ResolveDependenciesForInstallSameDependencyAtDifferentLevelsInGraphDuringUpdate()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();

            // A1 -> B1, C1
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            content: new[] { "A1" },
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B", "1.0"),
                                                                    PackageDependency.CreateDependency("C", "1.0")
                                                                });
            // B1
            IPackage packageB = PackageUtility.CreatePackage("B", "1.0", new[] { "B1" });

            // C1 -> B1, D1
            IPackage packageC = PackageUtility.CreatePackage("C", "1.0",
                                                            content: new[] { "C1" },
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B", "1.0"),
                                                                    PackageDependency.CreateDependency("D", "1.0")
                                                                });

            // D1 -> B1
            IPackage packageD = PackageUtility.CreatePackage("D", "1.0",
                                                            content: new[] { "A1" },
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B", "1.0")
                                                                });

            // A2 -> B2, C2
            IPackage packageA2 = PackageUtility.CreatePackage("A", "2.0",
                                                            content: new[] { "A2" },
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B", "2.0"),
                                                                    PackageDependency.CreateDependency("C", "2.0")
                                                                });
            // B2
            IPackage packageB2 = PackageUtility.CreatePackage("B", "2.0", new[] { "B2" });

            // C2 -> B2, D2
            IPackage packageC2 = PackageUtility.CreatePackage("C", "2.0",
                                                            content: new[] { "C2" },
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B", "2.0"),
                                                                    PackageDependency.CreateDependency("D", "2.0")
                                                                });

            // D2 -> B2
            IPackage packageD2 = PackageUtility.CreatePackage("D", "2.0",
                                                            content: new[] { "D2" },
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B", "2.0")
                                                                });


            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageC);
            sourceRepository.AddPackage(packageD);
            sourceRepository.AddPackage(packageA2);
            sourceRepository.AddPackage(packageB2);
            sourceRepository.AddPackage(packageC2);
            sourceRepository.AddPackage(packageD2);

            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);
            localRepository.AddPackage(packageC);
            localRepository.AddPackage(packageD);



            IPackageOperationResolver resolver = new UpdateWalker(
                localRepository,
                new DependencyResolverFromRepo(sourceRepository),
                new DependentsWalker(localRepository),
                NullConstraintProvider.Instance,
                NullLogger.Instance,
                updateDependencies: true,
                allowPrereleaseVersions: false);

            var operations = resolver.ResolveOperations(packageA2).ToList();
            Assert.Equal(8, operations.Count);
            AssertOperation("A", "1.0", PackageAction.Uninstall, operations[0]);
            AssertOperation("C", "1.0", PackageAction.Uninstall, operations[1]);
            AssertOperation("D", "1.0", PackageAction.Uninstall, operations[2]);
            AssertOperation("B", "1.0", PackageAction.Uninstall, operations[3]);
            AssertOperation("B", "2.0", PackageAction.Install, operations[4]);
            AssertOperation("D", "2.0", PackageAction.Install, operations[5]);
            AssertOperation("C", "2.0", PackageAction.Install, operations[6]);
            AssertOperation("A", "2.0", PackageAction.Install, operations[7]);
        }

        [Fact]
        public void ResolveDependenciesForInstallPackageWithDependencyReturnsPackageAndDependency()
        {
            // Arrange            
            var localRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    new PackageDependency("B")
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
            Assert.Equal(2, packages.Count);
            Assert.NotNull(packages["A"]);
            Assert.NotNull(packages["B"]);
        }

        [Fact]
        public void ResolveDependenciesForUninstallPackageWithDependentThrows()
        {
            // Arrange
            var localRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    new PackageDependency("B")
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
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveOperations(packageB), "Unable to uninstall 'B 1.0' because 'A 1.0' depends on it.");
        }

        [Fact]
        public void ResolveDependenciesForUninstallPackageWithDependentAndRemoveDependenciesThrows()
        {
            // Arrange
            var localRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    new PackageDependency("B")
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
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveOperations(packageB), "Unable to uninstall 'B 1.0' because 'A 1.0' depends on it.");
        }

        [Fact]
        public void ResolveDependenciesForUninstallPackageWithDependentAndForceReturnsPackage()
        {
            // Arrange
            var localRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    new PackageDependency("B")
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
            Assert.Equal(1, packages.Count);
            Assert.NotNull(packages["B"]);
        }

        [Fact]
        public void ResolveDependenciesForUninstallPackageWithRemoveDependenciesExcludesDependencyIfDependencyInUse()
        {
            // Arrange
            var localRepository = new MockPackageRepository();

            // A 1.0 -> [B, C] 
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                new PackageDependency("B"),
                                                                new PackageDependency("C")
                                                            });


            IPackage packageB = PackageUtility.CreatePackage("B", "1.0");
            IPackage packageC = PackageUtility.CreatePackage("C", "1.0");

            // D -> [C]
            IPackage packageD = PackageUtility.CreatePackage("D", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                new PackageDependency("C"),
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
            Assert.Equal(2, packages.Count);
            Assert.NotNull(packages["A"]);
            Assert.NotNull(packages["B"]);
        }

        [Fact]
        public void ResolveDependenciesForUninstallPackageWithRemoveDependenciesSetAndForceReturnsAllDependencies()
        {
            // Arrange
            var localRepository = new MockPackageRepository();

            // A 1.0 -> [B, C]
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    new PackageDependency("B"),
                                                                    new PackageDependency("C")
                                                            });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0");
            IPackage packageC = PackageUtility.CreatePackage("C", "1.0");

            // D -> [C]
            IPackage packageD = PackageUtility.CreatePackage("D", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                new PackageDependency("C"),
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
            Assert.NotNull(packages["A"]);
            Assert.NotNull(packages["B"]);
            Assert.NotNull(packages["C"]);
        }

        [Fact]
        public void ProjectInstallWalkerIgnoresSolutionLevelPackages()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();

            IPackage projectPackage = PackageUtility.CreatePackage("A", "1.0",
                dependencies: new List<PackageDependency> {
                    PackageDependency.CreateDependency("B", "[1.5]")
                }, 
                content: new[] { "content" });

            sourceRepository.AddPackage(projectPackage);

            IPackage toolsPackage = PackageUtility.CreatePackage("B", "1.5", 
                content: Enumerable.Empty<string>(),
                tools: new[] { "init.ps1" });
            sourceRepository.AddPackage(toolsPackage);

            IPackageOperationResolver resolver = new UpdateWalker(localRepository,
                                                                   new DependencyResolverFromRepo(sourceRepository),
                                                                   new DependentsWalker(localRepository),
                                                                   NullConstraintProvider.Instance,
                                                                   NullLogger.Instance,
                                                                   updateDependencies: true,
                                                                   allowPrereleaseVersions: false) { AcceptedTargets = PackageTargets.Project };

            // Act
            var packages = resolver.ResolveOperations(projectPackage)
                                   .ToDictionary(p => p.Package.Id);

            // Assert            
            Assert.Equal(1, packages.Count);
            Assert.NotNull(packages["A"]);
        }

        [Fact]
        public void AfterPackageWalkMetaPackageIsClassifiedTheSameAsDependencies()
        {
            // Arrange
            var mockRepository = new MockPackageRepository();
            var walker = new TestWalker(mockRepository);

            IPackage metaPackage = PackageUtility.CreatePackage(
                "A", "1.0",
                content: Enumerable.Empty<string>(),
                dependencies: new List<PackageDependency> {
                    new PackageDependency("B"),
                    new PackageDependency("C")
                },
                createRealStream: false);

            IPackage projectPackageA = PackageUtility.CreatePackage("B", "1.0", content: new[] { "contentB" });
            IPackage projectPackageB = PackageUtility.CreatePackage("C", "1.0", content: new[] { "contentC" });

            mockRepository.AddPackage(projectPackageA);
            mockRepository.AddPackage(projectPackageB);

            Assert.Equal(PackageTargets.None, walker.GetPackageInfo(metaPackage).Target);

            // Act
            walker.Walk(metaPackage);

            // Assert
            Assert.Equal(PackageTargets.Project, walker.GetPackageInfo(metaPackage).Target);
        }

        [Fact]
        public void LocalizedIntelliSenseFileCountsAsProjectTarget()
        {
            // Arrange
            var mockRepository = new MockPackageRepository();
            var walker = new TestWalker(mockRepository);

            IPackage runtimePackage = PackageUtility.CreatePackage("A", "1.0",
                                                            assemblyReferences: new[] { @"lib\A.dll", @"lib\A.xml" });

            IPackage satellitePackage = PackageUtility.CreatePackage("A.fr-fr", "1.0",
                                                            dependencies: new[] { new PackageDependency("A") },
                                                            satelliteAssemblies: new[] { @"lib\fr-fr\A.xml" },
                                                            language: "fr-fr");

            mockRepository.AddPackage(runtimePackage);
            mockRepository.AddPackage(satellitePackage);

            // Act
            walker.Walk(satellitePackage);

            // Assert
            Assert.Equal(PackageTargets.Project, walker.GetPackageInfo(satellitePackage).Target);
        }

        [Fact]
        public void AfterPackageWalkSatellitePackageIsClassifiedTheSameAsDependencies()
        {
            // Arrange
            var mockRepository = new MockPackageRepository();
            var walker = new TestWalker(mockRepository);

            IPackage runtimePackage = PackageUtility.CreatePackage("A", "1.0",
                                                            assemblyReferences: new[] { @"lib\A.dll" });

            IPackage satellitePackage = PackageUtility.CreatePackage("A.fr-fr", "1.0",
                                                            dependencies: new[] { new PackageDependency("A") },
                                                            satelliteAssemblies: new[] { @"lib\fr-fr\A.resources.dll" },
                                                            language: "fr-fr");

            mockRepository.AddPackage(runtimePackage);
            mockRepository.AddPackage(satellitePackage);

            // Act
            walker.Walk(satellitePackage);

            // Assert
            Assert.Equal(PackageTargets.Project, walker.GetPackageInfo(satellitePackage).Target);
        }

        [Fact]
        public void MetaPackageWithMixedTargetsThrows()
        {
            // Arrange
            var mockRepository = new MockPackageRepository();
            var walker = new TestWalker(mockRepository);

            IPackage metaPackage = PackageUtility.CreatePackage("A", "1.0",
                content: Enumerable.Empty<string>(),
                dependencies: new List<PackageDependency> {
                    new PackageDependency("B"),
                    new PackageDependency("C")
                },
                createRealStream: false);

            IPackage projectPackageA = PackageUtility.CreatePackage("B", "1.0", content: new[] { "contentB" });
            IPackage solutionPackage = PackageUtility.CreatePackage("C", "1.0", content: Enumerable.Empty<string>(), tools: new[] { "tools" });

            mockRepository.AddPackage(projectPackageA);
            mockRepository.AddPackage(solutionPackage);

            // Act && Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => walker.Walk(metaPackage), "Child dependencies of dependency only packages cannot mix external and project packages.");
        }

        [Fact]
        public void ExternalPackagesThatDepdendOnProjectLevelPackagesThrows()
        {
            // Arrange
            var mockRepository = new MockPackageRepository();
            var walker = new TestWalker(mockRepository);

            IPackage solutionPackage = PackageUtility.CreatePackage(
                "A", "1.0",
                dependencies: new List<PackageDependency> {
                    new PackageDependency("B")
                }, 
                content: Enumerable.Empty<string>(),
                tools: new[] { "install.ps1" });

            IPackage projectPackageA = PackageUtility.CreatePackage("B", "1.0", content: new[] { "contentB" });

            mockRepository.AddPackage(projectPackageA);
            mockRepository.AddPackage(solutionPackage);

            // Act && Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => walker.Walk(solutionPackage), "External packages cannot depend on packages that target projects.");
        }

        [Fact]
        public void InstallWalkerResolvesLowestMajorAndMinorVersionForDependencies()
        {
            // Arrange

            // A 1.0 -> B 1.0
            // B 1.0 -> C 1.1
            // C 1.1 -> D 1.0

            var A10 = PackageUtility.CreatePackage("A", "1.0", dependencies: new[] { PackageDependency.CreateDependency("B", "1.0") });

            var repository = new MockPackageRepository() {
                PackageUtility.CreatePackage("B", "2.0", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") }),
                PackageUtility.CreatePackage("B", "1.0", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") }),
                PackageUtility.CreatePackage("B", "1.0.1"),
                A10,
                PackageUtility.CreatePackage("D", "2.0"),
                PackageUtility.CreatePackage("C", "1.1.3", dependencies: new[] { PackageDependency.CreateDependency("D", "1.0") }),
                PackageUtility.CreatePackage("C", "1.1.1", dependencies: new[] { PackageDependency.CreateDependency("D", "1.0") }),
                PackageUtility.CreatePackage("C", "1.5.1", dependencies: new[] { PackageDependency.CreateDependency("D", "1.0") }),
                PackageUtility.CreatePackage("B", "1.0.9", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") }),
                PackageUtility.CreatePackage("B", "1.1", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") })
            };


            IPackageOperationResolver resolver = new InstallWalker(
                new MockPackageRepository(),
                new DependencyResolverFromRepo(repository),
                NullLogger.Instance,
                ignoreDependencies: false,
                allowPrereleaseVersions: false,
                dependencyVersion: DependencyVersion.HighestPatch);

            // Act
            var packages = resolver.ResolveOperations(A10).ToList();

            // Assert
            Assert.Equal(4, packages.Count);
            Assert.Equal("D", packages[0].Package.Id);
            Assert.Equal(new SemanticVersion("2.0"), packages[0].Package.Version);
            Assert.Equal("C", packages[1].Package.Id);
            Assert.Equal(new SemanticVersion("1.1.3"), packages[1].Package.Version);
            Assert.Equal("B", packages[2].Package.Id);
            Assert.Equal(new SemanticVersion("1.0.9"), packages[2].Package.Version);
            Assert.Equal("A", packages[3].Package.Id);
            Assert.Equal(new SemanticVersion("1.0"), packages[3].Package.Version);
        }

        // Tests that when DependencyVersion is lowest, the dependency with the lowest major minor and patch version
        // is picked.
        [Fact]
        public void InstallWalkerResolvesLowestMajorAndMinorAndPatchVersionOfListedPackagesForDependencies()
        {
            // Arrange

            // A 1.0 -> B 1.0
            // B 1.0 -> C 1.1
            // C 1.1 -> D 1.0

            var A10 = PackageUtility.CreatePackage("A", "1.0", dependencies: new[] { PackageDependency.CreateDependency("B", "1.0") });

            var repository = new MockPackageRepository() {
                PackageUtility.CreatePackage("B", "2.0", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") }),
                PackageUtility.CreatePackage("B", "1.0", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") }, listed: false),
                PackageUtility.CreatePackage("B", "1.0.1"),
                A10,
                PackageUtility.CreatePackage("D", "2.0"),
                PackageUtility.CreatePackage("C", "1.1.3", dependencies: new[] { PackageDependency.CreateDependency("D", "1.0") }),
                PackageUtility.CreatePackage("C", "1.1.1", dependencies: new[] { PackageDependency.CreateDependency("D", "1.0") }, listed: false),
                PackageUtility.CreatePackage("C", "1.5.1", dependencies: new[] { PackageDependency.CreateDependency("D", "1.0") }),
                PackageUtility.CreatePackage("B", "1.0.9", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") }),
                PackageUtility.CreatePackage("B", "1.1", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") })
            };


            IPackageOperationResolver resolver = new InstallWalker(new MockPackageRepository(),
                new DependencyResolverFromRepo(repository),
                constraintProvider: null,
                logger: NullLogger.Instance,
                targetFramework: null,
                ignoreDependencies: false,
                allowPrereleaseVersions: false,
                dependencyVersion: DependencyVersion.Lowest);

            // Act
            var packages = resolver.ResolveOperations(A10).ToList();

            // Assert
            Assert.Equal(2, packages.Count);
            Assert.Equal("B", packages[0].Package.Id);
            Assert.Equal(new SemanticVersion("1.0.1"), packages[0].Package.Version);
            Assert.Equal("A", packages[1].Package.Id);
            Assert.Equal(new SemanticVersion("1.0"), packages[1].Package.Version);
        }

        // Tests that when DependencyVersion is HighestPatch, the dependency with the lowest major minor and highest patch version
        // is picked.
        [Fact]
        public void InstallWalkerResolvesLowestMajorAndMinorHighestPatchVersionOfListedPackagesForDependencies()
        {
            // Arrange

            // A 1.0 -> B 1.0
            // B 1.0 -> C 1.1
            // C 1.1 -> D 1.0

            var A10 = PackageUtility.CreatePackage("A", "1.0", dependencies: new[] { PackageDependency.CreateDependency("B", "1.0") });

            var repository = new MockPackageRepository() {
                PackageUtility.CreatePackage("B", "2.0", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") }),
                PackageUtility.CreatePackage("B", "1.0", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") }, listed: false),
                PackageUtility.CreatePackage("B", "1.0.1"),
                A10,
                PackageUtility.CreatePackage("D", "2.0"),
                PackageUtility.CreatePackage("C", "1.1.3", dependencies: new[] { PackageDependency.CreateDependency("D", "1.0") }),
                PackageUtility.CreatePackage("C", "1.1.1", dependencies: new[] { PackageDependency.CreateDependency("D", "1.0") }, listed: false),
                PackageUtility.CreatePackage("C", "1.5.1", dependencies: new[] { PackageDependency.CreateDependency("D", "1.0") }),
                PackageUtility.CreatePackage("B", "1.0.9", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") }),
                PackageUtility.CreatePackage("B", "1.1", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") })
            };


            IPackageOperationResolver resolver = new InstallWalker(new MockPackageRepository(),
                new DependencyResolverFromRepo(repository),
                constraintProvider: null,
                logger: NullLogger.Instance,
                targetFramework: null,
                ignoreDependencies: false,
                allowPrereleaseVersions: false,
                dependencyVersion: DependencyVersion.HighestPatch);

            // Act
            var packages = resolver.ResolveOperations(A10).ToList();

            // Assert
            Assert.Equal(4, packages.Count);
            Assert.Equal("D", packages[0].Package.Id);
            Assert.Equal(new SemanticVersion("2.0"), packages[0].Package.Version);
            Assert.Equal("C", packages[1].Package.Id);
            Assert.Equal(new SemanticVersion("1.1.3"), packages[1].Package.Version);
            Assert.Equal("B", packages[2].Package.Id);
            Assert.Equal(new SemanticVersion("1.0.9"), packages[2].Package.Version);
            Assert.Equal("A", packages[3].Package.Id);
            Assert.Equal(new SemanticVersion("1.0"), packages[3].Package.Version);
        }

        [Fact]
        public void ResolveOperationsForPackagesWherePackagesOrderIsDifferentFromItsDependencyOrder()
        {
            // Arrange

            // A 1.0 -> B 1.0 to 1.5
            // A 2.0 -> B 1.8
            // B 1.0
            // B 2.0
            // C 1.0
            // C 2.0

            var A10 = PackageUtility.CreatePackage("A", "1.0", dependencies: new[] { PackageDependency.CreateDependency("B", "[1.0, 1.5]") });
            var A20 = PackageUtility.CreatePackage("A", "2.0", dependencies: new[] { PackageDependency.CreateDependency("B", "1.8") });
            var B10 = PackageUtility.CreatePackage("B", "1.0");
            var B20 = PackageUtility.CreatePackage("B", "2.0");
            var C10 = PackageUtility.CreatePackage("C", "1.0");
            var C20 = PackageUtility.CreatePackage("C", "2.0");

            var sourceRepository = new MockPackageRepository() {
                A10,                
                A20,
                B10,
                B20,
                C10,
                C20,
            };

            var localRepository = new MockPackageRepository() {
                A10,
                B10,
                C10
            };

            var resolver = new InstallWalker(localRepository,
                new DependencyResolverFromRepo(sourceRepository),
                constraintProvider: NullConstraintProvider.Instance,
                logger: NullLogger.Instance,
                targetFramework: null,
                ignoreDependencies: false,
                allowPrereleaseVersions: false,
                dependencyVersion: DependencyVersion.Lowest);

            var updatePackages = new List<IPackage> { A20, B20, C20 };
            IList<IPackage> allUpdatePackagesByDependencyOrder;

            // Act
            var operations = resolver.ResolveOperations(updatePackages, out allUpdatePackagesByDependencyOrder);

            // Assert
            Assert.True(operations.Count == 3);
            Assert.True(operations[0].Package == B20 && operations[0].Action == PackageAction.Install);
            Assert.True(operations[1].Package == A20 && operations[1].Action == PackageAction.Install);
            Assert.True(operations[2].Package == C20 && operations[2].Action == PackageAction.Install);

            Assert.True(allUpdatePackagesByDependencyOrder[0] == B20);
            Assert.True(allUpdatePackagesByDependencyOrder[1] == A20);
            Assert.True(allUpdatePackagesByDependencyOrder[2] == C20);
        }

        private void AssertOperation(string expectedId, string expectedVersion, PackageAction expectedAction, PackageOperation operation)
        {
            Assert.Equal(expectedAction, operation.Action);
            Assert.Equal(expectedId, operation.Package.Id);
            Assert.Equal(new SemanticVersion(expectedVersion), operation.Package.Version);
        }

        private class TestWalker : PackageWalker
        {
            private readonly IPackageRepository _repository;
            public TestWalker(IPackageRepository repository)
            {
                _repository = repository;
            }

            protected override IPackage ResolveDependency(PackageDependency dependency)
            {
                return DependencyResolveUtility.ResolveDependency(
                    _repository, dependency, AllowPrereleaseVersions, false);
            }
        }
    }
}
