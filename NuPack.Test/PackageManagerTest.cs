namespace NuPack.Test {
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NuPack.Test.Mocks;    

    [TestClass]
    public class PackageManagerTest {
        [TestMethod]
        public void InstallingPackageWithUnknownDependencyAndIgnoreDepencenciesInstallsPackageWithoutDependencies() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var packageManager = new PackageManager(sourceRepository, new MockProjectSystem(), localRepository);

            Package packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                 new PackageDependency("C")
                                                             });
            
            Package packageC = PackageUtility.CreatePackage("C", "1.0");
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageC);

            // Act
            packageManager.InstallPackage("A", ignoreDependencies: true);

            // Assert
            Assert.IsTrue(localRepository.IsPackageInstalled(packageA));
            Assert.IsFalse(localRepository.IsPackageInstalled(packageC));
        }        

        [TestMethod]
        public void UninstallingUnknownPackageThrows() {
            // Arrange
            PackageManager packageManager = CreatePackageManager();

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => packageManager.UninstallPackage("foo"), "Unable to find package 'foo'");
        }

        [TestMethod]
        public void UninstallingUnknownNullOrEmptyPackageIdThrows() {
            // Arrange
            PackageManager packageManager = CreatePackageManager();

            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => packageManager.UninstallPackage((string)null), "packageId");
            ExceptionAssert.ThrowsArgNullOrEmpty(() => packageManager.UninstallPackage(String.Empty), "packageId");
        }

        [TestMethod]
        public void UninstallingPackageWithNoDependents() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var packageManager = new PackageManager(sourceRepository, new MockProjectSystem(), localRepository);
            var package = PackageUtility.CreatePackage("foo", "1.2.33");
            localRepository.AddPackage(package);

            // Act
            packageManager.UninstallPackage("foo");

            // Assert
            Assert.IsFalse(packageManager.LocalRepository.IsPackageInstalled(package));
        }

        [TestMethod]
        public void InstallingUnknownPackageThrows() {
            // Arrange
            PackageManager packageManager = CreatePackageManager();

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => packageManager.InstallPackage("unknown"),
                                                              "Unable to find package 'unknown'");
        }

        [TestMethod]
        public void InstallPackageNullOrEmptyPackageIdThrows() {
            // Arrange
            PackageManager packageManager = CreatePackageManager();

            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => packageManager.InstallPackage((string)null), "packageId");
            ExceptionAssert.ThrowsArgNullOrEmpty(() => packageManager.InstallPackage(String.Empty), "packageId");
        }

        [TestMethod]
        public void InstallPackageAddsAllFilesToFileSystem() {
            // Arrange
            MockProjectSystem projectSystem = new MockProjectSystem();            
            var sourceRepository = new MockPackageRepository();
            var packageManager = new PackageManager(sourceRepository, projectSystem, new MockPackageRepository());

            Package packageA = PackageUtility.CreatePackage("A", "1.0", 
                                                             new[] { "content", @"sub\content" }, 
                                                             new[] { "reference.dll" }, 
                                                             new[] { "init.ps1" });

            sourceRepository.AddPackage(packageA);

            // Act
            packageManager.InstallPackage("A");

            // Assert
            Assert.AreEqual(0, projectSystem.References.Count);
            Assert.AreEqual(4, projectSystem.Paths.Count);
            Assert.IsTrue(projectSystem.FileExists(@"A.1.0\content"));
            Assert.IsTrue(projectSystem.FileExists(@"A.1.0\sub\content"));
            Assert.IsTrue(projectSystem.FileExists(@"A.1.0\lib\reference.dll"));
            Assert.IsTrue(projectSystem.FileExists(@"A.1.0\tools\init.ps1"));
        }

        [TestMethod]
        public void UnInstallingPackageUninstallsPackageButNotDependencies() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            PackageManager packageManager = new PackageManager(sourceRepository, new MockProjectSystem(), localRepository);

            Package packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                new PackageDependency("B")
                                                            });

            Package packageB = PackageUtility.CreatePackage("B", "1.0");

            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);

            // Act
            packageManager.UninstallPackage("A");

            // Assert            
            Assert.IsFalse(localRepository.IsPackageInstalled(packageA));
            Assert.IsTrue(localRepository.IsPackageInstalled(packageB));
        }

        [TestMethod]
        public void ReInstallingPackageAfterUninstallingDependencyShouldReinstallAllDependencies() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            PackageManager packageManager = new PackageManager(sourceRepository, new MockProjectSystem(), localRepository);

            Package packageA = PackageUtility.CreatePackage("A", "1.0",
                dependencies: new List<PackageDependency> {
                    new PackageDependency("B")
                });

            Package packageB = PackageUtility.CreatePackage("B", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                new PackageDependency("C")
                                                            });

            var packageC = PackageUtility.CreatePackage("C", "1.0");

            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);

            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageC);

            // Act
            packageManager.InstallPackage("A");

            // Assert            
            Assert.IsTrue(localRepository.IsPackageInstalled(packageA));
            Assert.IsTrue(localRepository.IsPackageInstalled(packageB));
            Assert.IsTrue(localRepository.IsPackageInstalled(packageC));
        }

        private PackageManager CreatePackageManager() {
            return new PackageManager(new MockPackageRepository(), new MockProjectSystem());
        }

    }
}
