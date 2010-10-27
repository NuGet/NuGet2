using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test;
using NuGet.VisualStudio.Cmdlets;

namespace NuGet.VisualStudio.Test {
    [TestClass]
    public class GetPackageCmdletTest {
        [TestMethod]
        public void GetPackageReturnsAllPackagesFromActiveSourceWhenNoParametersAreSpecified() {
            // Arrange 
            var cmdlet = BuildCmdlet();

            // Act 
            var result = cmdlet.GetResults();

            // Assert
            Assert.AreEqual(3, result.Count());
        }

        [TestMethod]
        public void GetPackageReturnsAllPackagesFromSpecifiedSourceWhenNoParametersAreSpecified() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Source = "foo";

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("P1", result.First().Id);
            Assert.AreEqual("P6", result.Last().Id);
        }

        [TestMethod]
        public void GetPackageReturnsFilteredPackagesFromActiveSource() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Filter = "P1 P3";

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(result.First().Id, "P1");
            Assert.AreEqual(result.Last().Id, "P3");
        }

        [TestMethod]
        public void GetPackageReturnsAllPackagesFromLocalRepositoryWhenInstalledIsPresent() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Installed = new SwitchParameter(isPresent: true);

            // Act 
            var result = cmdlet.GetResults();

            // Assert
            Assert.AreEqual(2, result.Count());
        }

        [TestMethod]
        public void GetPackageReturnsFilteredPackagesFromLocalRepositoryWhenInstalledIsPresent() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Installed = new SwitchParameter(isPresent: true);
            cmdlet.Filter = "P1";

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("P1", result.First().Id);
        }

        [TestMethod]
        public void GetPackageReturnsUpdatesWhenUpdatesIsPresent() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Updates = new SwitchParameter(isPresent: true);

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            var package = result.Single();
            Assert.AreEqual(Version.Parse("1.1"), package.Version);
            Assert.AreEqual("P1", package.Id);
        }

        [TestMethod]
        public void GetPackageReturnsUpdatesFromSourceWhenUpdatesIsPresent() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Updates = new SwitchParameter(isPresent: true);
            cmdlet.Source = "foo";

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            var package = result.Single();
            Assert.AreEqual(Version.Parse("1.4"), package.Version);
            Assert.AreEqual("P1", package.Id);
        }

        [TestMethod]
        public void GetPackageThrowsWhenSolutionIsClosedAndInstalledIsPresent() {
            // Arrange 
            var cmdlet = BuildCmdlet(isSolutionOpen: false);
            cmdlet.Installed = new SwitchParameter(isPresent: true);

            // Act 
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(),
                "The current environment doesn't have a solution open.");
        }

        [TestMethod]
        public void GetPackageThrowsWhenSolutionIsClosedAndUpdatesIsPresent() {
            // Arrange 
            var cmdlet = BuildCmdlet(isSolutionOpen: false);
            cmdlet.Updates = new SwitchParameter(isPresent: true);
            var result = new List<object>();

            // Act 
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(),
                "The current environment doesn't have a solution open.");
        }

        private static GetPackageCmdlet BuildCmdlet(bool isSolutionOpen = true) {
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(GetPackageManager);
            var cmdlet = new Mock<GetPackageCmdlet>(GetRepositoryFactory(), GetSourceProvider(), TestUtils.GetSolutionManager(isSolutionOpen: isSolutionOpen), packageManagerFactory.Object) { CallBase = true };
            return cmdlet.Object;
        }

        private static IPackageRepositoryFactory GetRepositoryFactory() {
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            var repository = new Mock<IPackageRepository>();
            var packages = new[] { PackageUtility.CreatePackage("P1", "1.4"), PackageUtility.CreatePackage("P6") };
            repository.Setup(c => c.GetPackages()).Returns(packages.AsQueryable());

            repositoryFactory.Setup(c => c.CreateRepository("foo")).Returns(repository.Object);

            return repositoryFactory.Object;
        }

        private static IVsPackageManager GetPackageManager() {
            var fileSystem = new Mock<IFileSystem>();
            var localRepo = new Mock<IPackageRepository>();
            var localPackages = new[] { PackageUtility.CreatePackage("P1", "0.9"), PackageUtility.CreatePackage("P2") };
            localRepo.Setup(c => c.GetPackages()).Returns(localPackages.AsQueryable());

            var remotePackages = new[] { PackageUtility.CreatePackage("P0", "1.1"), PackageUtility.CreatePackage("P1", "1.1"), PackageUtility.CreatePackage("P3") };
            var remoteRepo = new Mock<IPackageRepository>();
            remoteRepo.Setup(c => c.GetPackages()).Returns(remotePackages.AsQueryable());
            return new VsPackageManager(TestUtils.GetSolutionManager(), remoteRepo.Object, fileSystem.Object, localRepo.Object);
        }

        private static IPackageSourceProvider GetSourceProvider() {
            Mock<IPackageSourceProvider> sourceProvider = new Mock<IPackageSourceProvider>();
            sourceProvider.Setup(c => c.ActivePackageSource).Returns(new PackageSource("foo", "foo"));
            return sourceProvider.Object;
        }
    }
}
