using Newtonsoft.Json.Linq;
using NuGet.Client;
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
        [Fact]
        public void ResolveDependenciesForInstallDiamondDependencyGraph()
        {
            // Arrange
            // A -> [B, C]
            // B -> [D]
            // C -> [D]
            //    A
            //   / \
            //  B   C
            //   \ /
            //    D 

            var sourceRepository = new MockSourceRepository() { 
                CreatePackage("A", "1.0", new Dictionary<string, string>() { { "B", null }, { "C", null } }),
                CreatePackage("B", "1.0", new Dictionary<string, string>() { { "D", null } }),
                CreatePackage("C", "1.0", new Dictionary<string, string>() { { "D", null } }),
                CreatePackage("D", "1.0"),
            };

            var resolver = new Resolver(PackageActionType.Install, new PackageIdentity("A", new NuGetVersion("1.0")), sourceRepository) { DependencyVersion = DependencyBehavior.Lowest };
            resolver.AddOperationTarget(null);
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
            var resolver = new Resolver(PackageActionType.Install, new PackageIdentity("A", new NuGetVersion("1.0")), sourceRepository) { DependencyVersion = DependencyBehavior.Lowest };
            resolver.AddOperationTarget(null);
            var actions = resolver.ResolveActionsAsync().GetAwaiter().GetResult().ToList();
            var actionsById = actions.ToDictionary(p => p.PackageIdentity.Id);

            // Assert
            Assert.Equal(5, actions.Count);

            Assert.Equal("1.0.0", actionsById["A"].PackageIdentity.Version.ToNormalizedString());
            Assert.Equal("1.0.0", actionsById["B"].PackageIdentity.Version.ToNormalizedString());
            Assert.Equal("1.0.0", actionsById["C"].PackageIdentity.Version.ToNormalizedString());
            Assert.Equal("2.0.0", actionsById["D"].PackageIdentity.Version.ToNormalizedString());
            Assert.Equal("2.0.0", actionsById["E"].PackageIdentity.Version.ToNormalizedString());

            //Verify that D & E are first (order doesn't matter), then B & C (order doesn't matter), then A
            Assert.True(actions.Take(2).Select(a => a.PackageIdentity.Id).All(id => id == "D" || id == "E"));
            Assert.True(actions.Skip(2).Take(2).Select(a => a.PackageIdentity.Id).All(id => id == "B" || id == "C"));
            Assert.Equal("A", actions[4].PackageIdentity.Id);
        }

        // Tests that when there is a local package that can satisfy all dependencies, it is preferred over other packages.
        [Fact]
        public void ResolveActionsPreferInstalledPackages()
        {
            // Arrange

            // Local:
            // B 1.0
            // C 1.0

            // Remote
            // A 1.0 -> B 1.0, C 1.0
            // B 1.0
            // B 1.1
            // C 1.0
            // C 2.0

            // Expect: Install A 1.0 (no change to B or C)
            var sourceRepository = new MockSourceRepository() {
                CreatePackage("A", "1.0", new Dictionary<string, string>() { { "B", "1.0" }, { "C", "1.0" } }),
                CreatePackage("B", "1.0"),
                CreatePackage("B", "1.1"),
                CreatePackage("C", "1.0"),
                CreatePackage("C", "2.0"),
            };

            // Act
            var resolver = new Resolver(PackageActionType.Install, new PackageIdentity("A", new NuGetVersion("1.0")), sourceRepository) { DependencyVersion = DependencyBehavior.HighestMinor };
            var project = new MockProject();

            ((MockInstalledPackagesList)project.InstalledPackages).AddPackages(new[] {
                CreatePackage("B", "1.0"),
                CreatePackage("C", "1.0"),
            });
            resolver.AddOperationTarget(project);
            var packages = resolver.ResolveActionsAsync().GetAwaiter().GetResult().ToDictionary(p => p.PackageIdentity.Id);

            // Assert
            Assert.Equal(1, packages.Count);
            Assert.Equal("1.0.0", packages["A"].PackageIdentity.Version.ToNormalizedString());
        }



        //[Fact]
        //public void UninstallIgnoresMissingDependencies()
        //{
        //    // Arrange
        //    // A -> [B, C]
        //    // B -> [D]
        //    // C -> [D]

        //    var project = new MockProject();
        //    ((MockInstalledPackagesList)project.InstalledPackages).IsInstalledImpl = new Func<string, NuGetVersion, bool>((id, version) =>
        //    {
        //        return (id == "A" && version.ToNormalizedString() == "1.0.0") ||
        //               (id == "C" && version.ToNormalizedString() == "1.0.0") ||
        //               (id == "D" && version.ToNormalizedString() == "1.0.0");
        //    });

        //    // Act
        //    var resolver = new Resolver(PackageActionType.Uninstall, new PackageIdentity("A", new NuGetVersion("1.0")), null)
        //    {
        //        DependencyVersion = DependencyBehavior.HighestMinor,
        //        IgnoreDependencies = false,
        //    };
        //    resolver.AddOperationTarget(project);
        //    var actions = resolver.ResolveActionsAsync().GetAwaiter().GetResult().ToDictionary(p => p.PackageIdentity.Id);

        //    // Assert
        //    Assert.Equal(3, actions.Count);
        //    Assert.NotNull(actions["A"]);
        //    Assert.NotNull(actions["C"]);
        //    Assert.NotNull(actions["D"]);
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

        [Fact]
        public void ResolveActionsForInstallCircularReferenceThrows()
        {
            // Arrange
            var sourceRepository = new MockSourceRepository()
            {
                CreatePackage("A", "1.0", new Dictionary<string, string> { { "B", null } } ),
                CreatePackage("B", "1.0", new Dictionary<string, string> { { "A", null } } ),
            };

            // Act & Assert
            var resolver = new Resolver(PackageActionType.Install, new PackageIdentity("A", new NuGetVersion("1.0")), sourceRepository) { DependencyVersion = DependencyBehavior.Lowest };
            resolver.AddOperationTarget(null);
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveActionsAsync().GetAwaiter().GetResult(), "Circular dependency detected 'A 1.0 => B 1.0 => A'.");
        }


        [Fact]
        public void ResolveActionsForInstallCircularReferenceWithDifferentVersionOfPackageReferenceThrows()
        {
            // Arrange
            var sourceRepository = new MockSourceRepository() 
            {
                CreatePackage("A", "1.0", new Dictionary<string, string> { { "B", null } } ),
                CreatePackage("A", "1.5", new Dictionary<string, string> { { "B", null } } ),
                CreatePackage("B", "1.0", new Dictionary<string, string> { { "A", "[1.5]" } } ),
            };

            // Act & Assert
            var resolver = new Resolver(PackageActionType.Install, new PackageIdentity("A", new NuGetVersion("1.0")), sourceRepository) { DependencyVersion = DependencyBehavior.Lowest };
            resolver.AddOperationTarget(null);
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveActionsAsync().GetAwaiter().GetResult(), "Circular dependency detected 'A 1.0 => B 1.0 => A [1.5]'.");
        }

        [Fact]
        public void ResolveActionsForSimpleUpdate()
        {
            // Arrange
            // Installed: A, B
            // A 1.0 -> B [1.0]
            var project = new MockProject()
            {
                CreatePackage("A", "1.0", new Dictionary<string, string> { { "B", "1.0" } } ),
                CreatePackage("B", "1.0"),
            };

            var sourceRepository = new MockSourceRepository()
            {
                CreatePackage("A", "1.0", new Dictionary<string, string> { { "B", "1.0" } } ),
                CreatePackage("A", "2.0", new Dictionary<string, string> { { "B", "1.0" } } ),
                CreatePackage("B", "1.0"),
            };

            // Act
            var resolver = new Resolver(PackageActionType.Install, new PackageIdentity("A", new NuGetVersion("2.0")), sourceRepository);
            resolver.DependencyVersion = DependencyBehavior.HighestPatch;
            resolver.AddOperationTarget(project);
            var actions = resolver.ResolveActionsAsync().GetAwaiter().GetResult().ToList();

            // Assert
            Assert.Equal(2, actions.Count);
            AssertOperation("A", "1.0.0", PackageActionType.Uninstall, actions[0]);
            AssertOperation("A", "2.0.0", PackageActionType.Install, actions[1]);

        }

        //// A 1.0 -> B [1.0]
        //IPackage A10 = PackageUtility.CreatePackage("A", "1.0",
        //                                                dependencies: new List<PackageDependency> {
        //                                                        PackageDependency.CreateDependency("B", "[1.0]")
        //                                                    }, content: new[] { "a1" });

        //// A 2.0 -> B (any version)
        //IPackage A20 = PackageUtility.CreatePackage("A", "2.0",
        //                                                dependencies: new List<PackageDependency> {
        //                                                        new PackageDependency("B")
        //                                                    }, content: new[] { "a2" });

        //IPackage B10 = PackageUtility.CreatePackage("B", "1.0", content: new[] { "b1" });
        //IPackage B101 = PackageUtility.CreatePackage("B", "1.0.1", content: new[] { "b101" });
        //IPackage B20 = PackageUtility.CreatePackage("B", "2.0", content: new[] { "a2" });
        //localRepository.Add(A10);
        //localRepository.Add(B10);
        //sourceRepository.AddPackage(A10);
        //sourceRepository.AddPackage(A20);
        //sourceRepository.AddPackage(B10);
        //sourceRepository.AddPackage(B101);
        //sourceRepository.AddPackage(B20);

        //IPackageOperationResolver resolver = new UpdateWalker(localRepository,
        //                                                      sourceRepository,
        //                                                      new DependentsWalker(localRepository),
        //                                                      NullConstraintProvider.Instance,
        //                                                      NullLogger.Instance,
        //                                                      updateDependencies: true,
        //                                                      allowPrereleaseVersions: false) { AcceptedTargets = PackageTargets.Project };

        //// Act
        //var packages = resolver.ResolveOperations(B101).ToList();

        //// Assert
        //Assert.Equal(2, packages.Count);

        [Fact(Skip="Scenario not coded yet. Next up to get working...")]
        public void ResolveActionsForUpdateWherePeerConsumersRequireUpdate()
        {
            // Arrange
            // Installed: A 1.0, B 1.0, C 1.0
            // A 1.0 -> B [1.0]
            // C 1.0 -> B [1.0]
            //
            // Install A 2.0 -> B [2.0]
            //
            // Expected A 2.0, B 2.0, C 2.0
            var project = new MockProject()
            {
                CreatePackage("A", "1.0", new Dictionary<string, string> { { "B", "[1.0]" } } ),
                CreatePackage("B", "1.0"),
                CreatePackage("C", "1.0", new Dictionary<string, string> { { "B", "[1.0]" } } ),
            };

            var sourceRepository = new MockSourceRepository()
            {
                CreatePackage("A", "1.0", new Dictionary<string, string> { { "B", "[1.0]" } } ),
                CreatePackage("A", "2.0", new Dictionary<string, string> { { "B", "[2.0]" } } ),
                CreatePackage("B", "1.0"),
                CreatePackage("B", "2.0"),
                CreatePackage("C", "1.0", new Dictionary<string, string> { { "B", "[1.0]" } } ),
                CreatePackage("C", "2.0", new Dictionary<string, string> { { "B", "[2.0]" } } ),
            };

            // Act
            var resolver = new Resolver(PackageActionType.Install, new PackageIdentity("A", new NuGetVersion("2.0")), sourceRepository);
            resolver.DependencyVersion = DependencyBehavior.HighestPatch;
            resolver.AddOperationTarget(project);
            var actions = resolver.ResolveActionsAsync().GetAwaiter().GetResult().ToList();

            // Assert
            Assert.Equal(6, actions.Count);
            AssertOperation("A", "1.0.0", PackageActionType.Uninstall, actions[0]);
            AssertOperation("C", "1.0.0", PackageActionType.Uninstall, actions[1]);
            AssertOperation("B", "1.0.0", PackageActionType.Uninstall, actions[2]);
            AssertOperation("B", "2.0.0", PackageActionType.Install, actions[3]);
            AssertOperation("A", "2.0.0", PackageActionType.Install, actions[4]);
            AssertOperation("C", "2.0.0", PackageActionType.Install, actions[5]);
        }

        private void AssertOperation(string expectedId, string expectedVersion, PackageActionType expectedPackageAction, Client.Resolution.PackageAction actualAction)
        {
            Assert.Equal(expectedId, actualAction.PackageIdentity.Id);
            Assert.Equal(expectedVersion, actualAction.PackageIdentity.Version.ToNormalizedString());
            Assert.Equal(expectedPackageAction, actualAction.ActionType);
        }

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
            var sourceRepository = new MockSourceRepository()
            {
                CreatePackage("A", "1.0", new Dictionary<string, string>() { { "B", "1.5" } }),
                CreatePackage("B", "1.4")
            };

            // Act & Assert
            var resolver = new Resolver(PackageActionType.Install, new PackageIdentity("A", new NuGetVersion("1.0")), sourceRepository) { DependencyVersion = DependencyBehavior.Lowest };
            resolver.AddOperationTarget(null);
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveActionsAsync().GetAwaiter().GetResult());
        }

        [Fact]
        public void ResolveDependencyForInstallPackageWithDependencyThatDoesntMeetExactVersionThrows()
        {
            // Arrange
            var sourceRepository = new MockSourceRepository()
            {
                CreatePackage("A", "1.0", new Dictionary<string, string>() { { "B", "[1.3]" } }),
                CreatePackage("B", "1.4"),
            };

            // Act & Assert
            var resolver = new Resolver(PackageActionType.Install, new PackageIdentity("A", new NuGetVersion("1.0")), sourceRepository) { DependencyVersion = DependencyBehavior.Lowest };
            resolver.AddOperationTarget(null);
            ExceptionAssert.Throws<InvalidOperationException>(() => resolver.ResolveActionsAsync().GetAwaiter().GetResult());
        }

        [Fact]
        public void ResolveOperationsForInstallSameDependencyAtDifferentLevelsInGraph()
        {
            // Arrange
            var sourceRepository = new MockSourceRepository()
            {
                CreatePackage("A", "1.0", new Dictionary<string, string>() { { "B", "1.0" }, { "C", "1.0" } }),
                CreatePackage("B", "1.0"),
                CreatePackage("C", "1.0", new Dictionary<string, string>() { { "B", "1.0" }, { "D", "1.0" } }),
                CreatePackage("D", "1.0", new Dictionary<string, string>() { { "B", "1.0" } })
            };

            // Act
            var resolver = new Resolver(PackageActionType.Install, new PackageIdentity("A", new NuGetVersion("1.0")), sourceRepository) { DependencyVersion = DependencyBehavior.Lowest };
            resolver.AddOperationTarget(null);
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

        // Tests that when DependencyVersion is HighestMinor, the dependency with the lowest major highest minor and highest patch version
        // is picked.
        [Fact]
        public void ResolvesLowestMajorHighestMinorHighestPatchVersionOfListedPackagesForDependencies()
        {
            // Arrange

            // A 1.0 -> B 1.0
            // B 1.0 -> C 1.1
            // C 1.1 -> D 1.0

            var sourceRepository = new MockSourceRepository() {
                CreatePackage("A", "1.0", new Dictionary<string, string>() { { "B", "1.0" } }),
                CreatePackage("B", "2.0", new Dictionary<string, string>() { { "C", "1.1" } }),
                CreatePackage("B", "1.0", new Dictionary<string, string>() { { "C", "1.1" } }),
                CreatePackage("B", "1.0.1"),
                CreatePackage("D", "2.0"),
                CreatePackage("C", "1.1.3", new Dictionary<string, string>() { { "D", "1.0" } }),
                CreatePackage("C", "1.1.1", new Dictionary<string, string>() { { "D", "1.0" } }),
                CreatePackage("C", "1.5.1", new Dictionary<string, string>() { { "D", "1.0" } }),
                CreatePackage("B", "1.0.9", new Dictionary<string, string>() { { "C", "1.1" } }),
                CreatePackage("B", "1.1", new Dictionary<string, string>() { { "C", "1.1" } })
            };

            // Act
            var resolver = new Resolver(PackageActionType.Install, new PackageIdentity("A", new NuGetVersion("1.0")), sourceRepository) { DependencyVersion = DependencyBehavior.HighestMinor };
            resolver.AddOperationTarget(null);
            var packages = resolver.ResolveActionsAsync().GetAwaiter().GetResult().ToDictionary(p => p.PackageIdentity.Id);

            // Assert
            Assert.Equal(4, packages.Count);
            Assert.Equal("2.0.0", packages["D"].PackageIdentity.Version.ToNormalizedString());
            Assert.Equal("1.5.1", packages["C"].PackageIdentity.Version.ToNormalizedString());
            Assert.Equal("1.1.0", packages["B"].PackageIdentity.Version.ToNormalizedString());
            Assert.Equal("1.0.0", packages["A"].PackageIdentity.Version.ToNormalizedString());
        }


        // Tests that when DependencyVersion is Lowest, the dependency with the lowest major minor and highest patch version
        // is picked.
        [Fact]
        public void ResolvesLowestMajorAndMinorAndPatchVersionOfListedPackagesForDependencies()
        {
            // Arrange

            // A 1.0 -> B 1.0
            // B 1.0 -> C 1.1
            // C 1.1 -> D 1.0

            var sourceRepository = new MockSourceRepository() {
                CreatePackage("A", "1.0", new Dictionary<string, string>() { { "B", "1.0" } }),
                CreatePackage("B", "2.0", new Dictionary<string, string>() { { "C", "1.1" } }),
                CreatePackage("B", "1.0.1"),
                CreatePackage("D", "2.0"),
                CreatePackage("C", "1.1.3", new Dictionary<string, string>() { { "D", "1.0" } }),
                CreatePackage("C", "1.1.1", new Dictionary<string, string>() { { "D", "1.0" } }),
                CreatePackage("C", "1.5.1", new Dictionary<string, string>() { { "D", "1.0" } }),
                CreatePackage("B", "1.0.9", new Dictionary<string, string>() { { "C", "1.1" } }),
                CreatePackage("B", "1.1", new Dictionary<string, string>() { { "C", "1.1" } })
            };

            // Act
            var resolver = new Resolver(PackageActionType.Install, new PackageIdentity("A", new NuGetVersion("1.0")), sourceRepository) { DependencyVersion = DependencyBehavior.Lowest };
            resolver.AddOperationTarget(null);
            var packages = resolver.ResolveActionsAsync().GetAwaiter().GetResult().ToDictionary(p => p.PackageIdentity.Id);

            // Assert
            Assert.Equal(2, packages.Count);
            Assert.Equal("1.0.1", packages["B"].PackageIdentity.Version.ToNormalizedString());
            Assert.Equal("1.0.0", packages["A"].PackageIdentity.Version.ToNormalizedString());
        }

        // Tests that when DependencyVersion is HighestPatch, the dependency with the lowest major minor and highest patch version
        // is picked.
        [Fact]
        public void ResolvesLowestMajorAndMinorHighestPatchVersionOfListedPackagesForDependencies()
        {
            // Arrange

            // A 1.0 -> B 1.0
            // B 1.0 -> C 1.1
            // C 1.1 -> D 1.0
            var sourceRepository = new MockSourceRepository()
            {
                CreatePackage("A", "1.0", new Dictionary<string, string>() { { "B", "1.0" } }),
                CreatePackage("B", "2.0", new Dictionary<string, string>() { { "C", "1.1" } }),
                CreatePackage("B", "1.0", new Dictionary<string, string>() { { "C", "1.1" } }),
                CreatePackage("B", "1.0.1"),
                CreatePackage("D", "2.0"),
                CreatePackage("C", "1.1.3", new Dictionary<string, string>() { { "D", "1.0" } }),
                CreatePackage("C", "1.1.1", new Dictionary<string, string>() { { "D", "1.0" } }),
                CreatePackage("C", "1.5.1", new Dictionary<string, string>() { { "D", "1.0" } }),
                CreatePackage("B", "1.0.9", new Dictionary<string, string>() { { "C", "1.1" } }),
                CreatePackage("B", "1.1", new Dictionary<string, string>() { { "C", "1.1" } })
            };

            // Act
            var resolver = new Resolver(PackageActionType.Install, new PackageIdentity("A", new NuGetVersion("1.0")), sourceRepository) { DependencyVersion = DependencyBehavior.HighestPatch };
            resolver.AddOperationTarget(null);
            var packages = resolver.ResolveActionsAsync().GetAwaiter().GetResult().ToDictionary(p => p.PackageIdentity.Id);

            // Assert
            Assert.Equal(4, packages.Count);
            Assert.Equal("2.0.0", packages["D"].PackageIdentity.Version.ToNormalizedString());
            Assert.Equal("1.1.3", packages["C"].PackageIdentity.Version.ToNormalizedString());
            Assert.Equal("1.0.9", packages["B"].PackageIdentity.Version.ToNormalizedString());
            Assert.Equal("1.0.0", packages["A"].PackageIdentity.Version.ToNormalizedString());
        }

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

        private class MockSourceRepository : SourceRepository, ICollection<JObject>
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

            public void Add(JObject item)
            {
                AddPackage(item);
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(JObject item)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(JObject[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public int Count
            {
                get { throw new NotImplementedException(); }
            }

            public bool IsReadOnly
            {
                get { throw new NotImplementedException(); }
            }

            public bool Remove(JObject item)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<JObject> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        private class MockInstalledPackagesList : InstalledPackagesList
        {
            private Dictionary<string, JObject> packagesById;

            public MockInstalledPackagesList()
            {
                this.packagesById = new Dictionary<string, JObject>();
            }

            public override Task<IEnumerable<JObject>> GetAllInstalledPackagesAndMetadata()
            {
                return Task.FromResult(packagesById.Values.AsEnumerable());
            }

            public override Task<IEnumerable<JObject>> Search(SourceRepository source, string searchTerm, int skip, int take, CancellationToken cancelToken)
            {
                throw new NotImplementedException();
            }

            public override IEnumerable<InstalledPackageReference> GetInstalledPackages()
            {
                throw new NotImplementedException();
            }

            public override InstalledPackageReference GetInstalledPackage(string packageId)
            {
                throw new NotImplementedException();
            }

            public override bool IsInstalled(string packageId, NuGetVersion packageVersion)
            {
                return IsInstalled(packageId) && packagesById[packageId].GetVersion().Equals(packageVersion);
            }

            public override bool IsInstalled(string packageId)
            {
                return packagesById.ContainsKey(packageId);
            }

            internal void AddPackage(JObject package)
            {
                packagesById.Add(package.GetId(), package);
            }

            internal void AddPackages(IEnumerable<JObject> packages)
            {
                foreach (var package in packages)
                {
                    AddPackage(package);
                }
            }
        }

        private class MockProject : Project, ICollection<JObject>
        {
            private InstalledPackagesList installedPackages;

            public MockProject()
            {
                this.installedPackages = new MockInstalledPackagesList();
            }

            public override bool Equals(Project other)
            {
                throw new NotImplementedException();
            }

            public override string Name
            {
                get { throw new NotImplementedException(); }
            }

            public override bool IsAvailable
            {
                get { throw new NotImplementedException(); }
            }

            public override InstalledPackagesList InstalledPackages
            {
                get { return this.installedPackages; }
            }

            public override Solution OwnerSolution
            {
                get { throw new NotImplementedException(); }
            }

            public override IEnumerable<System.Runtime.Versioning.FrameworkName> GetSupportedFrameworks()
            {
                throw new NotImplementedException();
            }

            public override Task<IEnumerable<JObject>> SearchInstalled(SourceRepository source, string searchText, int skip, int take, CancellationToken cancelToken)
            {
                throw new NotImplementedException();
            }

            public void Add(JObject item)
            {
                ((MockInstalledPackagesList)this.InstalledPackages).AddPackage(item);
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(JObject item)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(JObject[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public int Count
            {
                get { throw new NotImplementedException(); }
            }

            public bool IsReadOnly
            {
                get { throw new NotImplementedException(); }
            }

            public bool Remove(JObject item)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<JObject> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }
    }
}
