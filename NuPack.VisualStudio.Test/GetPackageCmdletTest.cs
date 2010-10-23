using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuPack.Test;
using NuPack.VisualStudio.Cmdlets;

namespace NuPack.VisualStudio.Test {
    [TestClass]
    public class GetPackageCmdletTest {
        [TestMethod]
        public void GetPackageReturnsAllPackagesFromActiveSourceWhenNoParametersAreSpecified() {
            // Arrange 
            var cmdlet = new GetPackageCmdlet(GetRepositoryFactory(), TestUtils.GetSolutionManager(), TestUtils.GetDTE(), GetPackageManager());
            
            // Act 
            var result = cmdlet.GetResults();

            // Assert
            Assert.AreEqual(3, result.Count());
        }

        [TestMethod]
        public void GetPackageReturnsAllPackagesFromSpecifiedSourceWhenNoParametersAreSpecified() {
            // Arrange 
            var cmdlet = new GetPackageCmdlet(GetRepositoryFactory(), TestUtils.GetSolutionManager(), TestUtils.GetDTE(), GetPackageManager());
            cmdlet.Source = "foo";
           
            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("C", result.First().Id);
            Assert.AreEqual("Y", result.Last().Id);
        }

        [TestMethod]
        public void GetPackageReturnsFilteredPackagesFromActiveSource() {
            // Arrange 
            var cmdlet = new GetPackageCmdlet(GetRepositoryFactory(), TestUtils.GetSolutionManager(), TestUtils.GetDTE(), GetPackageManager());
            cmdlet.Filter = "A C";

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(result.First().Id, "A");
            Assert.AreEqual(result.Last().Id, "C");
        }

        [TestMethod]
        public void GetPackageReturnsAllPackagesFromLocalRepositoryWhenInstalledIsPresent() {
            // Arrange 
            var cmdlet = new GetPackageCmdlet(GetRepositoryFactory(), TestUtils.GetSolutionManager(), TestUtils.GetDTE(), GetPackageManager());
            cmdlet.Installed = new SwitchParameter(isPresent: true);

            // Act 
            var result = cmdlet.GetResults();

            // Assert
            Assert.AreEqual(2, result.Count());
        }

        [TestMethod]
        public void GetPackageReturnsFilteredPackagesFromLocalRepositoryWhenInstalledIsPresent() {
            // Arrange 
            var cmdlet = new GetPackageCmdlet(GetRepositoryFactory(), TestUtils.GetSolutionManager(), TestUtils.GetDTE(), GetPackageManager());
            cmdlet.Installed = new SwitchParameter(isPresent: true);
            cmdlet.Filter = "C";

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("C", result.First().Id);
        }

        [TestMethod]
        public void GetPackageReturnsUpdatesWhenUpdatesIsPresent() {
            // Arrange 
            var cmdlet = new GetPackageCmdlet(GetRepositoryFactory(), TestUtils.GetSolutionManager(), TestUtils.GetDTE(), GetPackageManager());
            cmdlet.Updates = new SwitchParameter(isPresent: true);

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            var package = result.Single();
            Assert.AreEqual(Version.Parse("1.1"), package.Version);
            Assert.AreEqual("C", package.Id);
        }

        [TestMethod]
        public void GetPackageReturnsUpdatesFromSourceWhenUpdatesIsPresent() {
            // Arrange 
            var cmdlet = new GetPackageCmdlet(GetRepositoryFactory(), TestUtils.GetSolutionManager(), TestUtils.GetDTE(), GetPackageManager());
            cmdlet.Updates = new SwitchParameter(isPresent: true);
            cmdlet.Source = "foo";

            // Act 
            var result = cmdlet.GetResults<dynamic>(); 

            // Assert
            var package = result.Single();
            Assert.AreEqual(Version.Parse("1.4"), package.Version);
            Assert.AreEqual("C", package.Id);
        }

        [TestMethod]
        public void GetPackageThrowsWhenSolutionIsClosedAndInstalledIsPresent() {
            // Arrange 
            var cmdlet = new GetPackageCmdlet(GetRepositoryFactory(), TestUtils.GetSolutionManager(), TestUtils.GetDTE(isSolutionOpen: false), GetPackageManager());
            cmdlet.Installed = new SwitchParameter(isPresent: true);

            // Act 
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(), 
                "The current environment doesn't have a solution open.");
        }

        [TestMethod]
        public void GetPackageThrowsWhenSolutionIsClosedAndUpdatesIsPresent() {
            // Arrange 
            var cmdlet = new GetPackageCmdlet(GetRepositoryFactory(), TestUtils.GetSolutionManager(), TestUtils.GetDTE(isSolutionOpen: false), GetPackageManager());
            cmdlet.Updates = new SwitchParameter(isPresent: true);
            var result = new List<object>();

            // Act 
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(),
                "The current environment doesn't have a solution open.");
        }

        private static IPackageRepositoryFactory GetRepositoryFactory() {
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            var repository = new Mock<IPackageRepository>();
            var packages = new[] { GetPackage("C", "1.4"), GetPackage("Y") };
            repository.Setup(c => c.GetPackages()).Returns(packages.AsQueryable());

            repositoryFactory.Setup(c => c.CreateRepository("foo")).Returns(repository.Object);

            return repositoryFactory.Object;
        }

        private static VSPackageManager GetPackageManager() {
            var fileSystem = new Mock<IFileSystem>();
            var localRepo = new Mock<IPackageRepository>();
            var localPackages = new[] { GetPackage("A"), GetPackage("C", "0.9") };
            var updates = new [] { GetPackage("X"), GetPackage("Y") };
            localRepo.Setup(c => c.GetPackages()).Returns(localPackages.AsQueryable());

            var remotePackages = new[] { GetPackage("A"), GetPackage("C", "1.1"), GetPackage("D") };
            var remoteRepo = new Mock<IPackageRepository>();
            remoteRepo.Setup(c => c.GetPackages()).Returns(remotePackages.AsQueryable());
            return new VSPackageManager(TestUtils.GetSolutionManager(), remoteRepo.Object, localRepo.Object, fileSystem.Object);
        }

        private static IPackage GetPackage(string name, string version = "1.0") {
            var package = new Mock<IPackage>();
            package.SetupGet(c => c.Id).Returns(name);
            package.SetupGet(c => c.Version).Returns(Version.Parse(version));
            package.SetupGet(c => c.Description).Returns(name);

            return package.Object;
        }
    }
}
