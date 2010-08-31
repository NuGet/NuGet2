namespace NuPack.Test {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Versioning;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NuPack.Test.Mocks;
    using Moq;

    [TestClass]
    public class ProjectManagerTest {
        [TestMethod]
        public void AddingPackageReferenceNullOrEmptyPackageIdThrows() {
            // Arrange
            ProjectManager projectManager = CreateProjectManager();

            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => projectManager.AddPackageReference((string)null), "packageId");
            ExceptionAssert.ThrowsArgNullOrEmpty(() => projectManager.AddPackageReference(String.Empty), "packageId");
        }

        [TestMethod]
        public void AddingUnknownPackageReferenceThrows() {
            // Arrange
            ProjectManager projectManager = CreateProjectManager();

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.AddPackageReference("unknown"), "Unable to find package 'unknown'");
        }

        [TestMethod]
        public void AddPackageReferenceWhenNewVersionOfPackageAlreadyReferencedThrows() {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(sourceRepository, PackageUtility.CreateAssemblyResolver(), new MockProjectSystem());
            Package packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                    PackageDependency.CreateDependency("B")
                                                                });
            Package packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                    PackageDependency.CreateDependency("B")
                                                                });
            Package packageB10 = PackageUtility.CreatePackage("B", "1.0");
            projectManager.LocalRepository.AddPackage(packageA20);
            projectManager.LocalRepository.AddPackage(packageB10);

            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.AddPackageReference("A", Version.Parse("1.0")), @"C:\MockFileSystem\ is already referencing a newer version of 'A'");
        }

        [TestMethod]
        public void RemovingUnknownPackageReferenceThrows() {
            // Arrange
            var projectManager = CreateProjectManager();

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.RemovePackageReference("foo"), "Unable to find package 'foo'");
        }

        [TestMethod]
        public void RemovingPackageReferenceWithOtherProjectWithReferencesThatWereNotCopiedToProject() {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(sourceRepository, PackageUtility.CreateAssemblyResolver(), new MockProjectSystem());
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "1.0",
                                                        files: null,
                                                        assemblyReferences: new[] { PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName("SP", new Version("40.0"))) },
                                                        tools: null,
                                                        dependencies: null);
            projectManager.LocalRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageA);
            projectManager.LocalRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageB);

            // Act
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.IsFalse(projectManager.LocalRepository.IsPackageInstalled(packageA));
        }

        [TestMethod]
        public void RemovingUnknownPackageReferenceNullOrEmptyPackageIdThrows() {
            // Arrange
            var projectManager = CreateProjectManager();

            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => projectManager.RemovePackageReference((string)null), "packageId");
            ExceptionAssert.ThrowsArgNullOrEmpty(() => projectManager.RemovePackageReference(String.Empty), "packageId");
        }

        [TestMethod]
        public void RemovingPackageReferenceWithNoDependents() {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(sourceRepository, PackageUtility.CreateAssemblyResolver(), new MockProjectSystem());
            var package = PackageUtility.CreatePackage("foo", "1.2.33");
            projectManager.LocalRepository.AddPackage(package);
            sourceRepository.AddPackage(package);

            // Act
            projectManager.RemovePackageReference("foo");

            // Assert
            Assert.IsFalse(projectManager.LocalRepository.IsPackageInstalled(package));
        }

        [TestMethod]
        public void AddPackageReferenceAddsContentAndReferencesProjectSystem() {
            // Arrange
            MockProjectSystem projectSystem = new MockProjectSystem();
            MockPackageRepository mockRepository = new MockPackageRepository();
            ProjectManager projectManager = new ProjectManager(mockRepository, PackageUtility.CreateAssemblyResolver(), projectSystem);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        new[] { "content" },
                                                        new[] { "reference.dll" },
                                                        new[] { "tool" });

            mockRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.AreEqual(2, projectSystem.Paths.Count);
            Assert.AreEqual(1, projectSystem.References.Count);
            Assert.IsTrue(projectSystem.References.Contains(@"FullPath\reference.dll"));
            Assert.IsTrue(projectSystem.FileExists(@"content"));
            Assert.IsTrue(projectSystem.FileExists(@"packages.xml"));
        }

        [TestMethod]
        public void RemovePackageReferenceExcludesFileIfAnotherPackageUsesThem() {
            // Arrange
            MockProjectSystem mockProjectSystem = new MockProjectSystem();
            MockPackageRepository mockRepository = new MockPackageRepository();

            ProjectManager projectManager = new ProjectManager(mockRepository, PackageUtility.CreateAssemblyResolver(), mockProjectSystem);
            Package packageA = PackageUtility.CreatePackage("A", "1.0",
                                                             new[] { "fileA", "commonFile" },
                                                             new[] { "referenceA.dll", "commonReference.dll" });

            Package packageB = PackageUtility.CreatePackage("B", "1.0",
                                                            new[] { "fileB", "commonFile" },
                                                            new[] { "referenceB.dll", "commonReference.dll" });

            mockRepository.AddPackage(packageA);
            mockRepository.AddPackage(packageB);

            projectManager.AddPackageReference("A");
            projectManager.AddPackageReference("B");

            // Act
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.IsTrue(mockProjectSystem.Deleted.Contains("fileA"));
            Assert.IsTrue(mockProjectSystem.Deleted.Contains("referenceA.dll"));
            Assert.IsTrue(mockProjectSystem.FileExists("commonFile"));
            Assert.IsTrue(mockProjectSystem.References.Contains(@"FullPath\commonReference.dll"));
        }

        [TestMethod]
        public void AddPackageReferenceWhenOlderVersionOfPackageInstalledDoesAnUpgrade() {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(sourceRepository, PackageUtility.CreateAssemblyResolver(), new MockProjectSystem());
            Package packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                    PackageDependency.CreateDependency("B", version:new Version("1.0"))
                                                                });
            Package packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                    PackageDependency.CreateDependency("B", version:new Version("2.0"))
                                                                });
            Package packageB10 = PackageUtility.CreatePackage("B", "1.0");
            Package packageB20 = PackageUtility.CreatePackage("B", "2.0");

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);

            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageB20);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.IsFalse(projectManager.LocalRepository.IsPackageInstalled(packageA10));
            Assert.IsFalse(projectManager.LocalRepository.IsPackageInstalled(packageB10));
            Assert.IsTrue(projectManager.LocalRepository.IsPackageInstalled(packageA20));
            Assert.IsTrue(projectManager.LocalRepository.IsPackageInstalled(packageB20));
        }

        [TestMethod]
        public void UpdatePackageNullOrEmptyPackageIdThrows() {
            // Arrange
            ProjectManager packageManager = CreateProjectManager();

            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => packageManager.UpdatePackageReference(null), "packageId");
            ExceptionAssert.ThrowsArgNullOrEmpty(() => packageManager.UpdatePackageReference(String.Empty), "packageId");
        }

        [TestMethod]
        public void UpdatePackageReferenceWithMixedDependenciesUpdatesPackageAndDependenciesIfUnused() {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(sourceRepository, PackageUtility.CreateAssemblyResolver(), new MockProjectSystem());

            // A 1.0 -> [B 1.0, C 1.0]
            Package packageA10 = PackageUtility.CreatePackage("A",
                                                               "1.0",
                                                               dependencies: new List<PackageDependency> { 
                                                                    PackageDependency.CreateDependency("B", version: Version.Parse( "1.0")),
                                                                    PackageDependency.CreateDependency("C", version: Version.Parse( "1.0"))
                                                                });

            Package packageB10 = PackageUtility.CreatePackage("B", "1.0");
            Package packageC10 = PackageUtility.CreatePackage("C", "1.0");

            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);
            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            projectManager.LocalRepository.AddPackage(packageC10);

            Package packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                               dependencies: new List<PackageDependency> { 
                                                                                    PackageDependency.CreateDependency("B", version: Version.Parse( "1.0")),
                                                                                    PackageDependency.CreateDependency("C", version: Version.Parse( "2.0")),
                                                                                    PackageDependency.CreateDependency("D", version: Version.Parse( "1.0"))
                                                               });

            Package packageC20 = PackageUtility.CreatePackage("C", "2.0");
            Package packageD10 = PackageUtility.CreatePackage("D", "1.0");

            // A 2.0 -> [B 1.0, C 2.0, D 1.0]
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageC20);
            sourceRepository.AddPackage(packageD10);

            // Act
            projectManager.UpdatePackageReference("A");

            // Assert
            Assert.IsTrue(projectManager.LocalRepository.IsPackageInstalled(packageA20));
            Assert.IsTrue(projectManager.LocalRepository.IsPackageInstalled(packageB10));
            Assert.IsTrue(projectManager.LocalRepository.IsPackageInstalled(packageC20));
            Assert.IsTrue(projectManager.LocalRepository.IsPackageInstalled(packageD10));
            Assert.IsFalse(projectManager.LocalRepository.IsPackageInstalled(packageA10));
            Assert.IsFalse(projectManager.LocalRepository.IsPackageInstalled(packageC10));
        }

        [TestMethod]
        public void UpdatePackageReferenceIfPackageNotReferencedThrows() {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(sourceRepository, PackageUtility.CreateAssemblyResolver(), new MockProjectSystem());

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.UpdatePackageReference("A"), @"C:\MockFileSystem\ does not reference 'A'.");
        }

        [TestMethod]
        public void UpdatePackageReferenceToOlderVersionThrows() {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(sourceRepository, PackageUtility.CreateAssemblyResolver(), new MockProjectSystem());

            // A 1.0 -> [B 1.0]
            Package packageA10 = PackageUtility.CreatePackage("A", "1.0");
            Package packageA20 = PackageUtility.CreatePackage("A", "2.0");
            Package packageA30 = PackageUtility.CreatePackage("A", "3.0");

            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageA30);

            projectManager.LocalRepository.AddPackage(packageA20);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.UpdatePackageReference("A", version: Version.Parse("1.0")), @"C:\MockFileSystem\ is already referencing a newer version of 'A'");
        }

        [TestMethod]
        public void UpdatePackageReferenceWithUnresolvedDependencyThrows() {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(sourceRepository, PackageUtility.CreateAssemblyResolver(), new MockProjectSystem());

            // A 1.0 -> [B 1.0]
            Package packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                               dependencies: new List<PackageDependency> { 
                                                                   PackageDependency.CreateDependency("B", version: Version.Parse( "1.0")),
                                                               });

            Package packageB10 = PackageUtility.CreatePackage("B", "1.0");

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);

            // A 2.0 -> [B 2.0]
            Package packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                    PackageDependency.CreateDependency("B", version: Version.Parse( "2.0"))
                                                            });

            sourceRepository.AddPackage(packageA20);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.UpdatePackageReference("A"), "Unable to resolve dependency 'B (= 2.0)'");
        }

        [TestMethod]
        public void UpdatePackageReferenceWithUpdateDependenciesSetToFalseIgnoresDependencies() {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(sourceRepository, PackageUtility.CreateAssemblyResolver(), new MockProjectSystem());

            // A 1.0 -> [B 1.0]
            Package packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                               dependencies: new List<PackageDependency> { 
                                                                   PackageDependency.CreateDependency("B", version: Version.Parse( "1.0")),
                                                               });


            Package packageB10 = PackageUtility.CreatePackage("B", "1.0");

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);

            // A 2.0 -> [B 2.0]
            Package packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                    PackageDependency.CreateDependency("B", version: Version.Parse( "2.0")),
                                                                });

            Package packageB20 = PackageUtility.CreatePackage("B", "2.0");

            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB20);

            // Act
            projectManager.UpdatePackageReference("A", updateDependencies: false);

            // Assert
            Assert.IsTrue(projectManager.LocalRepository.IsPackageInstalled(packageA20));
            Assert.IsFalse(projectManager.LocalRepository.IsPackageInstalled(packageA10));
            Assert.IsTrue(projectManager.LocalRepository.IsPackageInstalled(packageB10));
            Assert.IsFalse(projectManager.LocalRepository.IsPackageInstalled(packageB20));
        }

        [TestMethod]
        public void UpdateDependencyDependentsHaveSatisfyableDependencies() {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(sourceRepository, PackageUtility.CreateAssemblyResolver(), new MockProjectSystem());

            // A 1.0 -> [C >= 1.0]
            Package packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> {                                                                         
                                                                        PackageDependency.CreateDependency("C", minVersion: Version.Parse( "1.0"))
                                                                    });

            // B 1.0 -> [C <= 2.0]
            Package packageB10 = PackageUtility.CreatePackage("B", "1.0",
                                                                dependencies: new List<PackageDependency> {                                                                         
                                                                        PackageDependency.CreateDependency("C", maxVersion: Version.Parse("2.0"))
                                                                    });

            Package packageC10 = PackageUtility.CreatePackage("C", "1.0");

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            projectManager.LocalRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);

            Package packageC20 = PackageUtility.CreatePackage("C", "2.0");

            // A 2.0 -> [B 1.0, C 2.0, D 1.0]
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageC20);

            // Act
            projectManager.UpdatePackageReference("C");

            // Assert
            Assert.IsTrue(projectManager.LocalRepository.IsPackageInstalled(packageA10));
            Assert.IsTrue(projectManager.LocalRepository.IsPackageInstalled(packageB10));
            Assert.IsTrue(projectManager.LocalRepository.IsPackageInstalled(packageC20));
            Assert.IsFalse(projectManager.LocalRepository.IsPackageInstalled(packageC10));
        }

        [TestMethod]
        public void UpdatePackageReferenceWithSatisfyableDependencies() {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(sourceRepository, PackageUtility.CreateAssemblyResolver(), new MockProjectSystem());

            // A 1.0 -> [B 1.0, C 1.0]
            Package packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", version: Version.Parse( "1.0")),
                                                                        PackageDependency.CreateDependency("C", version: Version.Parse( "1.0"))
                                                                    });

            Package packageB10 = PackageUtility.CreatePackage("B", "1.0");
            Package packageC10 = PackageUtility.CreatePackage("C", "1.0");

            // G 1.0 -> [C (>= 1.0)]
            Package packageG10 = PackageUtility.CreatePackage("G", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("C", minVersion: Version.Parse("1.0"))
                                                                    });

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            projectManager.LocalRepository.AddPackage(packageC10);
            projectManager.LocalRepository.AddPackage(packageG10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageG10);

            Package packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", version: Version.Parse( "1.0")),
                                                                        PackageDependency.CreateDependency("C", version: Version.Parse( "2.0")),
                                                                        PackageDependency.CreateDependency("D", version: Version.Parse( "1.0"))
                                                                    });

            Package packageC20 = PackageUtility.CreatePackage("C", "2.0");
            Package packageD10 = PackageUtility.CreatePackage("D", "1.0");

            // A 2.0 -> [B 1.0, C 2.0, D 1.0]
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageC20);
            sourceRepository.AddPackage(packageD10);

            // Act
            projectManager.UpdatePackageReference("A");

            // Assert
            Assert.IsTrue(projectManager.LocalRepository.IsPackageInstalled(packageA20));
            Assert.IsTrue(projectManager.LocalRepository.IsPackageInstalled(packageB10));
            Assert.IsTrue(projectManager.LocalRepository.IsPackageInstalled(packageC20));
            Assert.IsTrue(projectManager.LocalRepository.IsPackageInstalled(packageD10));
            Assert.IsTrue(projectManager.LocalRepository.IsPackageInstalled(packageG10));

            Assert.IsFalse(projectManager.LocalRepository.IsPackageInstalled(packageC10));
            Assert.IsFalse(projectManager.LocalRepository.IsPackageInstalled(packageA10));
        }

        [TestMethod]
        public void UpdatePackageReferenceWithDependenciesInUseThrowsConflictError() {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(sourceRepository, PackageUtility.CreateAssemblyResolver(), new MockProjectSystem());

            // A 1.0 -> [B 1.0, C 1.0]
            Package packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", version: Version.Parse( "1.0")),
                                                                        PackageDependency.CreateDependency("C", version: Version.Parse( "1.0"))
                                                                    });

            Package packageB10 = PackageUtility.CreatePackage("B", "1.0");
            Package packageC10 = PackageUtility.CreatePackage("C", "1.0");

            // G 1.0 -> [C 1.0]
            Package packageG10 = PackageUtility.CreatePackage("G", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("C", version: Version.Parse("1.0"))
                                                                    });

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            projectManager.LocalRepository.AddPackage(packageC10);
            projectManager.LocalRepository.AddPackage(packageG10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageG10);

            Package packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", version: Version.Parse( "1.0")),
                                                                        PackageDependency.CreateDependency("C", version: Version.Parse( "2.0")),
                                                                        PackageDependency.CreateDependency("D", version: Version.Parse( "1.0"))
                                                                    });

            Package packageC20 = PackageUtility.CreatePackage("C", "2.0");
            Package packageD10 = PackageUtility.CreatePackage("D", "1.0");

            // A 2.0 -> [B 1.0, C 2.0, D 1.0]
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageC20);
            sourceRepository.AddPackage(packageD10);

            // Act
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.UpdatePackageReference("A"), "Conflict occurred. 'C 1.0' referenced but requested 'C 2.0'. 'G 1.0' depends on 'C 1.0'");
        }

        [TestMethod]
        public void UpdatePackageReferenceFromRepositoryThrowsIfPackageHasDependents() {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(sourceRepository, PackageUtility.CreateAssemblyResolver(), new MockProjectSystem());
            Package packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", version:new Version("1.0"))
                                                                    });
            Package packageB10 = PackageUtility.CreatePackage("B", "1.0");
            Package packageB20 = PackageUtility.CreatePackage("B", "2.0");
            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageB20);

            // Act & Assert            
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.UpdatePackageReference("B"), "Conflict occurred. 'B 1.0' referenced but requested 'B 2.0'. 'A 1.0' depends on 'B 1.0'");
        }

        [TestMethod]
        public void UpdatePackageReferenceNoVersionSpecifiedShouldUpdateToLatest() {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(sourceRepository, PackageUtility.CreateAssemblyResolver(), new MockProjectSystem());
            Package package10 = PackageUtility.CreatePackage("NetFramework", "1.0");
            projectManager.LocalRepository.AddPackage(package10);
            sourceRepository.AddPackage(package10);

            Package package11 = PackageUtility.CreatePackage("NetFramework", "1.1");
            sourceRepository.AddPackage(package11);

            Package package20 = PackageUtility.CreatePackage("NetFramework", "2.0");
            sourceRepository.AddPackage(package20);

            Package package35 = PackageUtility.CreatePackage("NetFramework", "3.5");
            sourceRepository.AddPackage(package35);

            // Act
            projectManager.UpdatePackageReference("NetFramework");

            // Assert
            Assert.IsFalse(projectManager.LocalRepository.IsPackageInstalled(package10));
            Assert.IsTrue(projectManager.LocalRepository.IsPackageInstalled(package35));
        }

        [TestMethod]
        public void UpdatePackageReferenceVersionSpeciedShouldUpdateToSpecifiedVersion() {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(sourceRepository, PackageUtility.CreateAssemblyResolver(), new MockProjectSystem());
            var package10 = PackageUtility.CreatePackage("NetFramework", "1.0");
            projectManager.LocalRepository.AddPackage(package10);
            sourceRepository.AddPackage(package10);

            var package11 = PackageUtility.CreatePackage("NetFramework", "1.1");
            sourceRepository.AddPackage(package11);

            var package20 = PackageUtility.CreatePackage("NetFramework", "2.0");
            sourceRepository.AddPackage(package20);

            // Act
            projectManager.UpdatePackageReference("NetFramework", new Version("1.1"));

            // Assert
            Assert.IsFalse(projectManager.LocalRepository.IsPackageInstalled(package10));
            Assert.IsTrue(projectManager.LocalRepository.IsPackageInstalled(package11));
        }

        [TestMethod]
        public void RemovingPackageReferenceRemovesPackageButNotDependencies() {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(sourceRepository, PackageUtility.CreateAssemblyResolver(), new MockProjectSystem());

            Package packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                PackageDependency.CreateDependency("B")
                                                            });

            Package packageB = PackageUtility.CreatePackage("B", "1.0");

            projectManager.LocalRepository.AddPackage(packageA);
            projectManager.LocalRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);

            // Act
            projectManager.RemovePackageReference("A");

            // Assert            
            Assert.IsFalse(projectManager.LocalRepository.IsPackageInstalled(packageA));
            Assert.IsTrue(projectManager.LocalRepository.IsPackageInstalled(packageB));
        }

        [TestMethod]
        public void ReAddingAPackageReferenceAfterRemovingADependencyShouldReReferenceAllDependencies() {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(sourceRepository, PackageUtility.CreateAssemblyResolver(), new MockProjectSystem());

            Package packageA = PackageUtility.CreatePackage("A", "1.0",
                dependencies: new List<PackageDependency> {
                    PackageDependency.CreateDependency("B")
                });

            Package packageB = PackageUtility.CreatePackage("B", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                PackageDependency.CreateDependency("C")
                                                            });

            var packageC = PackageUtility.CreatePackage("C", "1.0");

            projectManager.LocalRepository.AddPackage(packageA);
            projectManager.LocalRepository.AddPackage(packageB);

            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageC);

            // Act
            projectManager.AddPackageReference("A");

            // Assert            
            Assert.IsTrue(projectManager.LocalRepository.IsPackageInstalled(packageA));
            Assert.IsTrue(projectManager.LocalRepository.IsPackageInstalled(packageB));
            Assert.IsTrue(projectManager.LocalRepository.IsPackageInstalled(packageC));
        }

        [TestMethod]
        public void AddPackageReferenceWithAnyNonCompatibleReferenceThrows() {
            // Arrange
            Mock<MockProjectSystem> mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };
            var sourceRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(sourceRepository, PackageUtility.CreateAssemblyResolver(), mockProjectSystem.Object);
            mockProjectSystem.Setup(m => m.TargetFramework).Returns(new FrameworkName(".NETFramework", new Version("2.0")));
            var mockPackage = new Mock<Package>();
            mockPackage.Setup(m => m.Id).Returns("A");
            mockPackage.Setup(m => m.Version).Returns(new Version("1.0"));
            var assemblyReference = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("5.0")));
            mockPackage.Setup(m => m.AssemblyReferences).Returns(new[] { assemblyReference });
            sourceRepository.AddPackage(mockPackage.Object);

            // Act & Assert            
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.AddPackageReference("A"), "Unable to find assembly references that are compatible with the target framework '.NETFramework,Version=v2.0'");
        }

        [TestMethod]
        public void GetCompatibleReferencesPicksHigestVersionLessThanTargetVersion() {
            // Arrange                                                                                                                       
            var assemblyReference10 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("1.0")));
            var assemblyReference20 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("2.0")));
            var assemblyReference30 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("3.0")));
            var assemblyReference40 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("4.0")));
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReference10, assemblyReference20, assemblyReference30, assemblyReference40 };

            // Act
            var targetAssemblyReferences = ProjectManager.GetCompatibleAssemblyReferences(new FrameworkName(".NETFramework", new Version("3.5")), assemblyReferences)
                                                         .ToList();

            // Assert
            Assert.AreSame(assemblyReference30, targetAssemblyReferences[0]);
        }

        [TestMethod]
        public void GetCompatibleReferencesReferenceWithNullVersionHasExactTargetVersion() {
            // Arrange
            var assemblyReference10 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("1.0")));
            var assemblyReference20 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("2.0")));
            var assemblyReference30 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("3.0")));
            var assemblyReference40 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("4.0")));
            var assemblyReferenceNoVersion = PackageUtility.CreateAssemblyReference("foo.dll", null);
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReference10, assemblyReference20, assemblyReference30, assemblyReference40, assemblyReferenceNoVersion };

            // Act
            var targetAssemblyReferences = ProjectManager.GetCompatibleAssemblyReferences(new FrameworkName(".NETFramework", new Version("3.5")), assemblyReferences).ToList();

            // Assert
            Assert.AreSame(assemblyReferenceNoVersion, targetAssemblyReferences[0]);
        }

        [TestMethod]
        public void GetCompatibleReferencesMutipleAssemblies() {
            // Arrange
            var assemblyReference10 = PackageUtility.CreateAssemblyReference("foo1.dll", new FrameworkName(".NETFramework", new Version("1.0")));
            var assemblyReference20 = PackageUtility.CreateAssemblyReference("foo1.dll", new FrameworkName(".NETFramework", new Version("2.0")));
            var assemblyReference30 = PackageUtility.CreateAssemblyReference("foo2.dll", new FrameworkName(".NETFramework", new Version("3.0")));
            var assemblyReference40 = PackageUtility.CreateAssemblyReference("foo2.dll", new FrameworkName(".NETFramework", new Version("4.0")));
            var assemblyReferenceNoVersion = PackageUtility.CreateAssemblyReference("foo3.dll", null);
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReference10, assemblyReference20, assemblyReference30, assemblyReference40, assemblyReferenceNoVersion };

            // Act
            var compatibleAssemblyReferences = ProjectManager.GetCompatibleAssemblyReferences(new FrameworkName(".NETFramework", new Version("3.5")), assemblyReferences).ToList();

            // Assert
            Assert.AreEqual(1, compatibleAssemblyReferences.Count);
            Assert.AreEqual(assemblyReferenceNoVersion, compatibleAssemblyReferences[0]);
        }

        [TestMethod]
        public void GetCompatibleReferencesReturnsNullIfNoBestMatchFound() {
            // Arrange
            var assemblyReference = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("5.0")));
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReference };

            // Act
            var compatibleAssemblyReferences = ProjectManager.GetCompatibleAssemblyReferences(new FrameworkName(".NETFramework", new Version("3.5")), assemblyReferences);

            // Assert
            Assert.IsNull(compatibleAssemblyReferences);
        }

        private ProjectManager CreateProjectManager() {
            return new ProjectManager(new MockPackageRepository(), PackageUtility.CreateAssemblyResolver(), new MockProjectSystem());
        }
    }
}
