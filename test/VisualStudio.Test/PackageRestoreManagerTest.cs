using System;
using System.IO;
using System.Threading;
using EnvDTE;
using Moq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    using PackageUtility = NuGet.Test.PackageUtility;

    public class PackageRestoreManagerTest : IDisposable
    {
        private static readonly string _testRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        [Fact]
        public void CallingCheckForMissingPackagesRaisesThePackagesMissingStatusChangedEventWithTrueValue()
        {
            // Scenario:
            // Project's packages.config specifies: A[1.0], B[1.2-alpha]
            // The solution's packages folder contains only A[1.0]

            // Arrange
            string tempSolutionPath = "x:\\project1";

            var project = new Mock<Project>();

            // setup SolutionManager
            var solutionManager = new Mock<ISolutionManager>(MockBehavior.Strict);
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(true);
            solutionManager.Setup(p => p.SolutionDirectory).Returns(tempSolutionPath);
            solutionManager.Setup(p => p.GetProjects()).Returns(new[] { project.Object });

            // setup VsPackageManager
            var projectFileSystem = new MockFileSystem();
            projectFileSystem.AddFile("packages.config", 
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <packages>
                    <package id=""A"" version=""1.0.0.0"" />
                    <package id=""B"" version=""1.2-alpha"" />
                </packages>");

            var packageReferenceRepository = new PackageReferenceRepository(projectFileSystem, projectName: null, sourceRepository: new Mock<ISharedPackageRepository>().Object);
            var projectManager = new Mock<IProjectManager>();
            projectManager.Setup(p => p.LocalRepository).Returns(packageReferenceRepository);

            var localRepository = new MockPackageRepository();
            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            
            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.GetProjectManager(project.Object)).Returns(projectManager.Object);
            packageManager.Setup(p => p.LocalRepository).Returns(localRepository);

            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(p => p.CreatePackageManager()).Returns(packageManager.Object);

            var packageRestore = CreateInstance(
                solutionManager: solutionManager.Object,
                packageManagerFactory: packageManagerFactory.Object);

            bool? packagesMissing = null;
            packageRestore.PackagesMissingStatusChanged += (o, e) =>
                {
                    packagesMissing = e.PackagesMissing;
                };
            
            // Act
            packageRestore.CheckForMissingPackages();

            // Assert
            Assert.Equal(true, packagesMissing);
        }

        [Fact]
        public void CallingCheckForMissingPackagesRaisesThePackagesMissingStatusChangedEventWithTrueValueIfPackagesFolderIsMissing()
        {
            // Scenario:
            // Project's packages.config specifies: A[1.0]
            // The solution's packages folder doesn't contain any packages

            // Arrange
            string tempSolutionPath = "x:\\project1";

            var project = new Mock<Project>();

            // setup SolutionManager
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(true);
            solutionManager.Setup(p => p.SolutionDirectory).Returns(tempSolutionPath);
            solutionManager.Setup(p => p.GetProjects()).Returns(new[] { project.Object });

            // setup VsPackageManager
            var projectFileSystem = new MockFileSystem();
            projectFileSystem.AddFile("packages.config", 
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <packages>
                    <package id=""A"" version=""1.0.0.0"" />
                </packages>");

            var packageReferenceRepository = new PackageReferenceRepository(projectFileSystem, projectName: null, sourceRepository: new Mock<ISharedPackageRepository>().Object);
            var projectManager = new Mock<IProjectManager>();
            projectManager.Setup(p => p.LocalRepository).Returns(packageReferenceRepository);

            var localRepository = new MockPackageRepository();

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.GetProjectManager(project.Object)).Returns(projectManager.Object);
            packageManager.Setup(p => p.LocalRepository).Returns(localRepository);

            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(p => p.CreatePackageManager()).Returns(packageManager.Object);

            var packageRestore = CreateInstance(
                solutionManager: solutionManager.Object,
                packageManagerFactory: packageManagerFactory.Object);

            bool? packagesMissing = null;
            packageRestore.PackagesMissingStatusChanged += (o, e) =>
            {
                packagesMissing = e.PackagesMissing;
            };

            // Act
            packageRestore.CheckForMissingPackages();

            // Assert
            Assert.Equal(true, packagesMissing);
        }

        [Fact]
        public void CallingCheckForMissingPackagesRaisesThePackagesMissingStatusChangedEventWithFalseValue()
        {
            // Scenario:
            // Project's packages.config specifies: A[1.0], B[1.2-alpha]
            // The solution's packages folder contains only A[1.0], B[1.2-alpha]

            // Arrange
            string tempSolutionPath = "x:\\project1";

            var project = new Mock<Project>();

            // setup SolutionManager
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(true);
            solutionManager.Setup(p => p.SolutionDirectory).Returns(tempSolutionPath);
            solutionManager.Setup(p => p.GetProjects()).Returns(new[] { project.Object });

            // setup file system
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(p => p.DirectoryExists(".nuget")).Returns(true);
            fileSystem.Setup(p => p.FileExists(".nuget\\NuGet.exe")).Returns(true);
            fileSystem.Setup(p => p.FileExists(".nuget\\NuGet.targets")).Returns(true);

            // setup VsPackageManager
            var projectFileSystem = new MockFileSystem();
            projectFileSystem.AddFile("packages.config", 
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <packages>
                    <package id=""A"" version=""1.0.0.0"" />
                    <package id=""B"" version=""1.2-alpha"" />
                </packages>");

            var packageReferenceRepository = new PackageReferenceRepository(projectFileSystem, projectName: null, sourceRepository: new Mock<ISharedPackageRepository>().Object);
            var projectManager = new Mock<IProjectManager>();
            projectManager.Setup(p => p.LocalRepository).Returns(packageReferenceRepository);

            var localRepository = new MockPackageRepository();
            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            localRepository.AddPackage(PackageUtility.CreatePackage("B", "1.2-alpha"));

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.GetProjectManager(project.Object)).Returns(projectManager.Object);
            packageManager.Setup(p => p.LocalRepository).Returns(localRepository);

            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(p => p.CreatePackageManager()).Returns(packageManager.Object);

            var packageRestore = CreateInstance(
                solutionManager: solutionManager.Object,
                packageManagerFactory: packageManagerFactory.Object);

            bool? packagesMissing = null;
            packageRestore.PackagesMissingStatusChanged += (o, e) =>
            {
                packagesMissing = e.PackagesMissing;
            };

            // Act
            packageRestore.CheckForMissingPackages();

            // Assert
            Assert.Equal(false, packagesMissing);
        }

        [Fact]
        public void CallingRestoreMissingPackagesMethodInstallMissingPackages()
        {
            // Scenario:
            // Project's packages.config specifies: A[1.0]
            // The solution's packages folder contains only A[1.0], B[1.2-alpha], C[2.0-RC1]
            // Call RestoreMissingPackages() will install B[1.2-alpha] and C[2.0-RC1] into the solution

            // Arrange
            string tempSolutionPath = "x:\\project1";

            var project = new Mock<Project>();

            // setup SolutionManager
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(true);
            solutionManager.Setup(p => p.SolutionDirectory).Returns(tempSolutionPath);
            solutionManager.Setup(p => p.GetProjects()).Returns(new[] { project.Object });

            // setup file system
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(p => p.DirectoryExists(".nuget")).Returns(true);
            fileSystem.Setup(p => p.FileExists(".nuget\\NuGet.exe")).Returns(true);
            fileSystem.Setup(p => p.FileExists(".nuget\\NuGet.targets")).Returns(true);
            
            // setup VsPackageManager
            string tempFile = @"<?xml version=""1.0"" encoding=""utf-8""?>
                <packages>
                    <package id=""A"" version=""1.0.0.0"" />
                    <package id=""B"" version=""1.2-alpha"" />
                    <package id=""C"" version=""2.0-RC1"" />
                </packages>";

            var projectFileSystem = new MockFileSystem();
            projectFileSystem.AddFile("packages.config", tempFile);

            var packageReferenceRepository = new PackageReferenceRepository(projectFileSystem, projectName: null, sourceRepository: new Mock<ISharedPackageRepository>().Object);
            var projectManager = new Mock<IProjectManager>();
            projectManager.Setup(p => p.LocalRepository).Returns(packageReferenceRepository);

            var localRepository = new MockPackageRepository();
            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.GetProjectManager(project.Object)).Returns(projectManager.Object);
            packageManager.Setup(p => p.LocalRepository).Returns(localRepository);

            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(p => p.CreatePackageManager()).Returns(packageManager.Object);
            packageManagerFactory.Setup(p => p.CreatePackageManagerWithAllPackageSources()).Returns(packageManager.Object);

            var packageRestore = CreateInstance(
                solutionManager: solutionManager.Object,
                packageManagerFactory: packageManagerFactory.Object);

            Exception exception = null;
            var manualEvent = new ManualResetEventSlim();

            // Act
            packageRestore.RestoreMissingPackages().ContinueWith(
                task => 
                {
                    if (task.IsFaulted)
                    {
                        exception = task.Exception;
                        manualEvent.Set();
                        return;
                    }

                    try
                    {
                        // Assert
                        packageManager.Verify(p => p.InstallPackage("A", new SemanticVersion("1.0.0.0"), true, true), Times.Never());
                        packageManager.Verify(p => p.InstallPackage("B", new SemanticVersion("1.2-alpha"), true, true), Times.Once());
                        packageManager.Verify(p => p.InstallPackage("C", new SemanticVersion("2.0-RC1"), true, true), Times.Once());
                    }
                    catch (Exception testException)
                    {
                        exception = testException;
                    }
                    finally
                    {
                        manualEvent.Set();
                    }
                });

            manualEvent.Wait();

            Assert.Null(exception);
        }

        private PackageRestoreManager CreateInstance(
            ISolutionManager solutionManager = null, 
            IVsPackageManagerFactory packageManagerFactory = null)
        {
            if (solutionManager == null)
            {
                solutionManager = new Mock<ISolutionManager>().Object;
            }

            if (packageManagerFactory == null)
            {
                packageManagerFactory = new Mock<IVsPackageManagerFactory>().Object;
            }

            return new PackageRestoreManager(
                solutionManager,
                packageManagerFactory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_testRoot, recursive: true);
            }
            catch { }
        }
    }
}