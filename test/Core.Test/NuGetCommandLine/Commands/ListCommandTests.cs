namespace NuGet.Test.NuGetCommandLine.Commands {

    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using NuGet.Commands;
    using NuGet.Common;
    using NuGet.Test.Mocks;

    [TestClass]
    public class ListCommandTests {

        [TestMethod]
        public void GetPackagesUsesSourceIfDefined() {
            // Arrange
            IPackageRepositoryFactory factory = CreatePackageRepositoryFactory();
            IConsole console = new Mock<IConsole>().Object;
            ListCommand cmd = new ListCommand(factory);
            cmd.Console = console;
            cmd.Source = "http://NotDefault";

            // Act
            IQueryable<IPackage> packages = cmd.GetPackages();

            // Assert
            Assert.AreEqual("CustomUrlUsed", packages.First().Id);

        }

        [TestMethod]
        public void GetPackagesUsesDefaultSourceIfNoSourceDefined() {
            // Arrange
            IPackageRepositoryFactory factory = CreatePackageRepositoryFactory();
            IConsole console = new Mock<IConsole>().Object;
            ListCommand cmd = new ListCommand(factory);
            cmd.Console = console;

            // Act
            IQueryable<IPackage> packages = cmd.GetPackages();

            // Assert
            Assert.AreEqual(3, packages.Count());
            Assert.AreEqual("DefaultUrlUsed", packages.First().Id);

        }

        [TestMethod]
        public void GetPackageUsesSearchTermsIfPresent() {
            // Arrange
            IPackageRepositoryFactory factory = CreatePackageRepositoryFactory();
            IConsole console = new Mock<IConsole>().Object;
            ListCommand cmd = new ListCommand(factory);
            cmd.Console = console;
            List<string> searchTerms = new List<string>();
            searchTerms.Add("SearchPackage");
            searchTerms.Add("AnotherTerm");
            cmd.Arguments = searchTerms;

            // Act
            IQueryable<IPackage> packages = cmd.GetPackages();

            // Assert
            Assert.AreEqual(2, packages.Count());
            Assert.AreEqual("SearchPackage", packages.First().Id);
            Assert.AreEqual("AnotherTerm", packages.Last().Id);

        }

        public IPackageRepositoryFactory CreatePackageRepositoryFactory() {
            //Default Repository
            MockPackageRepository defaultPackageRepository = new MockPackageRepository();
            var packageA = PackageUtility.CreatePackage("DefaultUrlUsed", "1.0");
            defaultPackageRepository.AddPackage(packageA);
            var packageC = PackageUtility.CreatePackage("SearchPackage", "1.0");
            defaultPackageRepository.AddPackage(packageC);
            var packageD = PackageUtility.CreatePackage("AnotherTerm", "1.0");
            defaultPackageRepository.AddPackage(packageD);

            //Nondefault Reposritory (Custom URL)
            MockPackageRepository nondefaultPackageRepository = new MockPackageRepository();
            var packageB = PackageUtility.CreatePackage("CustomUrlUsed", "1.0");
            nondefaultPackageRepository.AddPackage(packageB);

            //Setup Factory
            string defaultFeedUrl = "https://go.microsoft.com/fwlink/?LinkID=206669";
            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            packageRepositoryFactory.Setup(p => p.CreateRepository(It.Is<PackageSource>(s => s.Source.Equals(defaultFeedUrl)))).Returns(defaultPackageRepository);
            packageRepositoryFactory.Setup(p => p.CreateRepository(It.Is<PackageSource>(s => !s.Source.Equals(defaultFeedUrl)))).Returns(nondefaultPackageRepository);

            //Return the Factory
            return packageRepositoryFactory.Object;
        }
    }
}
