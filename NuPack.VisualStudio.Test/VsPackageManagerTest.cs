using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test.Mocks;
using NuGet.VisualStudio;

namespace NuGet.Test.VisualStudio {
    [TestClass]
    public class VsPackageManagerTest {
        [TestMethod]
        public void InstallPackageInstallsIntoProjectAndPackageManager() {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(m => m.GetProjects()).Returns(Enumerable.Empty<Project>());

            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, new MockProjectSystem(), new MockPackageRepository());
            var packageManager = new VsPackageManager(solutionManager.Object, sourceRepository, projectSystem, localRepository);

            var package = PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" });
            sourceRepository.AddPackage(package);

            // Act
            packageManager.InstallPackage(projectManager, "foo", new Version("1.0"), ignoreDependencies: false, logger: NullLogger.Instance);

            // Assert
            Assert.IsTrue(packageManager.LocalRepository.Exists(package));
            Assert.IsTrue(projectManager.LocalRepository.Exists(package));
        }

        [TestMethod]
        public void InstallPackgeWithNullProjectManagerOnlyInstallsIntoPackageManager() {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(m => m.GetProjects()).Returns(Enumerable.Empty<Project>());

            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var packageManager = new VsPackageManager(solutionManager.Object, sourceRepository, projectSystem, localRepository);

            var package = PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" });
            sourceRepository.AddPackage(package);

            // Act
            packageManager.InstallPackage(null, "foo", new Version("1.0"), ignoreDependencies: false, logger: NullLogger.Instance);

            // Assert
            Assert.IsTrue(packageManager.LocalRepository.Exists(package));
        }

        [TestMethod]
        public void UninstallPackageWithMultipleProjectReferencesUninstallsFromTargetProjectButNotPackageManagerOrOtherProjects() {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(m => m.GetProjects()).Returns(Enumerable.Empty<Project>());

            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var package = PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" });
            localRepository.AddPackage(package);
            sourceRepository.AddPackage(package);

            IProjectManager projectWithPackage = CreateProjectManagerWithPackage(package);
            IProjectManager otherProjectWithPackage = CreateProjectManagerWithPackage(package);
            var packageManager = new MockVsPackageManager(solutionManager.Object, sourceRepository, projectSystem, localRepository, new[] { otherProjectWithPackage });

            // Act
            packageManager.UninstallPackage(projectWithPackage, "foo", version: null, forceRemove: false, removeDependencies: false, logger: NullLogger.Instance);

            // Assert
            Assert.IsFalse(projectWithPackage.LocalRepository.Exists(package));
            Assert.IsTrue(otherProjectWithPackage.LocalRepository.Exists(package));
            Assert.IsTrue(packageManager.LocalRepository.Exists(package));
        }

        [TestMethod]
        public void UninstallPackageWithOneProjectReferencesUninstallsFromTargetProjectAndPackageManager() {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(m => m.GetProjects()).Returns(Enumerable.Empty<Project>());

            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var package = PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" });
            localRepository.AddPackage(package);
            sourceRepository.AddPackage(package);

            IProjectManager projectWithPackage = CreateProjectManagerWithPackage(package);
            var packageManager = new MockVsPackageManager(solutionManager.Object, sourceRepository, projectSystem, localRepository, Enumerable.Empty<IProjectManager>());

            // Act
            packageManager.UninstallPackage(projectWithPackage, "foo", version: null, forceRemove: false, removeDependencies: false, logger: NullLogger.Instance);

            // Assert
            Assert.IsFalse(projectWithPackage.LocalRepository.Exists(package));
            Assert.IsFalse(packageManager.LocalRepository.Exists(package));
        }

        private IProjectManager CreateProjectManagerWithPackage(IPackage package) {
            MockPackageRepository localRepository = new MockPackageRepository();
            localRepository.AddPackage(package);
            return new ProjectManager(new MockPackageRepository(),
                                      new DefaultPackagePathResolver(new MockProjectSystem()),
                                      new MockProjectSystem(),
                                      localRepository);
        }

        private class MockVsPackageManager : VsPackageManager {
            private IEnumerable<IProjectManager> _projectManagers;

            public MockVsPackageManager(ISolutionManager solutionManager,
                                        IPackageRepository sourceRepository,
                                        IFileSystem fileSystem,
                                        IPackageRepository localRepository,
                                        IEnumerable<IProjectManager> projectManagers) :
                base(solutionManager, sourceRepository, fileSystem, localRepository) {
                _projectManagers = projectManagers;
            }

            protected override IEnumerable<IProjectManager> ProjectManagers {
                get {
                    return _projectManagers;
                }
            }
        }
    }
}
