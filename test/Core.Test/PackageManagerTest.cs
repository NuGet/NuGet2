using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using NuGet.Resolver;
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
            ExceptionAssert.ThrowsArgNull(() => new PackageManager(null, new DefaultPackagePathResolver("foo"), new MockProjectSystem(), new MockSharedPackageRepository()), "sourceRepository");
            ExceptionAssert.ThrowsArgNull(() => new PackageManager(new MockPackageRepository(), null, new MockProjectSystem(), new MockSharedPackageRepository()), "pathResolver");
            ExceptionAssert.ThrowsArgNull(() => new PackageManager(new MockPackageRepository(), new DefaultPackagePathResolver("foo"), null, new MockSharedPackageRepository()), "fileSystem");
            ExceptionAssert.ThrowsArgNull(() => new PackageManager(new MockPackageRepository(), new DefaultPackagePathResolver("foo"), new MockProjectSystem(), null), "localRepository");
        }

        /* !!!
        [Fact]
        public void InstallingPackageWithUnknownDependencyAndIgnoreDepencenciesInstallsPackageWithoutDependencies()
        {
            // Arrange
            var localRepository = new MockSharedPackageRepository();
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
        } */

        /* !!! Should be changed to a test using Install-Package cmdlet
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
        */

        [Fact]
        public void InstallPackageAddsAllFilesToFileSystem()
        {
            // Arrange
            var projectSystem = new MockProjectSystem();
            var sourceRepository = new MockPackageRepository();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var packageManager = new PackageManager(sourceRepository, pathResolver, projectSystem, 
                new SharedPackageRepository(pathResolver, projectSystem, projectSystem));

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                             new[] { "contentFile", @"sub\contentFile" },
                                                             new[] { @"lib\reference.dll" },
                                                             new[] { @"readme.txt" });

            sourceRepository.AddPackage(packageA);

            // Act
            Install("A", new NullProjectManager(packageManager));

            // Assert
            Assert.Equal(0, projectSystem.References.Count);
            Assert.Equal(5, projectSystem.Paths.Count);
            Assert.True(projectSystem.FileExists(@"A.1.0\content\contentFile"));
            Assert.True(projectSystem.FileExists(@"A.1.0\content\sub\contentFile"));
            Assert.True(projectSystem.FileExists(@"A.1.0\lib\reference.dll"));
            Assert.True(projectSystem.FileExists(@"A.1.0\tools\readme.txt"));
            Assert.True(projectSystem.FileExists(@"A.1.0\A.1.0.nupkg"));
        }

        static void Install(
           string id,
           IProjectManager projectManager)
        {
            var resolver = new ActionResolver();

            // Resolve the package to install
            IPackage package = PackageRepositoryHelper.ResolvePackage(
                projectManager.PackageManager.SourceRepository,
                projectManager.PackageManager.LocalRepository,
                id,
                null,
                resolver.AllowPrereleaseVersions);

            // Resolve actions
            resolver.AddOperation(PackageAction.Install, package, projectManager);
            var actions = resolver.ResolveActions();

            var actionExecutor = new ActionExecutor();
            actionExecutor.Execute(actions);
        }

        [Fact]
        public void InstallingSatellitePackageCopiesFilesIntoRuntimePackageFolderWhenRuntimeIsInstalledAsADependency()
        {
            // Arrange
            // Create a runtime package and a satellite package that has a dependency on the runtime package, and uses the
            // local suffix convention.
            var runtimePackage = PackageUtility.CreatePackage("foo", "1.0.0", assemblyReferences: new[] { @"lib\foo.dll" });

            var satellitePackage = PackageUtility.CreatePackage("foo.ja-jp", "1.0.0", language: "ja-jp",
                                                    satelliteAssemblies: new[] { @"lib\ja-jp\foo.resources.dll", @"lib\ja-jp\foo.xml" },
                                                    dependencies: new[] { new PackageDependency("foo", VersionUtility.ParseVersionSpec("[1.0.0]")) });

            var projectSystem = new MockProjectSystem();
            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            sourceRepository.AddPackage(runtimePackage);
            sourceRepository.AddPackage(satellitePackage);

            // Act
            Install("foo.ja-jp", new NullProjectManager(packageManager));

            // Assert
            Assert.True(projectSystem.FileExists(@"foo.1.0.0\lib\foo.dll"));
            Assert.True(projectSystem.FileExists(@"foo.1.0.0\lib\ja-jp\foo.resources.dll"));
            Assert.True(projectSystem.FileExists(@"foo.1.0.0\lib\ja-jp\foo.xml"));
            Assert.True(projectSystem.FileExists(@"foo.ja-jp.1.0.0\lib\ja-jp\foo.resources.dll"));
            Assert.True(projectSystem.FileExists(@"foo.ja-jp.1.0.0\lib\ja-jp\foo.xml"));
        }

        [Fact]
        public void InstallSatellitePackageCopiesFilesToExistingRuntimePackageFolder()
        {
            // Arrange
            // Create a runtime package and a satellite package that has a dependency on the runtime package, and uses the
            // local suffix convention.
            var runtimePackage = PackageUtility.CreatePackage("foo", "1.0.0", assemblyReferences: new[] { @"lib\foo.dll" });

            var satellitePackage = PackageUtility.CreatePackage("foo.ja-jp", "1.0.0", language: "ja-jp",
                                                    satelliteAssemblies: new[] { @"lib\ja-jp\foo.resources.dll", @"lib\ja-jp\foo.xml" },
                                                    dependencies: new[] { new PackageDependency("foo", VersionUtility.ParseVersionSpec("[1.0.0]")) });

            var projectSystem = new MockProjectSystem();
            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            sourceRepository.AddPackage(runtimePackage);
            sourceRepository.AddPackage(satellitePackage);

            // Act
            Install("foo", new NullProjectManager(packageManager));
            Install("foo.ja-jp", new NullProjectManager(packageManager));

            // Assert
            Assert.True(projectSystem.FileExists(@"foo.1.0.0\lib\foo.dll"));
            Assert.True(projectSystem.FileExists(@"foo.1.0.0\lib\ja-jp\foo.resources.dll"));
            Assert.True(projectSystem.FileExists(@"foo.1.0.0\lib\ja-jp\foo.xml"));
            Assert.True(projectSystem.FileExists(@"foo.ja-jp.1.0.0\lib\ja-jp\foo.resources.dll"));
            Assert.True(projectSystem.FileExists(@"foo.ja-jp.1.0.0\lib\ja-jp\foo.xml"));
        }

        [Fact]
        public void InstallingSatellitePackageOnlyCopiesCultureSpecificLibFolderContents()
        {
            // Arrange
            // Create a runtime package and a satellite package that has a dependency on the runtime package, and uses the
            // local suffix convention.
            var runtimePackage = PackageUtility.CreatePackage("foo", "1.0.0",
                                                    assemblyReferences: new[] {
                                                        @"lib\foo.dll" },
                                                    content: new[] {
                                                        @"english.txt" });

            var satellitePackage = PackageUtility.CreatePackage("foo.ja-jp", "1.0.0", language: "ja-jp",
                                                    satelliteAssemblies: new[] {
                                                        @"lib\ja-jp\foo.resources.dll",
                                                        @"lib\ja-jp\foo.xml",
                                                        @"lib\japanese.xml" },
                                                    content: new[] {
                                                        @"english.txt",
                                                        @"japanese.txt" },
                                                    dependencies: new[] { new PackageDependency("foo", VersionUtility.ParseVersionSpec("[1.0.0]")) });

            var projectSystem = new MockProjectSystem();
            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            sourceRepository.AddPackage(runtimePackage);
            sourceRepository.AddPackage(satellitePackage);

            // Act
            Install("foo", new NullProjectManager(packageManager));
            Install("foo.ja-jp", new NullProjectManager(packageManager));

            // Assert
            Assert.Equal(9, projectSystem.Paths.Count);
            Assert.True(projectSystem.FileExists(@"foo.1.0.0\content\english.txt"));
            Assert.True(projectSystem.FileExists(@"foo.1.0.0\lib\foo.dll"));
            Assert.True(projectSystem.FileExists(@"foo.1.0.0\lib\ja-jp\foo.resources.dll"));
            Assert.True(projectSystem.FileExists(@"foo.1.0.0\lib\ja-jp\foo.xml"));

            Assert.True(projectSystem.FileExists(@"foo.ja-jp.1.0.0\content\english.txt")); 
            Assert.True(projectSystem.FileExists(@"foo.ja-jp.1.0.0\content\japanese.txt"));
            Assert.True(projectSystem.FileExists(@"foo.ja-jp.1.0.0\lib\japanese.xml"));
            Assert.True(projectSystem.FileExists(@"foo.ja-jp.1.0.0\lib\ja-jp\foo.resources.dll"));
            Assert.True(projectSystem.FileExists(@"foo.ja-jp.1.0.0\lib\ja-jp\foo.xml"));

            Assert.False(projectSystem.FileExists(@"foo.1.0.0\lib\japanese.xml"));
            Assert.False(projectSystem.FileExists(@"foo.1.0.0\content\japanese.txt"));
        }

        private void Uninstall(string id, IPackageManager packageManager, IProjectManager projectManager)
        {
            // Locate the package to uninstall
            IPackage package = packageManager.LocatePackageToUninstall(
                projectManager,
                id,
                version: null);

            // resolve actions
            var resolver = new ActionResolver();
            resolver.AddOperation(PackageAction.Uninstall, package, projectManager);
            var actions = resolver.ResolveActions();

            // execute actions
            var actionExecutor = new ActionExecutor();
            actionExecutor.Execute(actions);
        }

        [Fact]
        public void UninstallingSatellitePackageRemovesFilesFromRuntimePackageFolder()
        {
            // Arrange
            // Create a runtime package and a satellite package that has a dependency on the runtime package, and uses the
            // local suffix convention.
            var runtimePackage = PackageUtility.CreatePackage("foo", "1.0.0",
                                                    assemblyReferences: new[] {
                                                        @"lib\foo.dll" },
                                                    content: new[] {
                                                        @"english.txt" });

            var satellitePackage = PackageUtility.CreatePackage(
                "foo.ja-jp", "1.0.0", language: "ja-jp",
                satelliteAssemblies: new[] {
                    @"lib\ja-jp\foo.resources.dll",
                    @"lib\ja-jp\foo.xml",
                    @"lib\japanese.xml" },
                content: new[] {
                    @"english.txt",
                    @"japanese.txt" },
                dependencies: new[] { new PackageDependency("foo", VersionUtility.ParseVersionSpec("[1.0]")) });

            var packagesFolder = new MockFileSystem(@"c:\packagesFolder");
            var sharedRepository = new MockSharedPackageRepository2();
            var sourceRepository = new MockPackageRepository();

            var packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(packagesFolder), packagesFolder, sharedRepository);
            sourceRepository.AddPackage(runtimePackage);
            sourceRepository.AddPackage(satellitePackage);

            var projectSystem = new MockProjectSystem();
            var projectLocalRepo = new PackageReferenceRepository(
                new MockFileSystem(@"c:\project"),
                "projectName",
                sharedRepository);
            var projectManager = new ProjectManager(
                packageManager,
                packageManager.PathResolver,
                projectSystem,
                projectLocalRepo);

            // Act            
            Install("foo", projectManager);
            Install("foo.ja-jp", projectManager);

            Uninstall("foo.ja-jp", packageManager, projectManager);

            // Assert
            Assert.Equal(2, packagesFolder.Paths.Count);
            Assert.True(packagesFolder.FileExists(@"foo.1.0.0\content\english.txt"));
            Assert.True(packagesFolder.FileExists(@"foo.1.0.0\lib\foo.dll"));
            Assert.False(packagesFolder.FileExists(@"foo.1.0.0\lib\ja-jp\foo.resources.dll"));
            Assert.False(packagesFolder.FileExists(@"foo.1.0.0\lib\ja-jp\foo.xml"));

            Assert.False(packagesFolder.FileExists(@"foo.ja-jp.1.0.0\content\english.txt"));
            Assert.False (packagesFolder.FileExists(@"foo.ja-jp.1.0.0\content\japanese.txt"));
            Assert.False(packagesFolder.FileExists(@"foo.ja-jp.1.0.0\lib\japanese.xml"));
            Assert.False(packagesFolder.FileExists(@"foo.ja-jp.1.0.0\lib\ja-jp\foo.resources.dll"));
            Assert.False(packagesFolder.FileExists(@"foo.ja-jp.1.0.0\lib\ja-jp\foo.xml"));

            Assert.False(packagesFolder.FileExists(@"foo.1.0.0\lib\japanese.xml"));
            Assert.False(packagesFolder.FileExists(@"foo.1.0.0\content\japanese.txt"));
        }

        /// <summary>
        /// This test demonstrates that satellite packages that have satellite files
        /// that match the files in the runtime package can cause the runtime package's
        /// file to be removed when the satellite package is uninstalled.
        /// </summary>
        /// <remarks>
        /// This is an acceptable limitation of the design: during uninstallation of the satellite package,
        /// we don't check the runtime package to see if files qualified as satellite files
        /// already existed in the runtime package.
        /// <para>
        /// And as the uninstall.ps1 end-to-end tests demonstrate, the only way this collision can cause the
        /// runtime package's file to be removed is when the files are exact matches of one another.  Otherwise,
        /// the file will be recognized as different, and it won't be uninstalled when uninstalling ja-jp.
        /// </para>
        /// </remarks>
        [Fact]
        public void UninstallingSatellitePackageRemovesCollidingRuntimeFiles()
        {
            // Arrange
            // Create a runtime package and a satellite package that has a dependency on the runtime package, and uses the
            // local suffix convention.
            var runtimePackage = PackageUtility.CreatePackage(
                "foo", "1.0.0",
                content: Enumerable.Empty<string>(),
                assemblyReferences: new[] { @"lib\ja-jp\collision.txt" });

            var satellitePackage = PackageUtility.CreatePackage(
                "foo.ja-jp", "1.0.0", language: "ja-jp",
                content: Enumerable.Empty<string>(),
                satelliteAssemblies: new[] { @"lib\ja-jp\collision.txt" },
                dependencies: new[] { new PackageDependency("foo", VersionUtility.ParseVersionSpec("[1.0.0]")) });

            var packagesFolder = new MockFileSystem(@"c:\packagesFolder");
            var sharedRepository = new MockSharedPackageRepository2();
            var sourceRepository = new MockPackageRepository();
            var packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(packagesFolder), packagesFolder, sharedRepository);

            sourceRepository.AddPackage(runtimePackage);
            sourceRepository.AddPackage(satellitePackage);

            var projectSystem = new MockProjectSystem();
            var projectLocalRepo = new PackageReferenceRepository(
                new MockFileSystem(@"c:\project"),
                "projectName",
                sharedRepository);
            var projectManager = new ProjectManager(
                packageManager,
                packageManager.PathResolver,
                projectSystem,
                projectLocalRepo);

            // Act
            Install("foo", projectManager);
            Install("foo.ja-jp", projectManager);

            Uninstall("foo.ja-jp", packageManager, projectManager);

            // Assert
            Assert.Equal(0, packagesFolder.Paths.Count);
            Assert.False(packagesFolder.FileExists(@"foo.1.0.0\lib\ja-jp\collision.txt"));
        }

        /* !!! Should be changed to a test using Install-Package cmdlet
        [Fact]
        public void ReInstallingPackageAfterUninstallingDependencyShouldReinstallAllDependencies()
        {
            // Arrange
            var localRepository = new MockSharedPackageRepository();
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
        } */

        /* !!! Should be changed to a test using Install-Package cmdlet
        [Fact]
        public void InstallPackageThrowsExceptionPackageIsNotInstalled()
        {
            // Arrange
            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new Mock<IProjectSystem>();
            projectSystem.Setup(m => m.AddFiles(It.IsAny<IEnumerable<IPackageFile>>(), "A.1.0"))
                .Throws<UnauthorizedAccessException>();
            projectSystem.Setup(m => m.Root).Returns("FakeRoot");
            PackageManager packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem.Object), projectSystem.Object, localRepository);

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", new[] { "file" });
            sourceRepository.AddPackage(packageA);

            // Act
            ExceptionAssert.Throws<UnauthorizedAccessException>(() => packageManager.InstallPackage("A"));


            // Assert
            Assert.False(packageManager.LocalRepository.Exists(packageA));
        } */

        /* !!! Should be changed to a test using Install-Package cmdlet
        [Fact]
        public void UpdatePackageDoesNothingIfNoUpdatesAvailable()
        {
            // Arrange
            var localRepository = new MockSharedPackageRepository();
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
            var localRepository = new MockSharedPackageRepository();
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
        public void InstallPackageDisregardTargetFrameworkOfDependencies()
        {
            // Arrange
            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var packageManager = new PackageManager(
                sourceRepository, 
                new DefaultPackagePathResolver(projectSystem), 
                projectSystem, 
                localRepository);

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0.0",
                                                             dependencies: new[] {
                                                                 new PackageDependency("C", null)
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
            var localRepository = new MockSharedPackageRepository();
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
            var localRepository = new MockSharedPackageRepository();
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
            var localRepository = new MockSharedPackageRepository();
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
        } */

        /* !!! Should be changed to a test using Install-Package cmdlet
        [Fact]
        public void InstallPackageConsidersAlreadyInstalledPrereleasePackagesWhenResolvingDependencies()
        {
            // Arrange
            var packageB_05 = PackageUtility.CreatePackage("B", "0.5.0");
            var packageB_10a = PackageUtility.CreatePackage("B", "1.0.0-a");
            var packageA = PackageUtility.CreatePackage("A",
                                dependencies: new[] { new PackageDependency("B", VersionUtility.ParseVersionSpec("[0.5.0, 2.0.0)")) });

            var localRepository = new MockSharedPackageRepository();
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

        } */

        /* !!! Should be changed to a test using Install-Package cmdlet
        [Fact]
        public void InstallPackageNotifiesBatchProcessorWhenExpandingPackageFiles()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("B", "0.5.0", content: new[] { "content.txt" }, assemblyReferences: new[] { "Ref.dll" });

            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var fileSystem = new Mock<MockFileSystem> { CallBase = true };
            var batchProcessor = fileSystem.As<IBatchProcessor<string>>();
            batchProcessor.Setup(s => s.BeginProcessing(It.IsAny<IEnumerable<string>>(), PackageAction.Install))
                          .Callback((IEnumerable<string> files, PackageAction _) => Assert.Equal(new[] { @"content\content.txt", "Ref.dll" }, files))
                          .Verifiable();
            batchProcessor.Setup(s => s.EndProcessing()).Verifiable();

            var packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(fileSystem.Object), fileSystem.Object, localRepository);
            sourceRepository.AddPackage(package);

            // Act 
            packageManager.InstallPackage("B");
            
            // Assert
            Assert.True(localRepository.Exists(package));
            batchProcessor.Verify();
        } 

        [Fact]
        public void InstallPackageDoesNotPerformWalkInfoCheckWhenPassingTheFlag()
        {
            // In this test, we simulate installing a solution-level package which depends on 
            // a project-level package. Under normal condition, this is disallowed by NuGet. 
            // However, if passing the 'ignoreWalkInfo' parameter with value of 'true', 
            // NuGet will happily accept that. Hence the installation will succeed.
            // This is used by the package restore mode.

            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var packageManager = new PackageManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);

            // A is solution-level package
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0.0",
                                                             tools: new string[] { "init.ps1"},
                                                             dependencies: new[] {
                                                                 new PackageDependency("C")
                                                             });

            IPackage packageC = PackageUtility.CreatePackage("C", "1.0.0-RC-1", content: new string[] { "a.txt" } );
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageC);

            // Act
            packageManager.InstallPackage(packageA, ignoreDependencies: false, allowPrereleaseVersions: true, ignoreWalkInfo: true);

            // Assert
            Assert.True(localRepository.Exists(packageA));
            Assert.True(localRepository.Exists(packageC));
        } */

        private PackageManager CreatePackageManager()
        {
            var projectSystem = new MockProjectSystem();
            return new PackageManager(
                new MockPackageRepository(),
                new DefaultPackagePathResolver(projectSystem),
                projectSystem,
                new MockSharedPackageRepository());
        }
    }
}
