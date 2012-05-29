using System;
using System.Linq;
using EnvDTE;
using Moq;
using NuGet.Test;
using NuGet.Test.Mocks;
using Xunit;
using Xunit.Extensions;

namespace NuGet.VisualStudio.Test
{
    using PackageUtility = NuGet.Test.PackageUtility;
    using System.Runtime.Versioning;

    public class VsPackageManagerTest
    {
        [Fact]
        public void InstallPackageInstallsIntoProjectAndPackageManager()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, new MockProjectSystem(), new MockPackageRepository());
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, projectSystem, localRepository, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object);

            var package = PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" });
            sourceRepository.AddPackage(package);

            // Act
            packageManager.InstallPackage(projectManager, "foo", new SemanticVersion("1.0"), ignoreDependencies: false, allowPrereleaseVersions: false, logger: NullLogger.Instance);

            // Assert
            Assert.True(packageManager.LocalRepository.Exists(package));
            Assert.True(projectManager.LocalRepository.Exists(package));
        }

        [Fact]
        public void InstallPackageUsesProjectTargetFramework()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework, Version=4.5"));
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, projectSystem, new MockPackageRepository());
            var packageManager = new VsPackageManager(
                TestUtils.GetSolutionManager(), 
                sourceRepository, 
                new Mock<IFileSystemProvider>().Object, 
                projectSystem, 
                localRepository, 
                new Mock<IRecentPackageRepository>().Object, 
                new Mock<VsPackageInstallerEvents>().Object);

            var dependencySets = new PackageDependencySet[] {
                new PackageDependencySet(
                    new FrameworkName(".NETFramework, Version=4.5"),
                    new [] { new PackageDependency("B") }),

                new PackageDependencySet(
                    new FrameworkName(".NETFramework, Version=4.0"),
                    new [] { new PackageDependency("C") })
            };

            var package = PackageUtility.CreatePackage2(
                "foo", 
                "1.0", 
                new[] { "hello" },
                dependencySets: dependencySets);

            var packageB = PackageUtility.CreatePackage("B", "2.0", new[] { "good morning" });
            var packageC = PackageUtility.CreatePackage("C", "2.0", new[] { "good morning" });
            sourceRepository.AddPackage(package);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageC);

            // Act
            packageManager.InstallPackage(projectManager, "foo", new SemanticVersion("1.0"), ignoreDependencies: false, allowPrereleaseVersions: false, logger: NullLogger.Instance);

            // Assert
            Assert.True(packageManager.LocalRepository.Exists(package));
            Assert.True(packageManager.LocalRepository.Exists(packageB));
            Assert.False(packageManager.LocalRepository.Exists(packageC));
            Assert.True(projectManager.LocalRepository.Exists(package));
            Assert.True(projectManager.LocalRepository.Exists(packageB));
        }

        /// <summary>
        /// This test is exactly the same as the previous one but calls a different overload
        /// of VsPackageManager.
        /// </summary>
        [Fact]
        public void InstallPackageUsesProjectTargetFramework2()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework, Version=4.5"));
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, projectSystem, new MockPackageRepository());
            var packageManager = new VsPackageManager(
                TestUtils.GetSolutionManager(),
                sourceRepository,
                new Mock<IFileSystemProvider>().Object,
                projectSystem,
                localRepository,
                new Mock<IRecentPackageRepository>().Object,
                new Mock<VsPackageInstallerEvents>().Object);

            var dependencySets = new PackageDependencySet[] {
                new PackageDependencySet(
                    new FrameworkName(".NETFramework, Version=4.5"),
                    new [] { new PackageDependency("B") }),

                new PackageDependencySet(
                    new FrameworkName(".NETFramework, Version=4.0"),
                    new [] { new PackageDependency("C") })
            };

            var package = PackageUtility.CreatePackage2(
                "foo",
                "1.0",
                new[] { "hello" },
                dependencySets: dependencySets);

            var packageB = PackageUtility.CreatePackage("B", "2.0", new[] { "good morning" });
            var packageC = PackageUtility.CreatePackage("C", "2.0", new[] { "good morning" });
            sourceRepository.AddPackage(package);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageC);

            // Act
            packageManager.InstallPackage(
                projectManager, 
                "foo", 
                new SemanticVersion("1.0"), 
                ignoreDependencies: false, 
                allowPrereleaseVersions: false,
                skipAssemblyReferences: false,
                logger: NullLogger.Instance);

            // Assert
            Assert.True(packageManager.LocalRepository.Exists(package));
            Assert.True(packageManager.LocalRepository.Exists(packageB));
            Assert.False(packageManager.LocalRepository.Exists(packageC));
            Assert.True(projectManager.LocalRepository.Exists(package));
            Assert.True(projectManager.LocalRepository.Exists(packageB));
        }

        [Fact]
        public void InstallPackageWithSkipAssemblyReferencesInstallsIntoProjectAndPackageManager()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, projectSystem, new MockPackageRepository());
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, projectSystem, localRepository, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object);

            var package = PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" }, new [] { "assembly.dll" });
            sourceRepository.AddPackage(package);

            // Act
            packageManager.InstallPackage(projectManager, "foo", new SemanticVersion("1.0"), ignoreDependencies: false, allowPrereleaseVersions: false, skipAssemblyReferences: true, logger: NullLogger.Instance);

            // Assert
            Assert.True(packageManager.LocalRepository.Exists(package));
            Assert.True(projectManager.LocalRepository.Exists(package));
            Assert.Equal(0, projectSystem.References.Count);
        }

        [Fact]
        public void InstallPackageWithSkipAssemblyReferencesFalseInstallsIntoProjectAndPackageManager()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, projectSystem, new MockPackageRepository());
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, projectSystem, localRepository, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object);

            var package = PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" }, new[] { "assembly.dll" });
            sourceRepository.AddPackage(package);

            // Act
            packageManager.InstallPackage(projectManager, "foo", new SemanticVersion("1.0"), ignoreDependencies: false, allowPrereleaseVersions: false, skipAssemblyReferences: false, logger: NullLogger.Instance);

            // Assert
            Assert.True(packageManager.LocalRepository.Exists(package));
            Assert.True(projectManager.LocalRepository.Exists(package));
            Assert.Equal(1, projectSystem.References.Count);
        }

        [Fact]
        public void InstallPackageWithOperationsExecuteAllOperations()
        {
            // Arrange 
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, new MockProjectSystem(), new MockPackageRepository());
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, projectSystem, localRepository, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object);

            var package = PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" }, dependencies: new PackageDependency[] { new PackageDependency("bar") });
            sourceRepository.AddPackage(package);

            var package2 = PackageUtility.CreatePackage("bar", "2.0", new[] { "world" });
            sourceRepository.AddPackage(package2);

            var package3 = PackageUtility.CreatePackage("awesome", "1.0", new[] { "candy" });
            localRepository.AddPackage(package3);

            var operations = new PackageOperation[] {  
                 new PackageOperation(package, PackageAction.Install), 
                 new PackageOperation(package2, PackageAction.Install), 
                 new PackageOperation(package3, PackageAction.Uninstall) 
             };

            // Act 
            packageManager.InstallPackage(projectManager, package, operations, ignoreDependencies: false, allowPrereleaseVersions: false, logger: NullLogger.Instance);

            // Assert 
            Assert.True(packageManager.LocalRepository.Exists(package));
            Assert.True(packageManager.LocalRepository.Exists(package2));
            Assert.True(!packageManager.LocalRepository.Exists(package3));
            Assert.True(projectManager.LocalRepository.Exists(package));
            Assert.True(projectManager.LocalRepository.Exists(package2));
        }

        [Fact]
        public void InstallPackgeWithNullProjectManagerOnlyInstallsIntoPackageManager()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, projectSystem, localRepository, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object);

            var package = PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" });
            sourceRepository.AddPackage(package);

            // Act
            packageManager.InstallPackage((IProjectManager)null, "foo", new SemanticVersion("1.0"), ignoreDependencies: false, allowPrereleaseVersions: false, logger: NullLogger.Instance);

            // Assert
            Assert.True(packageManager.LocalRepository.Exists(package));
        }

        [Fact]
        public void UninstallProjectLevelPackageThrowsIfPackageIsReferenced()
        {
            // Arrange            
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            localRepository.Setup(m => m.IsReferenced("foo", It.IsAny<SemanticVersion>())).Returns(true);
            var sourceRepository = new MockPackageRepository();
            var fileSystem = new MockFileSystem();
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            var package = PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" });
            localRepository.Object.AddPackage(package);
            sourceRepository.AddPackage(package);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, fileSystem, localRepository.Object, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object);
            var projectManager = new ProjectManager(localRepository.Object, pathResolver, new MockProjectSystem(), new MockPackageRepository());

            // Act
            ExceptionAssert.Throws<InvalidOperationException>(() => packageManager.UninstallPackage(projectManager, "foo", version: null, forceRemove: false, removeDependencies: false, logger: NullLogger.Instance), 
                @"Unable to find package 'foo' in 'x:\MockFileSystem'.");
        }

        [Fact]
        public void UninstallProjectLevelPackageWithNoProjectManagerThrows()
        {
            // Arrange            
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            localRepository.Setup(m => m.IsReferenced("foo", It.IsAny<SemanticVersion>())).Returns(true);
            var sourceRepository = new MockPackageRepository();
            var fileSystem = new MockFileSystem();
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            var package = PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" });
            localRepository.Object.AddPackage(package);
            sourceRepository.AddPackage(package);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, fileSystem, localRepository.Object, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object);

            // Act
            ExceptionAssert.Throws<InvalidOperationException>(() => packageManager.UninstallPackage(null, "foo", version: null, forceRemove: false, removeDependencies: false, logger: NullLogger.Instance), "No project was specified.");
        }

        [Fact]
        public void UninstallPackageRemovesPackageIfPackageIsNotReferenced()
        {
            // Arrange            
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            localRepository.Setup(m => m.IsReferenced("foo", It.IsAny<SemanticVersion>())).Returns(false);
            var sourceRepository = new MockPackageRepository();
            var fileSystem = new MockFileSystem();
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            var package = PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" });
            localRepository.Object.AddPackage(package);
            sourceRepository.AddPackage(package);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, fileSystem, localRepository.Object, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object);

            // Act
            packageManager.UninstallPackage(null, "foo", version: null, forceRemove: false, removeDependencies: false, logger: NullLogger.Instance);

            // Assert
            Assert.False(packageManager.LocalRepository.Exists(package));
        }

        [Fact]
        public void UpdatePackageRemovesPackageIfPackageIsNotReferenced()
        {
            // Arrange            
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            localRepository.Setup(m => m.IsReferenced("A", new SemanticVersion("1.0"))).Returns(false);
            var projectRepository = new MockProjectPackageRepository(localRepository.Object);
            var sourceRepository = new MockPackageRepository();
            var fileSystem = new MockFileSystem();
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            var A10 = PackageUtility.CreatePackage("A", "1.0", new[] { "hello" });
            var A20 = PackageUtility.CreatePackage("A", "2.0", new[] { "hello" });
            sourceRepository.AddPackage(A10);
            sourceRepository.AddPackage(A20);
            localRepository.Object.AddPackage(A10);
            localRepository.Object.AddPackage(A20);
            projectRepository.Add(A10);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, fileSystem, localRepository.Object, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object);
            var projectManager = new ProjectManager(localRepository.Object, pathResolver, new MockProjectSystem(), projectRepository);

            // Act
            packageManager.UpdatePackage(projectManager, "A", version: null, updateDependencies: true, allowPrereleaseVersions: false, logger: NullLogger.Instance);

            // Assert
            Assert.False(packageManager.LocalRepository.Exists(A10));
        }

        [Fact]
        public void UpdatePackageDoesNotRemovesPackageIfPackageIsReferenced()
        {
            // Arrange            
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            localRepository.Setup(m => m.IsReferenced("A", new SemanticVersion("1.0"))).Returns(true);
            var projectRepository = new MockProjectPackageRepository(localRepository.Object);
            var sourceRepository = new MockPackageRepository();
            var fileSystem = new MockFileSystem();
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            var A10 = PackageUtility.CreatePackage("A", "1.0", new[] { "hello" });
            var A20 = PackageUtility.CreatePackage("A", "2.0", new[] { "hello" });
            sourceRepository.AddPackage(A10);
            sourceRepository.AddPackage(A20);
            localRepository.Object.AddPackage(A10);
            localRepository.Object.AddPackage(A20);
            projectRepository.Add(A10);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, fileSystem, localRepository.Object, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object);
            var projectManager = new ProjectManager(localRepository.Object, pathResolver, new MockProjectSystem(), projectRepository);

            // Act
            packageManager.UpdatePackage(projectManager, "A", version: null, updateDependencies: true, allowPrereleaseVersions: false, logger: NullLogger.Instance);

            // Assert
            Assert.True(packageManager.LocalRepository.Exists(A10));
        }

        [Fact]
        public void UpdatePackageWithSharedDependency()
        {
            // Arrange            
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var projectRepository = new MockProjectPackageRepository(localRepository.Object);
            var sourceRepository = new MockPackageRepository();
            var fileSystem = new MockFileSystem();
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            // A1 -> B1
            // A2 -> B2
            // F1 -> G1
            // G1 -> B1
            var A10 = PackageUtility.CreatePackage("A", "1.0", new[] { "hello" }, dependencies: new[] { new PackageDependency("B", VersionUtility.ParseVersionSpec("1.0")) });
            var A20 = PackageUtility.CreatePackage("A", "2.0", new[] { "hello" }, dependencies: new[] { new PackageDependency("B", VersionUtility.ParseVersionSpec("2.0")) });
            var B10 = PackageUtility.CreatePackage("B", "1.0", new[] { "hello" });
            var B20 = PackageUtility.CreatePackage("B", "2.0", new[] { "hello" });
            var F10 = PackageUtility.CreatePackage("F", "1.0", new[] { "hello" }, dependencies: new[] { new PackageDependency("G", VersionUtility.ParseVersionSpec("1.0")) });
            var G10 = PackageUtility.CreatePackage("G", "1.0", new[] { "hello" }, dependencies: new[] { new PackageDependency("B", VersionUtility.ParseVersionSpec("1.0")) });
            sourceRepository.AddPackage(A10);
            sourceRepository.AddPackage(A20);
            sourceRepository.AddPackage(B10);
            sourceRepository.AddPackage(B20);
            sourceRepository.AddPackage(F10);
            sourceRepository.AddPackage(G10);
            localRepository.Object.AddPackage(A10);
            localRepository.Object.AddPackage(A20);
            localRepository.Object.AddPackage(B10);
            localRepository.Object.AddPackage(B20);
            localRepository.Object.AddPackage(F10);
            localRepository.Object.AddPackage(G10);
            projectRepository.Add(A10);
            projectRepository.Add(B10);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, fileSystem, localRepository.Object, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object);
            var projectManager = new ProjectManager(localRepository.Object, pathResolver, new MockProjectSystem(), projectRepository);

            // Act
            packageManager.UpdatePackage(projectManager, "A", version: null, updateDependencies: true, allowPrereleaseVersions: false, logger: NullLogger.Instance);

            // Assert
            Assert.False(packageManager.LocalRepository.Exists(A10));
            Assert.False(packageManager.LocalRepository.Exists(B10));
            Assert.True(packageManager.LocalRepository.Exists(A20));
            Assert.True(packageManager.LocalRepository.Exists(B20));
            Assert.True(packageManager.LocalRepository.Exists(F10));
            Assert.True(packageManager.LocalRepository.Exists(G10));
        }

        [Fact]
        public void UpdatePackageWithSameDependency()
        {
            // Arrange            
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var projectRepository = new MockProjectPackageRepository(localRepository.Object);
            var sourceRepository = new MockPackageRepository();
            var fileSystem = new MockFileSystem();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            // A1 -> B1
            // A2 -> B1
            var A10 = PackageUtility.CreatePackage("A", "1.0", assemblyReferences: new[] { "A1.dll" }, dependencies: new[] { new PackageDependency("B", VersionUtility.ParseVersionSpec("1.0")) });
            var A20 = PackageUtility.CreatePackage("A", "2.0", assemblyReferences: new[] { "A2.dll" }, dependencies: new[] { new PackageDependency("B", VersionUtility.ParseVersionSpec("1.0")) });
            var B10 = PackageUtility.CreatePackage("B", "1.0", assemblyReferences: new[] { "B1.dll" });
            sourceRepository.AddPackage(A10);
            sourceRepository.AddPackage(A20);
            sourceRepository.AddPackage(B10);
            localRepository.Object.AddPackage(A10);
            localRepository.Object.AddPackage(A20);
            localRepository.Object.AddPackage(B10);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, fileSystem, localRepository.Object, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object);
            var projectManager = new ProjectManager(localRepository.Object, pathResolver, projectSystem, projectRepository);
            projectManager.AddPackageReference("A", new SemanticVersion("1.0"));

            // Act
            packageManager.UpdatePackage(projectManager, "A", version: null, updateDependencies: true, allowPrereleaseVersions: false, logger: NullLogger.Instance);

            // Assert
            Assert.False(packageManager.LocalRepository.Exists(A10));
            Assert.False(projectSystem.ReferenceExists("A1.dll"));
            Assert.True(packageManager.LocalRepository.Exists(B10));
            Assert.True(projectSystem.ReferenceExists("B1.dll"));
            Assert.True(packageManager.LocalRepository.Exists(A20));
            Assert.True(projectSystem.ReferenceExists("A2.dll"));
        }

        [Fact]
        public void UpdatePackageNewVersionOfPackageHasLessDependencies()
        {
            // Arrange            
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var projectRepository = new MockProjectPackageRepository(localRepository.Object);
            var sourceRepository = new MockPackageRepository();
            var fileSystem = new MockFileSystem();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            // A1 -> B1
            // A2
            var A10 = PackageUtility.CreatePackage("A", "1.0", assemblyReferences: new[] { "A1.dll" }, dependencies: new[] { new PackageDependency("B", VersionUtility.ParseVersionSpec("1.0")) });
            var A20 = PackageUtility.CreatePackage("A", "2.0", assemblyReferences: new[] { "A2.dll" });
            var B10 = PackageUtility.CreatePackage("B", "1.0", assemblyReferences: new[] { "B1.dll" });
            sourceRepository.AddPackage(A10);
            sourceRepository.AddPackage(A20);
            sourceRepository.AddPackage(B10);
            localRepository.Object.AddPackage(A10);
            localRepository.Object.AddPackage(A20);
            localRepository.Object.AddPackage(B10);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, fileSystem, localRepository.Object, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object);
            var projectManager = new ProjectManager(localRepository.Object, pathResolver, projectSystem, projectRepository);
            projectManager.AddPackageReference("A", new SemanticVersion("1.0"));

            // Act
            packageManager.UpdatePackage(projectManager, "A", version: null, updateDependencies: true, allowPrereleaseVersions: false, logger: NullLogger.Instance);

            // Assert
            Assert.False(packageManager.LocalRepository.Exists(A10));
            Assert.False(projectSystem.ReferenceExists("A1.dll"));
            Assert.False(packageManager.LocalRepository.Exists(B10));
            Assert.False(projectSystem.ReferenceExists("B1.dll"));
            Assert.True(packageManager.LocalRepository.Exists(A20));
            Assert.True(projectSystem.ReferenceExists("A2.dll"));
        }

        [Fact]
        public void UpdatePackageWithMultipleSharedDependencies()
        {
            // Arrange            
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var projectRepository = new MockProjectPackageRepository(localRepository.Object);
            var sourceRepository = new MockPackageRepository();
            var fileSystem = new MockFileSystem();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            // A1 -> B1, C1
            // A2 -> B1
            var A10 = PackageUtility.CreatePackage("A", "1.0", assemblyReferences: new[] { "A1.dll" }, dependencies: new[] { 
                new PackageDependency("B", VersionUtility.ParseVersionSpec("1.0")),
                new PackageDependency("C", VersionUtility.ParseVersionSpec("1.0")),
            });
            var A20 = PackageUtility.CreatePackage("A", "2.0", assemblyReferences: new[] { "A2.dll" }, dependencies: new[] { 
                new PackageDependency("B", VersionUtility.ParseVersionSpec("1.0"))
            });
            var B10 = PackageUtility.CreatePackage("B", "1.0", assemblyReferences: new[] { "B1.dll" });
            var C10 = PackageUtility.CreatePackage("C", "1.0", assemblyReferences: new[] { "C1.dll" });
            sourceRepository.AddPackage(A10);
            sourceRepository.AddPackage(A20);
            sourceRepository.AddPackage(B10);
            sourceRepository.AddPackage(C10);
            localRepository.Object.AddPackage(A10);
            localRepository.Object.AddPackage(A20);
            localRepository.Object.AddPackage(B10);
            localRepository.Object.AddPackage(C10);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, fileSystem, localRepository.Object, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object);
            var projectManager = new ProjectManager(localRepository.Object, pathResolver, projectSystem, projectRepository);
            projectManager.AddPackageReference("A", new SemanticVersion("1.0"));

            // Act
            packageManager.UpdatePackage(projectManager, "A", version: null, updateDependencies: true, allowPrereleaseVersions: false, logger: NullLogger.Instance);

            // Assert
            Assert.False(packageManager.LocalRepository.Exists(A10));
            Assert.False(projectSystem.ReferenceExists("A1.dll"));
            Assert.False(packageManager.LocalRepository.Exists(C10));
            Assert.False(projectSystem.ReferenceExists("C1.dll"));
            Assert.True(packageManager.LocalRepository.Exists(B10));
            Assert.True(projectSystem.ReferenceExists("B1.dll"));
            Assert.True(packageManager.LocalRepository.Exists(A20));
            Assert.True(projectSystem.ReferenceExists("A2.dll"));
        }

        [Fact]
        public void UpdatePackageWithNoProjectsInstallsAtSolutionLevel()
        {
            // Arrange 
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;

            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, projectSystem, localRepository, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object);

            var packageToUpdate = PackageUtility.CreatePackage("foo", "0.9");
            localRepository.AddPackage(packageToUpdate);
            
            var package = PackageUtility.CreatePackage("foo", "1.0", dependencies: new PackageDependency[] { new PackageDependency("bar") });
            sourceRepository.AddPackage(package);

            var package2 = PackageUtility.CreatePackage("bar", "2.0");
            sourceRepository.AddPackage(package2);

            var package3 = PackageUtility.CreatePackage("awesome", "1.0");
            localRepository.AddPackage(package3);

            var operations = new PackageOperation[] {  
                 new PackageOperation(package, PackageAction.Install), 
                 new PackageOperation(package2, PackageAction.Install), 
                 new PackageOperation(package3, PackageAction.Uninstall) 
             };

            // Act 
            packageManager.UpdatePackage(Enumerable.Empty<Project>(),
                                         package,
                                         operations,
                                         updateDependencies: true,
                                         allowPrereleaseVersions: false,
                                         logger: NullLogger.Instance,
                                         packageOperationEventListener: null);

            // Assert 
            Assert.True(packageManager.LocalRepository.Exists(package));
            Assert.True(packageManager.LocalRepository.Exists(package2));
            Assert.True(!packageManager.LocalRepository.Exists(package3));
        }

        [Fact]
        public void InstallPackageDoesNotInstallPackageWithIndirectDependencyThatIsPrerelease()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, projectSystem, localRepository, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object);

            var package = PackageUtility.CreatePackage("foo", "1.0.0", dependencies: new[] { new PackageDependency("bar") });
            sourceRepository.AddPackage(package);

            var versionSpec = VersionUtility.ParseVersionSpec("[0.6, 1.0)");
            var package2 = PackageUtility.CreatePackage("bar", "2.0.0", dependencies: new[] { new PackageDependency("qux", versionSpec) });
            sourceRepository.AddPackage(package2);

            var package3A = PackageUtility.CreatePackage("qux", "1.0-alpha");
            var package3B = PackageUtility.CreatePackage("qux", "1.0-beta");
            var package3 = PackageUtility.CreatePackage("qux", "1.0");
            sourceRepository.AddPackage(package3);
            sourceRepository.AddPackage(package3A);
            sourceRepository.AddPackage(package3B);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => packageManager.InstallPackage(package, ignoreDependencies: false, allowPrereleaseVersions: false),
                "Unable to resolve dependency 'qux (≥ 0.6 && < 1.0)'.");
        }

        [Fact]
        public void InstallPackageInstallsIndirectPrereleaseDependency()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, projectSystem, localRepository, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object);

            var package = PackageUtility.CreatePackage("foo", "1.0.0", dependencies: new[] { new PackageDependency("bar") });
            sourceRepository.AddPackage(package);

            var versionSpec = VersionUtility.ParseVersionSpec("[0.6, 1.0)");
            var package2 = PackageUtility.CreatePackage("bar", "2.0.0", dependencies: new[] { new PackageDependency("qux", versionSpec) });
            sourceRepository.AddPackage(package2);

            var package3A = PackageUtility.CreatePackage("qux", "1.0-alpha");
            var package3B = PackageUtility.CreatePackage("qux", "1.0-beta");
            var package3 = PackageUtility.CreatePackage("qux", "1.0");
            localRepository.AddPackage(package3);
            localRepository.AddPackage(package3A);
            localRepository.AddPackage(package3B);

            // Act
            packageManager.InstallPackage(package, ignoreDependencies: false, allowPrereleaseVersions: true);

            // Assert
            Assert.True(packageManager.LocalRepository.Exists(package));
            Assert.True(packageManager.LocalRepository.Exists(package2));
            Assert.True(packageManager.LocalRepository.Exists(package3B));
        }

        [Fact]
        public void UpdatePackageUpdatesToTheHighestReleasePackageIfPrereleaseFlagIsSetToFalse()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, projectSystem, localRepository, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object);

            var package3_1A = PackageUtility.CreatePackage("qux", "1.0-alpha");
            var package3_1B = PackageUtility.CreatePackage("qux", "1.1-alpha");
            var package3_10 = PackageUtility.CreatePackage("qux", "1.0");
            var package3_09 = PackageUtility.CreatePackage("qux", "0.9");
            localRepository.AddPackage(package3_10);
            localRepository.AddPackage(package3_1A);
            localRepository.AddPackage(package3_1B);
            localRepository.AddPackage(package3_09);

            // Act
            packageManager.InstallPackage(package3_09, ignoreDependencies: false, allowPrereleaseVersions: false);
            Assert.True(packageManager.LocalRepository.Exists(package3_09));
            packageManager.UpdatePackage("qux", updateDependencies: true, allowPrereleaseVersions: false);

            // Assert
            Assert.True(packageManager.LocalRepository.Exists(package3_10));
        }

        [Fact]
        public void UpdatePackageUpdatesToTheHighestPackageIfPrereleaseFlagIsSetToTrue()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, projectSystem, localRepository, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object);

            var package3_10A = PackageUtility.CreatePackage("qux", "1.0-alpha");
            var package3_11B = PackageUtility.CreatePackage("qux", "1.1-alpha");
            var package3_10 = PackageUtility.CreatePackage("qux", "1.0");
            var package3_09 = PackageUtility.CreatePackage("qux", "0.9");
            localRepository.AddPackage(package3_10);
            localRepository.AddPackage(package3_10A);
            localRepository.AddPackage(package3_11B);
            localRepository.AddPackage(package3_09);

            // Act
            packageManager.InstallPackage(package3_09, ignoreDependencies: false, allowPrereleaseVersions: true);
            Assert.True(packageManager.LocalRepository.Exists(package3_09));
            packageManager.UpdatePackage("qux", updateDependencies: true, allowPrereleaseVersions: true);

            // Assert
            Assert.True(packageManager.LocalRepository.Exists(package3_11B));
        }

        [Fact]
        public void UpdatePackageShowWarningIfThereIsOrphanedPackageUnderPackagesFolder()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            
            var packageManager = new VsPackageManager(
                TestUtils.GetSolutionManager(), 
                sourceRepository, 
                new Mock<IFileSystemProvider>().Object, 
                projectSystem, 
                localRepository, 
                new Mock<IRecentPackageRepository>().Object, 
                new Mock<VsPackageInstallerEvents>().Object);

            var orphanPackage = PackageUtility.CreatePackage("orphan", "1.0", new [] { "content" });
            localRepository.AddPackage(orphanPackage);

            var logger = new Mock<ILogger>();
            logger.Setup(l => l.Log(MessageLevel.Warning, "'orphan' was not installed in any project. Update failed.")).Verifiable();

            // Act
            packageManager.UpdatePackages(updateDependencies: true, allowPrereleaseVersions: true, logger: logger.Object, eventListener: new Mock<IPackageOperationEventListener>().Object);

            // Assert
            logger.Verify();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void InstallMetaPackageDoesNotAddEntryToSolutionPackagesConfig(bool allowPrerelease)
        {
            // Arrange
            // Source repository has A -> B, where B is a project-level package and A is a meta-package.
            // We want to make sure A is added to the packages.config of the project, and NOT packages.config of the solution
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            
            var sourceRepository = new MockPackageRepository();
            var packageB = PackageUtility.CreatePackage("B", "1.0", new string[] {"hello.txt"});
            var packageA = PackageUtility.CreatePackage("A", "2.0", dependencies: new [] { new PackageDependency("B")});
            sourceRepository.Add(packageA);
            sourceRepository.Add(packageB);

            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);

            var packageManager = new VsPackageManager(
                TestUtils.GetSolutionManager(),
                sourceRepository,
                new Mock<IFileSystemProvider>().Object,
                projectSystem,
                localRepository.Object,
                new Mock<IRecentPackageRepository>().Object,
                new Mock<VsPackageInstallerEvents>().Object);

            var projectRepository = new MockProjectPackageRepository(localRepository.Object);
            var projectManager = new ProjectManager(localRepository.Object, pathResolver, projectSystem, projectRepository);

            localRepository.Setup(r => r.IsReferenced(It.IsAny<string>(), It.IsAny<SemanticVersion>())).
                Returns((string id, SemanticVersion version) => projectRepository.Exists(id, version));

            // Act
            packageManager.InstallPackage(
                projectManager, 
                "A", 
                new SemanticVersion("2.0"), 
                ignoreDependencies: false, 
                allowPrereleaseVersions: allowPrerelease, 
                logger: null);

            // Assert
            Assert.True(projectRepository.Exists("A", new SemanticVersion("2.0")));
            Assert.True(projectRepository.Exists("B", new SemanticVersion("1.0")));

            // assert that packages.config for solution-level is not created.
            localRepository.Verify(r => r.AddPackageReferenceEntry(It.IsAny<string>(), It.IsAny<SemanticVersion>()), Times.Never());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void InstallMetaPackageDoesNotAddEntryToSolutionPackagesConfig2(bool skipAssemblyReferences)
        {
            // Arrange
            // Source repository has A -> B, where B is a project-level package and A is a meta-package.
            // We want to make sure A is added to the packages.config of the project, and NOT packages.config of the solution
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();

            var sourceRepository = new MockPackageRepository();
            var packageB = PackageUtility.CreatePackage("B", "1.0", new string[] { "hello.txt" });
            var packageA = PackageUtility.CreatePackage("A", "2.0", dependencies: new[] { new PackageDependency("B") });
            sourceRepository.Add(packageA);
            sourceRepository.Add(packageB);

            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);

            var packageManager = new VsPackageManager(
                TestUtils.GetSolutionManager(),
                sourceRepository,
                new Mock<IFileSystemProvider>().Object,
                projectSystem,
                localRepository.Object,
                new Mock<IRecentPackageRepository>().Object,
                new Mock<VsPackageInstallerEvents>().Object);

            var projectRepository = new MockProjectPackageRepository(localRepository.Object);
            var projectManager = new ProjectManager(localRepository.Object, pathResolver, projectSystem, projectRepository);

            localRepository.Setup(r => r.IsReferenced(It.IsAny<string>(), It.IsAny<SemanticVersion>())).
                Returns((string id, SemanticVersion version) => projectRepository.Exists(id, version));

            // Act
            packageManager.InstallPackage(
                projectManager, 
                "A", 
                new SemanticVersion("2.0"), 
                ignoreDependencies: false, 
                allowPrereleaseVersions: true, 
                skipAssemblyReferences: skipAssemblyReferences, 
                logger: null);

            // Assert
            Assert.True(projectRepository.Exists("A", new SemanticVersion("2.0")));
            Assert.True(projectRepository.Exists("B", new SemanticVersion("1.0")));

            // assert that packages.config for solution-level is not created.
            localRepository.Verify(r => r.AddPackageReferenceEntry(It.IsAny<string>(), It.IsAny<SemanticVersion>()), Times.Never());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void InstallSolutionLevelPackageAddEntryToSolutionPackagesConfig(bool allowPrerelease)
        {
            // Arrange
            // Source repository has A -> B, where B is a project-level package and A is a meta-package.
            // We want to make sure A is added to the packages.config of the project, and NOT packages.config of the solution
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();

            var sourceRepository = new MockPackageRepository();
            var packageA = PackageUtility.CreatePackage("A", "2.0", tools: new[] { "install.ps1" });
            sourceRepository.Add(packageA);

            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);

            var packageManager = new VsPackageManager(
                TestUtils.GetSolutionManager(),
                sourceRepository,
                new Mock<IFileSystemProvider>().Object,
                projectSystem,
                localRepository.Object,
                new Mock<IRecentPackageRepository>().Object,
                new Mock<VsPackageInstallerEvents>().Object);

            var projectRepository = new MockProjectPackageRepository(localRepository.Object);
            var projectManager = new ProjectManager(localRepository.Object, pathResolver, projectSystem, projectRepository);

            localRepository.Setup(r => r.IsReferenced(It.IsAny<string>(), It.IsAny<SemanticVersion>())).
                Returns((string id, SemanticVersion version) => projectRepository.Exists(id, version));

            // Act
            packageManager.InstallPackage(
                projectManager,
                "A",
                new SemanticVersion("2.0"),
                ignoreDependencies: false,
                allowPrereleaseVersions: allowPrerelease,
                logger: null);

            // Assert
            Assert.True(!projectRepository.Exists("A", new SemanticVersion("2.0")));

            // assert that packages.config for solution-level is created.
            localRepository.Verify(r => r.AddPackageReferenceEntry("A", new SemanticVersion("2.0")), Times.Once());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void InstallSolutionLevelPackageAddEntryToSolutionPackagesConfig2(bool skipAssemblyReferences)
        {
            // Arrange
            // Source repository has A -> B, where B is a project-level package and A is a meta-package.
            // We want to make sure A is added to the packages.config of the project, and NOT packages.config of the solution
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();

            var sourceRepository = new MockPackageRepository();
            var packageA = PackageUtility.CreatePackage("A", "2.0", tools: new[] { "install.ps1" });
            sourceRepository.Add(packageA);

            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);

            var packageManager = new VsPackageManager(
                TestUtils.GetSolutionManager(),
                sourceRepository,
                new Mock<IFileSystemProvider>().Object,
                projectSystem,
                localRepository.Object,
                new Mock<IRecentPackageRepository>().Object,
                new Mock<VsPackageInstallerEvents>().Object);

            var projectRepository = new MockProjectPackageRepository(localRepository.Object);
            var projectManager = new ProjectManager(localRepository.Object, pathResolver, projectSystem, projectRepository);

            localRepository.Setup(r => r.IsReferenced(It.IsAny<string>(), It.IsAny<SemanticVersion>())).
                Returns((string id, SemanticVersion version) => projectRepository.Exists(id, version));

            // Act
            packageManager.InstallPackage(
                projectManager,
                "A",
                new SemanticVersion("2.0"),
                ignoreDependencies: false,
                allowPrereleaseVersions: true,
                skipAssemblyReferences: skipAssemblyReferences,
                logger: null);

            // Assert
            Assert.True(!projectRepository.Exists("A", new SemanticVersion("2.0")));

            // assert that packages.config for solution-level is created.
            localRepository.Verify(r => r.AddPackageReferenceEntry("A", new SemanticVersion("2.0")), Times.Once());
        }

        [Fact]
        public void UpdatePackageEnsuresSatelliteReferencesAreCopiedToNewerVersion()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            localRepository.Setup(m => m.IsReferenced("A", new SemanticVersion("1.0"))).Returns(false);
            var projectRepository = new MockProjectPackageRepository(localRepository.Object);
            var sourceRepository = new MockPackageRepository();
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"A.1.0\.satellite\A.fr-Fr$1.0.ref");

            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            var A10 = PackageUtility.CreatePackage("A", "1.0", assemblyReferences: new[] { @"lib\net40\A.dll" });
            var A20 = PackageUtility.CreatePackage("A", "2.0", assemblyReferences: new[] { @"lib\net40\A.dll" });
            var ALoc10 = PackageUtility.CreatePackage("A.fr-FR", "1.0", language: "fr-FR",
                                            assemblyReferences: new[] { @"lib\net40\fr-FR\A.resources.dll" },
                                            dependencies: new[] { new PackageDependency("A") });
            sourceRepository.AddPackage(A20);
            localRepository.Object.AddPackage(A10);
            localRepository.Object.AddPackage(ALoc10);
            projectRepository.Add(A10);

            var projectA = Mock.Of<Project>();
            var projectB = Mock.Of<Project>();
            var packageManager = new Mock<VsPackageManager>(TestUtils.GetSolutionManager(), sourceRepository, 
                    Mock.Of<IFileSystemProvider>(), fileSystem, localRepository.Object, 
                    Mock.Of<IRecentPackageRepository>(), Mock.Of<VsPackageInstallerEvents>());
            packageManager.Setup(s => s.GetProjectManager(projectA)).Returns(new ProjectManager(localRepository.Object, pathResolver, new MockProjectSystem(), projectRepository));
            packageManager.Setup(s => s.GetProjectManager(projectB)).Returns(new ProjectManager(localRepository.Object, pathResolver, new MockProjectSystem(), projectRepository));
            
            var operations = new[] { 
                new PackageOperation(A10, PackageAction.Uninstall),
                new PackageOperation(A20, PackageAction.Install),
            };

            // Act
            packageManager.Object.UpdatePackage(new[] { projectA, projectB }, A20, operations, updateDependencies: false, allowPrereleaseVersions: false, 
                    logger: NullLogger.Instance, packageOperationEventListener: null);

            // Assert
            Assert.Contains(@"A.2.0\.satellite\A.fr-FR$1.0.ref", fileSystem.Paths.Keys);
            Assert.Contains(@"A.2.0\lib\net40\fr-FR\A.resources.dll", fileSystem.Paths.Keys);
        }

        [Fact]
        public void UpdatePackageAcrossProjectsEnsuresSatelliteReferencesAreCopiedToNewerVersion()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            localRepository.Setup(m => m.IsReferenced("A", new SemanticVersion("1.0"))).Returns(false);
            var projectRepository = new MockProjectPackageRepository(localRepository.Object);
            var sourceRepository = new MockPackageRepository();
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"A.1.0\.satellite\A.fr-Fr$1.0.ref");

            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            var A10 = PackageUtility.CreatePackage("A", "1.0", assemblyReferences: new[] { @"lib\net40\A.dll" });
            var A20 = PackageUtility.CreatePackage("A", "2.0", assemblyReferences: new[] { @"lib\net40\A.dll" });
            var ALoc10 = PackageUtility.CreatePackage("A.fr-FR", "1.0", language: "fr-FR",
                                            assemblyReferences: new[] { @"lib\net40\fr-FR\A.resources.dll" },
                                            dependencies: new[] { new PackageDependency("A") });
            sourceRepository.AddPackage(A20);
            localRepository.Object.AddPackage(A10);
            localRepository.Object.AddPackage(A20);
            localRepository.Object.AddPackage(ALoc10);
            projectRepository.Add(A10);

            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository,
                    Mock.Of<IFileSystemProvider>(), fileSystem, localRepository.Object,
                    Mock.Of<IRecentPackageRepository>(), Mock.Of<VsPackageInstallerEvents>());
            var projectManager = new ProjectManager(localRepository.Object, pathResolver, new MockProjectSystem(), projectRepository);

            // Act
            packageManager.UpdatePackage(projectManager, "A", version: null, updateDependencies: true, allowPrereleaseVersions: false, logger: NullLogger.Instance);

            // Assert
            Assert.Contains(@"A.2.0\.satellite\A.fr-FR$1.0.ref", fileSystem.Paths.Keys);
            Assert.Contains(@"A.2.0\lib\net40\fr-FR\A.resources.dll", fileSystem.Paths.Keys);
        }

        // This repository better simulates what happens when we're running the package manager in vs
        private class MockProjectPackageRepository : MockPackageRepository
        {
            private readonly IPackageRepository _parent;
            public MockProjectPackageRepository(IPackageRepository parent)
            {
                _parent = parent;
            }
            public override IQueryable<IPackage> GetPackages()
            {
                return base.GetPackages().Where(p => _parent.Exists(p));
            }
        }
    }
}