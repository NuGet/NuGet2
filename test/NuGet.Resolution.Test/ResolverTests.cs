using Newtonsoft.Json.Linq;
using NuGet.Client;
using NuGet.Client.Interop;
using NuGet.Client.ProjectSystem;
using NuGet.Client.Resolution;
using NuGet.Test;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NuGet.Resolution.Test
{
    public class ResolverTests
    {
        //[Fact]
        //public void ResolveDependenciesForInstallCircularReferenceThrows()
        //{
            
            //// Arrange
            //var localRepository = new MockPackageRepository();
            //var sourceRepository = new MockPackageRepository();

            //IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
            //                                                dependencies: new List<PackageDependency> {
            //                                                        new PackageDependency("B")
            //                                                    });


            //IPackage packageB = PackageUtility.CreatePackage("B", "1.0",
            //                                                dependencies: new List<PackageDependency> {
            //                                                        new PackageDependency("A")
            //                                                    });

            //sourceRepository.AddPackage(packageA);
            //sourceRepository.AddPackage(packageB);

            //IPackageOperationResolver resolver = new EggoInstallWalker(localRepository,
            //                                                 sourceRepository,
            //                                                 NullLogger.Instance,
            //                                                 ignoreDependencies: false,
            //                                                 allowPrereleaseVersions: false,
            //                                                 dependencyVersion: DependencyVersion.Lowest);

            //// Act & Assert
            //ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveOperations(packageA), "Circular dependency detected 'A 1.0 => B 1.0 => A 1.0'.");
        //}

        [Fact]
        public void ResolveDependenciesForInstallDiamondDependencyGraph()
        {
            // Arrange
            var sourceRepository = new MockSourceRepository();
            // A -> [B, C]
            // B -> [D]
            // C -> [D]
            //    A
            //   / \
            //  B   C
            //   \ /
            //    D 

            var packageA = CreatePackage("A", "1.0", new Dictionary<string, string>() { { "B", null }, { "C", null } });
            var packageB = CreatePackage("B", "1.0", new Dictionary<string, string>() { { "D", null } });
            var packageC = CreatePackage("C", "1.0", new Dictionary<string, string>() { { "D", null } });
            var packageD = CreatePackage("D", "1.0");

            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageC);
            sourceRepository.AddPackage(packageD);

            var resolver = new Resolver() { DependencyVersion = DependencyBehavior.Lowest };
            resolver.AddOperation(PackageActionType.Install, new PackageIdentity("A", new NuGetVersion("1.0")), null, sourceRepository);
            var actions = resolver.ResolveActionsAsync().GetAwaiter().GetResult().ToDictionary(p => p.PackageIdentity.Id);

            // Assert
            Assert.Equal(4, actions.Count());
            Assert.NotNull(actions["A"]);
            Assert.NotNull(actions["B"]);
            Assert.NotNull(actions["C"]);
            Assert.NotNull(actions["D"]);
        }


        [Fact]
        public void ResolveDependenciesForInstallDiamondDependencyGraphWithDifferentVersionsOfSamePackage()
        {
            // Arrange
            var sourceRepository = new MockSourceRepository();
            // A -> [B, C]
            // B -> [D >= 1, E >= 2]
            // C -> [D >= 2, E >= 1]
            //     A
            //   /   \
            //  B     C
            //  | \   | \
            //  D1 E2 D2 E1

            var packageA = CreatePackage("A", "1.0", new Dictionary<string, string>() { { "B", null }, { "C", null } });
            var packageB = CreatePackage("B", "1.0", new Dictionary<string, string>() { { "D", "1.0" }, { "E", "2.0" } });
            var packageC = CreatePackage("C", "1.0", new Dictionary<string, string>() { { "D", "2.0" }, { "E", "1.0" } });
            var packageD1 = CreatePackage("D", "1.0");
            var packageD2 = CreatePackage("D", "2.0");
            var packageE1 = CreatePackage("E", "1.0");
            var packageE2 = CreatePackage("E", "2.0");

            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageC);
            sourceRepository.AddPackage(packageD2);
            sourceRepository.AddPackage(packageD1);
            sourceRepository.AddPackage(packageE2);
            sourceRepository.AddPackage(packageE1);

            // Act
            var resolver = new Resolver() { DependencyVersion = DependencyBehavior.Lowest };
            resolver.AddOperation(PackageActionType.Install, new PackageIdentity("A", new NuGetVersion("1.0")), null, sourceRepository);
            var actions = resolver.ResolveActionsAsync().GetAwaiter().GetResult().ToDictionary(p => p.PackageIdentity.Id);

            // Assert
            Assert.Equal(5, actions.Count);
            Assert.Equal("1.0.0", actions["A"].PackageIdentity.Version.ToNormalizedString());
            Assert.Equal("1.0.0", actions["B"].PackageIdentity.Version.ToNormalizedString());
            Assert.Equal("1.0.0", actions["C"].PackageIdentity.Version.ToNormalizedString());
            Assert.Equal("2.0.0", actions["D"].PackageIdentity.Version.ToNormalizedString());
            Assert.Equal("2.0.0", actions["E"].PackageIdentity.Version.ToNormalizedString());
        }

        //[Fact]
        //public void UninstallWalkerIgnoresMissingDependencies()
        //{
        //    // Arrange
        //    var localRepository = new MockPackageRepository();
        //    // A -> [B, C]
        //    // B -> [D]
        //    // C -> [D]

        //    IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            new PackageDependency("B"),
        //                                                            new PackageDependency("C")
        //                                                        });

        //    IPackage packageC = PackageUtility.CreatePackage("C", "1.0",
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            new PackageDependency("D")
        //                                                        });

        //    IPackage packageD = PackageUtility.CreatePackage("D", "1.0");

        //    localRepository.AddPackage(packageA);
        //    localRepository.AddPackage(packageC);
        //    localRepository.AddPackage(packageD);

        //    IPackageOperationResolver resolver = new UninstallWalker(localRepository,
        //                                                       new DependentsWalker(localRepository),
        //                                                       NullLogger.Instance,
        //                                                       removeDependencies: true,
        //                                                       forceRemove: false);

        //    // Act
        //    var packages = resolver.ResolveOperations(packageA)
        //                           .ToDictionary(p => p.Package.Id);

        //    // Assert
        //    Assert.Equal(3, packages.Count);
        //    Assert.NotNull(packages["A"]);
        //    Assert.NotNull(packages["C"]);
        //    Assert.NotNull(packages["D"]);
        //}

        //[Fact]
        //public void ResolveDependenciesForUninstallDiamondDependencyGraph()
        //{
        //    // Arrange
        //    var localRepository = new MockPackageRepository();
        //    // A -> [B, C]
        //    // B -> [D]
        //    // C -> [D]
        //    //    A
        //    //   / \
        //    //  B   C
        //    //   \ /
        //    //    D 

        //    IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            new PackageDependency("B"),
        //                                                            new PackageDependency("C")
        //                                                        });


        //    IPackage packageB = PackageUtility.CreatePackage("B", "1.0",
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            new PackageDependency("D")
        //                                                        });

        //    IPackage packageC = PackageUtility.CreatePackage("C", "1.0",
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            new PackageDependency("D")
        //                                                        });

        //    IPackage packageD = PackageUtility.CreatePackage("D", "1.0");

        //    localRepository.AddPackage(packageA);
        //    localRepository.AddPackage(packageB);
        //    localRepository.AddPackage(packageC);
        //    localRepository.AddPackage(packageD);

        //    IPackageOperationResolver resolver = new UninstallWalker(localRepository,
        //                                                       new DependentsWalker(localRepository),
        //                                                       NullLogger.Instance,
        //                                                       removeDependencies: true,
        //                                                       forceRemove: false);



        //    // Act
        //    var packages = resolver.ResolveOperations(packageA)
        //                           .ToDictionary(p => p.Package.Id);

        //    // Assert
        //    Assert.Equal(4, packages.Count);
        //    Assert.NotNull(packages["A"]);
        //    Assert.NotNull(packages["B"]);
        //    Assert.NotNull(packages["C"]);
        //    Assert.NotNull(packages["D"]);
        //}

        //[Fact]
        //public void ResolveDependencyForInstallCircularReferenceWithDifferentVersionOfPackageReferenceThrows()
        //{
        //    // Arrange
        //    var localRepository = new MockPackageRepository();
        //    var sourceRepository = new MockPackageRepository();

        //    IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            new PackageDependency("B")
        //                                                        });

        //    IPackage packageA15 = PackageUtility.CreatePackage("A", "1.5",
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            new PackageDependency("B")
        //                                                        });


        //    IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0",
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            PackageDependency.CreateDependency("A", "[1.5]")
        //                                                        });

        //    sourceRepository.AddPackage(packageA10);
        //    sourceRepository.AddPackage(packageA15);
        //    sourceRepository.AddPackage(packageB10);

        //    IPackageOperationResolver resolver = new EggoInstallWalker(localRepository,
        //                                                     sourceRepository,
        //                                                     NullLogger.Instance,
        //                                                     ignoreDependencies: false,
        //                                                     allowPrereleaseVersions: false,
        //                                                     dependencyVersion: DependencyVersion.Lowest);


        //    // Act & Assert
        //    ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveOperations(packageA10), "Circular dependency detected 'A 1.0 => B 1.0 => A 1.5'.");
        //}

        //[Fact]
        //public void ResolvingDependencyForUpdateWithConflictingDependents()
        //{
        //    // Arrange
        //    var localRepository = new MockPackageRepository();
        //    var sourceRepository = new MockPackageRepository();

        //    // A 1.0 -> B [1.0]
        //    IPackage A10 = PackageUtility.CreatePackage("A", "1.0",
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            PackageDependency.CreateDependency("B", "[1.0]")
        //                                                        }, content: new[] { "a1" });

        //    // A 2.0 -> B (any version)
        //    IPackage A20 = PackageUtility.CreatePackage("A", "2.0",
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            new PackageDependency("B")
        //                                                        }, content: new[] { "a2" });

        //    IPackage B10 = PackageUtility.CreatePackage("B", "1.0", content: new[] { "b1" });
        //    IPackage B101 = PackageUtility.CreatePackage("B", "1.0.1", content: new[] { "b101" });
        //    IPackage B20 = PackageUtility.CreatePackage("B", "2.0", content: new[] { "a2" });
        //    localRepository.Add(A10);
        //    localRepository.Add(B10);
        //    sourceRepository.AddPackage(A10);
        //    sourceRepository.AddPackage(A20);
        //    sourceRepository.AddPackage(B10);
        //    sourceRepository.AddPackage(B101);
        //    sourceRepository.AddPackage(B20);

        //    IPackageOperationResolver resolver = new UpdateWalker(localRepository,
        //                                                          sourceRepository,
        //                                                          new DependentsWalker(localRepository),
        //                                                          NullConstraintProvider.Instance,
        //                                                          NullLogger.Instance,
        //                                                          updateDependencies: true,
        //                                                          allowPrereleaseVersions: false) { AcceptedTargets = PackageTargets.Project };

        //    // Act
        //    var packages = resolver.ResolveOperations(B101).ToList();

        //    // Assert
        //    Assert.Equal(4, packages.Count);
        //    AssertOperation("A", "1.0", PackageAction.Uninstall, packages[0]);
        //    AssertOperation("B", "1.0", PackageAction.Uninstall, packages[1]);
        //    AssertOperation("A", "2.0", PackageAction.Install, packages[2]);
        //    AssertOperation("B", "1.0.1", PackageAction.Install, packages[3]);
        //}

        //[Fact]
        //public void ResolvingDependencyForUpdateThatHasAnUnsatisfiedConstraint()
        //{
        //    // Arrange
        //    var localRepository = new MockPackageRepository();
        //    var sourceRepository = new MockPackageRepository();
        //    var constraintProvider = new Mock<IPackageConstraintProvider>();
        //    constraintProvider.Setup(m => m.GetConstraint("B")).Returns(VersionUtility.ParseVersionSpec("[1.4]"));
        //    constraintProvider.Setup(m => m.Source).Returns("foo");

        //    IPackage A10 = PackageUtility.CreatePackage("A", "1.0",
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            PackageDependency.CreateDependency("B", "1.5")
        //                                                        });
        //    IPackage A20 = PackageUtility.CreatePackage("A", "2.0",
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            PackageDependency.CreateDependency("B", "2.0")
        //                                                        });

        //    IPackage B15 = PackageUtility.CreatePackage("B", "1.5");
        //    IPackage B20 = PackageUtility.CreatePackage("B", "2.0");
        //    localRepository.Add(A10);
        //    localRepository.Add(B15);
        //    sourceRepository.AddPackage(A10);
        //    sourceRepository.AddPackage(A20);
        //    sourceRepository.AddPackage(B15);
        //    sourceRepository.AddPackage(B20);

        //    IPackageOperationResolver resolver = new EggoInstallWalker(localRepository,
        //                                                           sourceRepository,
        //                                                           constraintProvider.Object,
        //                                                           null,
        //                                                           NullLogger.Instance,
        //                                                           ignoreDependencies: false,
        //                                                           allowPrereleaseVersions: false,
        //                                                           dependencyVersion: DependencyVersion.Lowest);

        //    // Act & Assert
        //    ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveOperations(A20), "Unable to resolve dependency 'B (\u2265 2.0)'.'B' has an additional constraint (= 1.4) defined in foo.");
        //}

        [Fact]
        public void ResolveDependencyForInstallPackageWithDependencyThatDoesntMeetMinimumVersionThrows()
        {
            // Arrange
            var sourceRepository = new MockSourceRepository();
            var packageA = CreatePackage("A", "1.0", new Dictionary<string, string>() { { "B", "1.5" } });
            var packageB = CreatePackage("B", "1.4");
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);

            // Act & Assert
            var resolver = new Resolver() { DependencyVersion = DependencyBehavior.Lowest };
            resolver.AddOperation(PackageActionType.Install, new PackageIdentity("A", new NuGetVersion("1.0")), null, sourceRepository);
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveActionsAsync().GetAwaiter().GetResult());
        }

        [Fact]
        public void ResolveDependencyForInstallPackageWithDependencyThatDoesntMeetExactVersionThrows()
        {
            // Arrange
            var sourceRepository = new MockSourceRepository();
            var packageA = CreatePackage("A", "1.0", new Dictionary<string, string>() { { "B", "[1.3]" } });
            var packageB = CreatePackage("B", "1.4");
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);

            // Act & Assert
            var resolver = new Resolver() { DependencyVersion = DependencyBehavior.Lowest };
            resolver.AddOperation(PackageActionType.Install, new PackageIdentity("A", new NuGetVersion("1.0")), null, sourceRepository);
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveActionsAsync().GetAwaiter().GetResult());
        }

        [Fact]
        public void ResolveOperationsForInstallSameDependencyAtDifferentLevelsInGraph()
        {
            // Arrange
            var sourceRepository = new MockSourceRepository();
            //A1 -> B1, C1
            var packageA = CreatePackage("A", "1.0", new Dictionary<string, string>() { { "B", "1.0" }, { "C", "1.0" } });
            // B1
            var packageB = CreatePackage("B", "1.0");
            // C1 -> B1, D1
            var packageC = CreatePackage("C", "1.0", new Dictionary<string, string>() { { "B", "1.0" }, { "D", "1.0" } });
            // D1 -> B1
            var packageD = CreatePackage("D", "1.0", new Dictionary<string, string>() { { "B", "1.0" } });

            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageC);
            sourceRepository.AddPackage(packageD);

            // Act
            var resolver = new Resolver() { DependencyVersion = DependencyBehavior.Lowest };
            resolver.AddOperation(PackageActionType.Install, new PackageIdentity("A", new NuGetVersion("1.0")), null, sourceRepository);
            var actions = resolver.ResolveActionsAsync().GetAwaiter().GetResult().ToDictionary(p => p.PackageIdentity.Id);

            // Assert
            Assert.Equal(4, actions.Count());
            Assert.NotNull(actions["A"]);
            Assert.NotNull(actions["B"]);
            Assert.NotNull(actions["C"]);
            Assert.NotNull(actions["D"]);
        }

        //[Fact]
        //public void ResolveDependenciesForInstallSameDependencyAtDifferentLevelsInGraphDuringUpdate()
        //{
        //    // Arrange
        //    var localRepository = new MockPackageRepository();
        //    var sourceRepository = new MockPackageRepository();

        //    // A1 -> B1, C1
        //    IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
        //                                                    content: new[] { "A1" },
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            PackageDependency.CreateDependency("B", "1.0"),
        //                                                            PackageDependency.CreateDependency("C", "1.0")
        //                                                        });
        //    // B1
        //    IPackage packageB = PackageUtility.CreatePackage("B", "1.0", new[] { "B1" });

        //    // C1 -> B1, D1
        //    IPackage packageC = PackageUtility.CreatePackage("C", "1.0",
        //                                                    content: new[] { "C1" },
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            PackageDependency.CreateDependency("B", "1.0"),
        //                                                            PackageDependency.CreateDependency("D", "1.0")
        //                                                        });

        //    // D1 -> B1
        //    IPackage packageD = PackageUtility.CreatePackage("D", "1.0",
        //                                                    content: new[] { "A1" },
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            PackageDependency.CreateDependency("B", "1.0")
        //                                                        });

        //    // A2 -> B2, C2
        //    IPackage packageA2 = PackageUtility.CreatePackage("A", "2.0",
        //                                                    content: new[] { "A2" },
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            PackageDependency.CreateDependency("B", "2.0"),
        //                                                            PackageDependency.CreateDependency("C", "2.0")
        //                                                        });
        //    // B2
        //    IPackage packageB2 = PackageUtility.CreatePackage("B", "2.0", new[] { "B2" });

        //    // C2 -> B2, D2
        //    IPackage packageC2 = PackageUtility.CreatePackage("C", "2.0",
        //                                                    content: new[] { "C2" },
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            PackageDependency.CreateDependency("B", "2.0"),
        //                                                            PackageDependency.CreateDependency("D", "2.0")
        //                                                        });

        //    // D2 -> B2
        //    IPackage packageD2 = PackageUtility.CreatePackage("D", "2.0",
        //                                                    content: new[] { "D2" },
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            PackageDependency.CreateDependency("B", "2.0")
        //                                                        });


        //    sourceRepository.AddPackage(packageA);
        //    sourceRepository.AddPackage(packageB);
        //    sourceRepository.AddPackage(packageC);
        //    sourceRepository.AddPackage(packageD);
        //    sourceRepository.AddPackage(packageA2);
        //    sourceRepository.AddPackage(packageB2);
        //    sourceRepository.AddPackage(packageC2);
        //    sourceRepository.AddPackage(packageD2);

        //    localRepository.AddPackage(packageA);
        //    localRepository.AddPackage(packageB);
        //    localRepository.AddPackage(packageC);
        //    localRepository.AddPackage(packageD);



        //    IPackageOperationResolver resolver = new UpdateWalker(localRepository,
        //                                                                  sourceRepository,
        //                                                                  new DependentsWalker(localRepository),
        //                                                                  NullConstraintProvider.Instance,
        //                                                                  NullLogger.Instance,
        //                                                                  updateDependencies: true,
        //                                                                  allowPrereleaseVersions: false);

        //    var operations = resolver.ResolveOperations(packageA2).ToList();
        //    Assert.Equal(8, operations.Count);
        //    AssertOperation("A", "1.0", PackageAction.Uninstall, operations[0]);
        //    AssertOperation("C", "1.0", PackageAction.Uninstall, operations[1]);
        //    AssertOperation("D", "1.0", PackageAction.Uninstall, operations[2]);
        //    AssertOperation("B", "1.0", PackageAction.Uninstall, operations[3]);
        //    AssertOperation("B", "2.0", PackageAction.Install, operations[4]);
        //    AssertOperation("D", "2.0", PackageAction.Install, operations[5]);
        //    AssertOperation("C", "2.0", PackageAction.Install, operations[6]);
        //    AssertOperation("A", "2.0", PackageAction.Install, operations[7]);
        //}

        //[Fact]
        //public void ResolveDependenciesForInstallPackageWithDependencyReturnsPackageAndDependency()
        //{
        //    // Arrange            
        //    var localRepository = new MockPackageRepository();

        //    IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            new PackageDependency("B")
        //                                                        });

        //    IPackage packageB = PackageUtility.CreatePackage("B", "1.0");

        //    localRepository.AddPackage(packageA);
        //    localRepository.AddPackage(packageB);

        //    IPackageOperationResolver resolver = new UninstallWalker(localRepository,
        //                                                       new DependentsWalker(localRepository),
        //                                                       NullLogger.Instance,
        //                                                       removeDependencies: true,
        //                                                       forceRemove: false);

        //    // Act
        //    var packages = resolver.ResolveOperations(packageA)
        //                                    .ToDictionary(p => p.Package.Id);

        //    // Assert
        //    Assert.Equal(2, packages.Count);
        //    Assert.NotNull(packages["A"]);
        //    Assert.NotNull(packages["B"]);
        //}

        //[Fact]
        //public void ResolveDependenciesForUninstallPackageWithDependentThrows()
        //{
        //    // Arrange
        //    var localRepository = new MockPackageRepository();

        //    IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            new PackageDependency("B")
        //                                                        });

        //    IPackage packageB = PackageUtility.CreatePackage("B", "1.0");

        //    localRepository.AddPackage(packageA);
        //    localRepository.AddPackage(packageB);

        //    IPackageOperationResolver resolver = new UninstallWalker(localRepository,
        //                                                       new DependentsWalker(localRepository),
        //                                                       NullLogger.Instance,
        //                                                       removeDependencies: false,
        //                                                       forceRemove: false);

        //    // Act & Assert
        //    ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveOperations(packageB), "Unable to uninstall 'B 1.0' because 'A 1.0' depends on it.");
        //}

        //[Fact]
        //public void ResolveDependenciesForUninstallPackageWithDependentAndRemoveDependenciesThrows()
        //{
        //    // Arrange
        //    var localRepository = new MockPackageRepository();

        //    IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            new PackageDependency("B")
        //                                                        });

        //    IPackage packageB = PackageUtility.CreatePackage("B", "1.0");

        //    localRepository.AddPackage(packageA);
        //    localRepository.AddPackage(packageB);

        //    IPackageOperationResolver resolver = new UninstallWalker(localRepository,
        //                                                       new DependentsWalker(localRepository),
        //                                                       NullLogger.Instance,
        //                                                       removeDependencies: true,
        //                                                       forceRemove: false);

        //    // Act & Assert
        //    ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveOperations(packageB), "Unable to uninstall 'B 1.0' because 'A 1.0' depends on it.");
        //}

        //[Fact]
        //public void ResolveDependenciesForUninstallPackageWithDependentAndForceReturnsPackage()
        //{
        //    // Arrange
        //    var localRepository = new MockPackageRepository();

        //    IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            new PackageDependency("B")
        //                                                        });

        //    IPackage packageB = PackageUtility.CreatePackage("B", "1.0");

        //    localRepository.AddPackage(packageA);
        //    localRepository.AddPackage(packageB);

        //    IPackageOperationResolver resolver = new UninstallWalker(localRepository,
        //                                                       new DependentsWalker(localRepository),
        //                                                       NullLogger.Instance,
        //                                                       removeDependencies: false,
        //                                                       forceRemove: true);

        //    // Act
        //    var packages = resolver.ResolveOperations(packageB)
        //                     .ToDictionary(p => p.Package.Id);

        //    // Assert
        //    Assert.Equal(1, packages.Count);
        //    Assert.NotNull(packages["B"]);
        //}

        //[Fact]
        //public void ResolveDependenciesForUninstallPackageWithRemoveDependenciesExcludesDependencyIfDependencyInUse()
        //{
        //    // Arrange
        //    var localRepository = new MockPackageRepository();

        //    // A 1.0 -> [B, C] 
        //    IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
        //                                                    dependencies: new List<PackageDependency> {
        //                                                        new PackageDependency("B"),
        //                                                        new PackageDependency("C")
        //                                                    });


        //    IPackage packageB = PackageUtility.CreatePackage("B", "1.0");
        //    IPackage packageC = PackageUtility.CreatePackage("C", "1.0");

        //    // D -> [C]
        //    IPackage packageD = PackageUtility.CreatePackage("D", "1.0",
        //                                                    dependencies: new List<PackageDependency> {
        //                                                        new PackageDependency("C"),
        //                                                    });

        //    localRepository.AddPackage(packageD);
        //    localRepository.AddPackage(packageA);
        //    localRepository.AddPackage(packageB);
        //    localRepository.AddPackage(packageC);

        //    IPackageOperationResolver resolver = new UninstallWalker(localRepository,
        //                                                       new DependentsWalker(localRepository),
        //                                                       NullLogger.Instance,
        //                                                       removeDependencies: true,
        //                                                       forceRemove: false);

        //    // Act
        //    var packages = resolver.ResolveOperations(packageA)
        //                           .ToDictionary(p => p.Package.Id);

        //    // Assert            
        //    Assert.Equal(2, packages.Count);
        //    Assert.NotNull(packages["A"]);
        //    Assert.NotNull(packages["B"]);
        //}

        //[Fact]
        //public void ResolveDependenciesForUninstallPackageWithRemoveDependenciesSetAndForceReturnsAllDependencies()
        //{
        //    // Arrange
        //    var localRepository = new MockPackageRepository();

        //    // A 1.0 -> [B, C]
        //    IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
        //                                                    dependencies: new List<PackageDependency> {
        //                                                            new PackageDependency("B"),
        //                                                            new PackageDependency("C")
        //                                                    });

        //    IPackage packageB = PackageUtility.CreatePackage("B", "1.0");
        //    IPackage packageC = PackageUtility.CreatePackage("C", "1.0");

        //    // D -> [C]
        //    IPackage packageD = PackageUtility.CreatePackage("D", "1.0",
        //                                                    dependencies: new List<PackageDependency> {
        //                                                        new PackageDependency("C"),
        //                                                    });

        //    localRepository.AddPackage(packageA);
        //    localRepository.AddPackage(packageB);
        //    localRepository.AddPackage(packageC);
        //    localRepository.AddPackage(packageD);

        //    IPackageOperationResolver resolver = new UninstallWalker(localRepository,
        //                                                       new DependentsWalker(localRepository),
        //                                                       NullLogger.Instance,
        //                                                       removeDependencies: true,
        //                                                       forceRemove: true);

        //    // Act
        //    var packages = resolver.ResolveOperations(packageA)
        //                           .ToDictionary(p => p.Package.Id);

        //    // Assert            
        //    Assert.NotNull(packages["A"]);
        //    Assert.NotNull(packages["B"]);
        //    Assert.NotNull(packages["C"]);
        //}

        //[Fact]
        //public void ProjectInstallWalkerIgnoresSolutionLevelPackages()
        //{
        //    // Arrange
        //    var localRepository = new MockPackageRepository();
        //    var sourceRepository = new MockPackageRepository();

        //    IPackage projectPackage = PackageUtility.CreatePackage("A", "1.0",
        //        dependencies: new List<PackageDependency> {
        //            PackageDependency.CreateDependency("B", "[1.5]")
        //        },
        //        content: new[] { "content" });

        //    sourceRepository.AddPackage(projectPackage);

        //    IPackage toolsPackage = PackageUtility.CreatePackage("B", "1.5",
        //        content: Enumerable.Empty<string>(),
        //        tools: new[] { "init.ps1" });
        //    sourceRepository.AddPackage(toolsPackage);

        //    IPackageOperationResolver resolver = new UpdateWalker(localRepository,
        //                                                           sourceRepository,
        //                                                           new DependentsWalker(localRepository),
        //                                                           NullConstraintProvider.Instance,
        //                                                           NullLogger.Instance,
        //                                                           updateDependencies: true,
        //                                                           allowPrereleaseVersions: false) { AcceptedTargets = PackageTargets.Project };

        //    // Act
        //    var packages = resolver.ResolveOperations(projectPackage)
        //                           .ToDictionary(p => p.Package.Id);

        //    // Assert            
        //    Assert.Equal(1, packages.Count);
        //    Assert.NotNull(packages["A"]);
        //}

        //[Fact]
        //public void AfterPackageWalkMetaPackageIsClassifiedTheSameAsDependencies()
        //{
        //    // Arrange
        //    var mockRepository = new MockPackageRepository();
        //    var walker = new TestWalker(mockRepository);

        //    IPackage metaPackage = PackageUtility.CreatePackage(
        //        "A", "1.0",
        //        content: Enumerable.Empty<string>(),
        //        dependencies: new List<PackageDependency> {
        //            new PackageDependency("B"),
        //            new PackageDependency("C")
        //        },
        //        createRealStream: false);

        //    IPackage projectPackageA = PackageUtility.CreatePackage("B", "1.0", content: new[] { "contentB" });
        //    IPackage projectPackageB = PackageUtility.CreatePackage("C", "1.0", content: new[] { "contentC" });

        //    mockRepository.AddPackage(projectPackageA);
        //    mockRepository.AddPackage(projectPackageB);

        //    Assert.Equal(PackageTargets.None, walker.GetPackageInfo(metaPackage).Target);

        //    // Act
        //    walker.Walk(metaPackage);

        //    // Assert
        //    Assert.Equal(PackageTargets.Project, walker.GetPackageInfo(metaPackage).Target);
        //}

        //[Fact]
        //public void LocalizedIntelliSenseFileCountsAsProjectTarget()
        //{
        //    // Arrange
        //    var mockRepository = new MockPackageRepository();
        //    var walker = new TestWalker(mockRepository);

        //    IPackage runtimePackage = PackageUtility.CreatePackage("A", "1.0",
        //                                                    assemblyReferences: new[] { @"lib\A.dll", @"lib\A.xml" });

        //    IPackage satellitePackage = PackageUtility.CreatePackage("A.fr-fr", "1.0",
        //                                                    dependencies: new[] { new PackageDependency("A") },
        //                                                    satelliteAssemblies: new[] { @"lib\fr-fr\A.xml" },
        //                                                    language: "fr-fr");

        //    mockRepository.AddPackage(runtimePackage);
        //    mockRepository.AddPackage(satellitePackage);

        //    // Act
        //    walker.Walk(satellitePackage);

        //    // Assert
        //    Assert.Equal(PackageTargets.Project, walker.GetPackageInfo(satellitePackage).Target);
        //}

        //[Fact]
        //public void AfterPackageWalkSatellitePackageIsClassifiedTheSameAsDependencies()
        //{
        //    // Arrange
        //    var mockRepository = new MockPackageRepository();
        //    var walker = new TestWalker(mockRepository);

        //    IPackage runtimePackage = PackageUtility.CreatePackage("A", "1.0",
        //                                                    assemblyReferences: new[] { @"lib\A.dll" });

        //    IPackage satellitePackage = PackageUtility.CreatePackage("A.fr-fr", "1.0",
        //                                                    dependencies: new[] { new PackageDependency("A") },
        //                                                    satelliteAssemblies: new[] { @"lib\fr-fr\A.resources.dll" },
        //                                                    language: "fr-fr");

        //    mockRepository.AddPackage(runtimePackage);
        //    mockRepository.AddPackage(satellitePackage);

        //    // Act
        //    walker.Walk(satellitePackage);

        //    // Assert
        //    Assert.Equal(PackageTargets.Project, walker.GetPackageInfo(satellitePackage).Target);
        //}

        //[Fact]
        //public void MetaPackageWithMixedTargetsThrows()
        //{
        //    // Arrange
        //    var mockRepository = new MockPackageRepository();
        //    var walker = new TestWalker(mockRepository);

        //    IPackage metaPackage = PackageUtility.CreatePackage("A", "1.0",
        //        content: Enumerable.Empty<string>(),
        //        dependencies: new List<PackageDependency> {
        //            new PackageDependency("B"),
        //            new PackageDependency("C")
        //        },
        //        createRealStream: false);

        //    IPackage projectPackageA = PackageUtility.CreatePackage("B", "1.0", content: new[] { "contentB" });
        //    IPackage solutionPackage = PackageUtility.CreatePackage("C", "1.0", content: Enumerable.Empty<string>(), tools: new[] { "tools" });

        //    mockRepository.AddPackage(projectPackageA);
        //    mockRepository.AddPackage(solutionPackage);

        //    // Act && Assert
        //    ExceptionAssert.Throws<InvalidOperationException>(() => walker.Walk(metaPackage), "Child dependencies of dependency only packages cannot mix external and project packages.");
        //}

        //[Fact]
        //public void ExternalPackagesThatDepdendOnProjectLevelPackagesThrows()
        //{
        //    // Arrange
        //    var mockRepository = new MockPackageRepository();
        //    var walker = new TestWalker(mockRepository);

        //    IPackage solutionPackage = PackageUtility.CreatePackage(
        //        "A", "1.0",
        //        dependencies: new List<PackageDependency> {
        //            new PackageDependency("B")
        //        },
        //        content: Enumerable.Empty<string>(),
        //        tools: new[] { "install.ps1" });

        //    IPackage projectPackageA = PackageUtility.CreatePackage("B", "1.0", content: new[] { "contentB" });

        //    mockRepository.AddPackage(projectPackageA);
        //    mockRepository.AddPackage(solutionPackage);

        //    // Act && Assert
        //    ExceptionAssert.Throws<InvalidOperationException>(() => walker.Walk(solutionPackage), "External packages cannot depend on packages that target projects.");
        //}

        //[Fact]
        //public void InstallWalkerResolvesLowestMajorAndMinorVersionForDependencies()
        //{
        //    // Arrange

        //    // A 1.0 -> B 1.0
        //    // B 1.0 -> C 1.1
        //    // C 1.1 -> D 1.0

        //    var A10 = PackageUtility.CreatePackage("A", "1.0", dependencies: new[] { PackageDependency.CreateDependency("B", "1.0") });

        //    var repository = new MockPackageRepository() {
        //        PackageUtility.CreatePackage("B", "2.0", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") }),
        //        PackageUtility.CreatePackage("B", "1.0", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") }),
        //        PackageUtility.CreatePackage("B", "1.0.1"),
        //        A10,
        //        PackageUtility.CreatePackage("D", "2.0"),
        //        PackageUtility.CreatePackage("C", "1.1.3", dependencies: new[] { PackageDependency.CreateDependency("D", "1.0") }),
        //        PackageUtility.CreatePackage("C", "1.1.1", dependencies: new[] { PackageDependency.CreateDependency("D", "1.0") }),
        //        PackageUtility.CreatePackage("C", "1.5.1", dependencies: new[] { PackageDependency.CreateDependency("D", "1.0") }),
        //        PackageUtility.CreatePackage("B", "1.0.9", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") }),
        //        PackageUtility.CreatePackage("B", "1.1", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") })
        //    };


        //    IPackageOperationResolver resolver = new EggoInstallWalker(
        //        new MockPackageRepository(),
        //        repository,
        //        NullLogger.Instance,
        //        ignoreDependencies: false,
        //        allowPrereleaseVersions: false,
        //        dependencyVersion: DependencyVersion.HighestPatch);

        //    // Act
        //    var packages = resolver.ResolveOperations(A10).ToDictionary(o => o.Package.Id);

        //    // Assert
        //    Assert.Equal(4, packages.Count);

        //    Assert.Equal(new SemanticVersion("2.0"), packages["D"].Package.Version);
        //    Assert.Equal(new SemanticVersion("1.1.3"), packages["C"].Package.Version);
        //    Assert.Equal(new SemanticVersion("1.0.9"), packages["B"].Package.Version);
        //    Assert.Equal(new SemanticVersion("1.0"), packages["A"].Package.Version);
        //}

        //// Tests that when DependencyVersion is lowest, the dependency with the lowest major minor and patch version
        //// is picked.
        //[Fact]
        //public void InstallWalkerResolvesLowestMajorAndMinorAndPatchVersionOfListedPackagesForDependencies()
        //{
        //    // Arrange

        //    // A 1.0 -> B 1.0
        //    // B 1.0 -> C 1.1
        //    // C 1.1 -> D 1.0

        //    var A10 = PackageUtility.CreatePackage("A", "1.0", dependencies: new[] { PackageDependency.CreateDependency("B", "1.0") });

        //    var repository = new MockPackageRepository() {
        //        PackageUtility.CreatePackage("B", "2.0", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") }),
        //        PackageUtility.CreatePackage("B", "1.0", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") }, listed: false),
        //        PackageUtility.CreatePackage("B", "1.0.1"),
        //        A10,
        //        PackageUtility.CreatePackage("D", "2.0"),
        //        PackageUtility.CreatePackage("C", "1.1.3", dependencies: new[] { PackageDependency.CreateDependency("D", "1.0") }),
        //        PackageUtility.CreatePackage("C", "1.1.1", dependencies: new[] { PackageDependency.CreateDependency("D", "1.0") }, listed: false),
        //        PackageUtility.CreatePackage("C", "1.5.1", dependencies: new[] { PackageDependency.CreateDependency("D", "1.0") }),
        //        PackageUtility.CreatePackage("B", "1.0.9", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") }),
        //        PackageUtility.CreatePackage("B", "1.1", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") })
        //    };


        //    IPackageOperationResolver resolver = new EggoInstallWalker(new MockPackageRepository(),
        //        repository,
        //        constraintProvider: null,
        //        logger: NullLogger.Instance,
        //        targetFramework: null,
        //        ignoreDependencies: false,
        //        allowPrereleaseVersions: false,
        //        dependencyVersion: DependencyVersion.Lowest);

        //    // Act
        //    var packages = resolver.ResolveOperations(A10).ToList();

        //    // Assert
        //    Assert.Equal(2, packages.Count);
        //    Assert.Equal("B", packages[0].Package.Id);
        //    Assert.Equal(new SemanticVersion("1.0.1"), packages[0].Package.Version);
        //    Assert.Equal("A", packages[1].Package.Id);
        //    Assert.Equal(new SemanticVersion("1.0"), packages[1].Package.Version);
        //}

        //// Tests that when DependencyVersion is HighestPatch, the dependency with the lowest major minor and highest patch version
        //// is picked.
        //[Fact]
        //public void InstallWalkerResolvesLowestMajorAndMinorHighestPatchVersionOfListedPackagesForDependencies()
        //{
        //    // Arrange

        //    // A 1.0 -> B 1.0
        //    // B 1.0 -> C 1.1
        //    // C 1.1 -> D 1.0

        //    var A10 = PackageUtility.CreatePackage("A", "1.0", dependencies: new[] { PackageDependency.CreateDependency("B", "1.0") });

        //    var repository = new MockPackageRepository() {
        //        PackageUtility.CreatePackage("B", "2.0", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") }),
        //        PackageUtility.CreatePackage("B", "1.0", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") }, listed: false),
        //        PackageUtility.CreatePackage("B", "1.0.1"),
        //        A10,
        //        PackageUtility.CreatePackage("D", "2.0"),
        //        PackageUtility.CreatePackage("C", "1.1.3", dependencies: new[] { PackageDependency.CreateDependency("D", "1.0") }),
        //        PackageUtility.CreatePackage("C", "1.1.1", dependencies: new[] { PackageDependency.CreateDependency("D", "1.0") }, listed: false),
        //        PackageUtility.CreatePackage("C", "1.5.1", dependencies: new[] { PackageDependency.CreateDependency("D", "1.0") }),
        //        PackageUtility.CreatePackage("B", "1.0.9", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") }),
        //        PackageUtility.CreatePackage("B", "1.1", dependencies: new[] { PackageDependency.CreateDependency("C", "1.1") })
        //    };


        //    IPackageOperationResolver resolver = new EggoInstallWalker(new MockPackageRepository(),
        //        repository,
        //        constraintProvider: null,
        //        logger: NullLogger.Instance,
        //        targetFramework: null,
        //        ignoreDependencies: false,
        //        allowPrereleaseVersions: false,
        //        dependencyVersion: DependencyVersion.HighestPatch);

        //    // Act
        //    var packages = resolver.ResolveOperations(A10).ToDictionary(o => o.Package.Id);

        //    // Assert
        //    Assert.Equal(4, packages.Count);
        //    Assert.Equal(new SemanticVersion("2.0"), packages["D"].Package.Version);
        //    Assert.Equal(new SemanticVersion("1.1.3"), packages["C"].Package.Version);
        //    Assert.Equal(new SemanticVersion("1.0.9"), packages["B"].Package.Version);
        //    Assert.Equal(new SemanticVersion("1.0"), packages["A"].Package.Version);
        //}

        //[Fact]
        //public void ResolveOperationsForPackagesWherePackagesOrderIsDifferentFromItsDependencyOrder()
        //{
        //    // Arrange

        //    // A 1.0 -> B 1.0 to 1.5
        //    // A 2.0 -> B 1.8
        //    // B 1.0
        //    // B 2.0
        //    // C 1.0
        //    // C 2.0

        //    var A10 = PackageUtility.CreatePackage("A", "1.0", dependencies: new[] { PackageDependency.CreateDependency("B", "[1.0, 1.5]") });
        //    var A20 = PackageUtility.CreatePackage("A", "2.0", dependencies: new[] { PackageDependency.CreateDependency("B", "1.8") });
        //    var B10 = PackageUtility.CreatePackage("B", "1.0");
        //    var B20 = PackageUtility.CreatePackage("B", "2.0");
        //    var C10 = PackageUtility.CreatePackage("C", "1.0");
        //    var C20 = PackageUtility.CreatePackage("C", "2.0");

        //    var sourceRepository = new MockPackageRepository() {
        //        A10,                
        //        A20,
        //        B10,
        //        B20,
        //        C10,
        //        C20,
        //    };

        //    var localRepository = new MockPackageRepository() {
        //        A10,
        //        B10,
        //        C10
        //    };

        //    var resolver = new EggoInstallWalker(localRepository,
        //        sourceRepository,
        //        constraintProvider: NullConstraintProvider.Instance,
        //        logger: NullLogger.Instance,
        //        targetFramework: null,
        //        ignoreDependencies: false,
        //        allowPrereleaseVersions: false,
        //        dependencyVersion: DependencyVersion.Lowest);

        //    var updatePackages = new List<IPackage> { A20, B20, C20 };
        //    IList<IPackage> allUpdatePackagesByDependencyOrder;

        //    // Act
        //    var operations = resolver.ResolveOperations(updatePackages, out allUpdatePackagesByDependencyOrder);

        //    // Assert
        //    Assert.True(operations.Count == 3);
        //    Assert.True(operations[0].Package == B20 && operations[0].Action == PackageAction.Install);
        //    Assert.True(operations[1].Package == A20 && operations[1].Action == PackageAction.Install);
        //    Assert.True(operations[2].Package == C20 && operations[2].Action == PackageAction.Install);

        //    Assert.True(allUpdatePackagesByDependencyOrder[0] == B20);
        //    Assert.True(allUpdatePackagesByDependencyOrder[1] == A20);
        //    Assert.True(allUpdatePackagesByDependencyOrder[2] == C20);
        //}

        private JObject CreatePackage(string id, string version, IDictionary<string, string> dependencies = null)
        {
            var package = new JObject();
            package.Add(Properties.PackageId, id);
            package.Add(Properties.Version, version);

            JObject dependencyGroup = null;
            if (dependencies != null && dependencies.Count > 0)
            {
                dependencyGroup = new JObject();
                dependencyGroup.Add(Properties.Type, Types.DependencyGroup);
                dependencyGroup.Add(Properties.TargetFramework, null);
                dependencyGroup.Add(Properties.Dependencies, new JArray(dependencies.Select(kvp =>
                {
                    var dependency = new JObject();
                    dependency.Add(Properties.Type, Types.Dependency);
                    dependency.Add(Properties.PackageId, kvp.Key);
                    dependency.Add(Properties.Range, kvp.Value);
                    return dependency;
                })));

                package.Add(Properties.DependencyGroups, new JArray(new[] { dependencyGroup }));
            }

            return package;
        }

        private class MockSourceRepository : SourceRepository
        {
            private IList<JObject> packages = new List<JObject>();

            public override NuGet.Client.PackageSource Source
            {
                get { throw new NotImplementedException(); }
            }

            public override Task<IEnumerable<JObject>> Search(string searchTerm, SearchFilter filters, int skip, int take, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override Task<JObject> GetPackageMetadata(string id, NuGetVersion version)
            {
                return Task.FromResult(packages.FirstOrDefault(p => p.Value<string>(Properties.PackageId) == id &&
                                                           p.Value<string>(Properties.Version) == version.ToString()));
            }

            public override Task<IEnumerable<JObject>> GetPackageMetadataById(string packageId)
            {
                return Task.FromResult(packages.Where(p => p.Value<string>(Properties.PackageId) == packageId));
            }

            public override void RecordMetric(Client.Resolution.PackageActionType actionType, PackageIdentity packageIdentity, PackageIdentity dependentPackage, bool isUpdate, Client.Installation.InstallationTarget target)
            {
                throw new NotImplementedException();
            }

            internal void AddPackage(JObject package)
            {
                packages.Add(package);
            }
        }

    }
}
