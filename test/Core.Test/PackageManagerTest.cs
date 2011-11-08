using System;
using System.Collections.Generic;
using System.IO;
using Moq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{
    public class PackageManagerTest
    {
        [Fact]
        public void CtorThrowsIfDependenciesAreNull()
        {
            // Act & Assert
            ExceptionAssert.ThrowsArgNull(() => new PackageManager(null, new DefaultPackagePathResolver("foo"), new MockProjectSystem(), new MockPackageRepository()), "sourceRepository");
            ExceptionAssert.ThrowsArgNull(() => new PackageManager(new MockPackageRepository(), null, new MockProjectSystem(), new MockPackageRepository()), "pathResolver");
            ExceptionAssert.ThrowsArgNull(() => new PackageManager(new MockPackageRepository(), new DefaultPackagePathResolver("foo"), null, new MockPackageRepository()), "fileSystem");
            ExceptionAssert.ThrowsArgNull(() => new PackageManager(new MockPackageRepository(), new DefaultPackagePathResolver("foo"), new MockProjectSystem(), null), "localRepository");
        }

        [Fact]
        public void InstallingPackageWithUnknownDependencyAndIgnoreDepencenciesInstallsPackageWithoutDependencies()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                 new PackageDependency("C")
                                                             });

            IPackage packageC = PackageUtility.CreatePackage("C", "1.0");
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageC);

            // Act
            packageManager.InstallPackage("A", version: null, ignoreDependencies: true, allowPrereleaseVersions: true);

            // Assert
            Assert.True(localRepository.Exists(packageA));
            Assert.False(localRepository.Exists(packageC));
        }

        [Fact]
        public void UninstallingUnknownPackageThrows()
        {
            // Arrange
            PackageManager packageManager = CreatePackageManager();

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => packageManager.UninstallPackage("foo"), "Unable to find package 'foo'.");
        }

        [Fact]
        public void UninstallingUnknownNullOrEmptyPackageIdThrows()
        {
            // Arrange
            PackageManager packageManager = CreatePackageManager();

            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => packageManager.UninstallPackage((string)null), "packageId");
            ExceptionAssert.ThrowsArgNullOrEmpty(() => packageManager.UninstallPackage(String.Empty), "packageId");
        }

        [Fact]
        public void UninstallingPackageWithNoDependents()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var package = PackageUtility.CreatePackage("foo", "1.2.33");
            localRepository.AddPackage(package);

            // Act
            packageManager.UninstallPackage("foo");

            // Assert
            Assert.False(packageManager.LocalRepository.Exists(package));
        }

        [Fact]
        public void InstallingUnknownPackageThrows()
        {
            // Arrange
            PackageManager packageManager = CreatePackageManager();

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => packageManager.InstallPackage("unknown"),
                                                              "Unable to find package 'unknown'.");
        }

        [Fact]
        public void InstallPackageNullOrEmptyPackageIdThrows()
        {
            // Arrange
            PackageManager packageManager = CreatePackageManager();

            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => packageManager.InstallPackage((string)null), "packageId");
            ExceptionAssert.ThrowsArgNullOrEmpty(() => packageManager.InstallPackage(String.Empty), "packageId");
        }

        [Fact]
        public void InstallPackageAddsAllFilesToFileSystem()
        {
            // Arrange
            var projectSystem = new MockProjectSystem();
            var sourceRepository = new MockPackageRepository();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var packageManager = new PackageManager(sourceRepository, pathResolver, projectSystem, new LocalPackageRepository(pathResolver, projectSystem));

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                             new[] { "contentFile", @"sub\contentFile" },
                                                             new[] { @"lib\reference.dll" },
                                                             new[] { @"readme.txt" });

            sourceRepository.AddPackage(packageA);

            // Act
            packageManager.InstallPackage("A");

            // Assert
            Assert.Equal(0, projectSystem.References.Count);
            Assert.Equal(5, projectSystem.Paths.Count);
            Assert.True(projectSystem.FileExists(@"A.1.0\content\contentFile"));
            Assert.True(projectSystem.FileExists(@"A.1.0\content\sub\contentFile"));
            Assert.True(projectSystem.FileExists(@"A.1.0\lib\reference.dll"));
            Assert.True(projectSystem.FileExists(@"A.1.0\tools\readme.txt"));
            Assert.True(projectSystem.FileExists(@"A.1.0\A.1.0.nupkg"));
        }

        [Fact]
        public void UnInstallingPackageUninstallsPackageButNotDependencies()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                new PackageDependency("B")
                                                            });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0");

            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);

            // Act
            packageManager.UninstallPackage("A");

            // Assert            
            Assert.False(localRepository.Exists(packageA));
            Assert.True(localRepository.Exists(packageB));
        }

        [Fact]
        public void ReInstallingPackageAfterUninstallingDependencyShouldReinstallAllDependencies()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            PackageManager packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                dependencies: new List<PackageDependency> {
                    new PackageDependency("B")
                });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0",
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
            Assert.True(localRepository.Exists(packageA));
            Assert.True(localRepository.Exists(packageB));
            Assert.True(localRepository.Exists(packageC));
        }

        [Fact]
        public void InstallPackageThrowsExceptionPackageIsNotInstalled()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new Mock<IProjectSystem>();
            projectSystem.Setup(m => m.AddFile(@"A.1.0\content\file", It.IsAny<Stream>())).Throws<UnauthorizedAccessException>();
            projectSystem.Setup(m => m.Root).Returns("FakeRoot");
            PackageManager packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem.Object), projectSystem.Object, localRepository);

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", new[] { "file" });
            sourceRepository.AddPackage(packageA);

            // Act
            ExceptionAssert.Throws<UnauthorizedAccessException>(() => packageManager.InstallPackage("A"));


            // Assert
            Assert.False(packageManager.LocalRepository.Exists(packageA));
        }

        [Fact]
        public void UpdatePackageUninstallsPackageAndInstallsNewPackage()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            PackageManager packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);

            IPackage A10 = PackageUtility.CreatePackage("A", "1.0");
            IPackage A20 = PackageUtility.CreatePackage("A", "2.0");
            localRepository.Add(A10);
            sourceRepository.Add(A20);

            // Act
            packageManager.UpdatePackage("A", updateDependencies: true, allowPrereleaseVersions: false);

            // Assert
            Assert.False(localRepository.Exists("A", new SemanticVersion("1.0")));
            Assert.True(localRepository.Exists("A", new SemanticVersion("2.0")));
        }

        [Fact]
        public void UpdatePackageThrowsIfPackageNotInstalled()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            PackageManager packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);

            IPackage A20 = PackageUtility.CreatePackage("A", "2.0");
            sourceRepository.Add(A20);

            // Act
            ExceptionAssert.Throws<InvalidOperationException>(() => packageManager.UpdatePackage("A", updateDependencies: true, allowPrereleaseVersions: false),
                "Unable to find package 'A'.");
        }

        [Fact]
        public void UpdatePackageDoesNothingIfNoUpdatesAvailable()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            PackageManager packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);

            IPackage A10 = PackageUtility.CreatePackage("A", "1.0");
            localRepository.Add(A10);

            // Act
            packageManager.UpdatePackage("A", updateDependencies: true, allowPrereleaseVersions: false);

            // Assert
            Assert.True(localRepository.Exists("A", new SemanticVersion("1.0")));
        }

        [Fact]
        public void InstallPackageInstallsPrereleasePackages()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0.0-beta",
                                                             dependencies: new[] {
                                                                 new PackageDependency("C")
                                                             });

            IPackage packageC = PackageUtility.CreatePackage("C", "1.0.0");
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageC);

            // Act
            packageManager.InstallPackage("A", version: null, ignoreDependencies: false, allowPrereleaseVersions: true);

            // Assert
            Assert.True(localRepository.Exists(packageA));
            Assert.True(localRepository.Exists(packageC));
        }

        [Fact]
        public void InstallPackageInstallsPackagesWithPrereleaseDependenciesIfFlagIsSet()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0.0",
                                                             dependencies: new[] {
                                                                 new PackageDependency("C")
                                                             });

            IPackage packageC = PackageUtility.CreatePackage("C", "1.0.0-RC-1");
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageC);

            // Act
            packageManager.InstallPackage("A", version: null, ignoreDependencies: false, allowPrereleaseVersions: true);

            // Assert
            Assert.True(localRepository.Exists(packageA));
            Assert.True(localRepository.Exists(packageC));
        }

        [Fact]
        public void InstallPackageThrowsIfDependencyIsPrereleaseAndFlagIsNotSet()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0.0",
                                                             dependencies: new[] {
                                                                 new PackageDependency("C")
                                                             });

            IPackage packageC = PackageUtility.CreatePackage("C", "1.0.0-RC-1");
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageC);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => packageManager.InstallPackage("A", version: null, ignoreDependencies: false, allowPrereleaseVersions: false),
                "Unable to resolve dependency 'C'.");
        }

        [Fact]
        public void InstallPackageInstallsLowerReleaseVersionIfPrereleaseFlagIsNotSet()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0.0",
                                                             dependencies: new[] {
                                                                 new PackageDependency("C")
                                                             });

            IPackage packageC_RC = PackageUtility.CreatePackage("C", "1.0.0-RC-1");
            IPackage packageC = PackageUtility.CreatePackage("C", "0.9");
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageC);
            sourceRepository.AddPackage(packageC_RC);

            // Act 
            packageManager.InstallPackage("A", version: null, ignoreDependencies: false, allowPrereleaseVersions: false);

            // Assert
            Assert.True(localRepository.Exists(packageA));
            Assert.True(localRepository.Exists(packageC));
        }

        [Fact]
        public void InstallPackageConsidersAlreadyInstalledPrereleasePackagesWhenResolvingDependencies()
        {
            // Arrange
            var packageB_05 = PackageUtility.CreatePackage("B", "0.5.0");
            var packageB_10a = PackageUtility.CreatePackage("B", "1.0.0-a");
            var packageA = PackageUtility.CreatePackage("A",
                                dependencies: new[] { new PackageDependency("B", VersionUtility.ParseVersionSpec("[0.5.0, 2.0.0)")) });

            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB_10a);
            sourceRepository.AddPackage(packageB_05);

            // Act 
            // The allowPrereleaseVersions flag should be irrelevant since we specify a version.
            packageManager.InstallPackage("B", version: new SemanticVersion("1.0.0-a"), ignoreDependencies: false, allowPrereleaseVersions: false);
            // Verify we actually did install B.1.0.0a
            Assert.True(localRepository.Exists(packageB_10a));

            packageManager.InstallPackage("A");

            // Assert
            Assert.True(localRepository.Exists(packageA));
            Assert.True(localRepository.Exists(packageB_10a));

        }

        private PackageManager CreatePackageManager()
        {
            var projectSystem = new MockProjectSystem();
            return new PackageManager(
                new MockPackageRepository(),
                new DefaultPackagePathResolver(projectSystem),
                projectSystem,
                new MockPackageRepository());
        }
    }
}
