using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Test.Mocks;

namespace NuGet.Test.NuGetCommandLine.Commands {
    [TestClass]
    public class ListCommandTests {
        private const string DefaultRepoUrl = "https://go.microsoft.com/fwlink/?LinkID=206669";
        private const string NonDefaultRepoUrl1 = "http://NotDefault1";
        private const string NonDefaultRepoUrl2 = "http://NotDefault2";

        [TestMethod]
        public void GetPackagesUsesSourceIfDefined() {
            // Arrange
            IPackageRepositoryFactory factory = CreatePackageRepositoryFactory();
            IConsole console = new Mock<IConsole>().Object;
            ListCommand cmd = new ListCommand(factory);
            cmd.Console = console;
            cmd.Source = NonDefaultRepoUrl1;

            // Act
            var packages = cmd.GetPackages();

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
            var packages = cmd.GetPackages();

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
            var packages = cmd.GetPackages();

            // Assert
            Assert.AreEqual(2, packages.Count());
            Assert.AreEqual("SearchPackage", packages.First().Id);
            Assert.AreEqual("AnotherTerm", packages.Last().Id);

        }

        [TestMethod]
        public void GetPackageCollapsesVersionsByDefault() {
            // Arrange
            var factory = CreatePackageRepositoryFactory();
            var console = new Mock<IConsole>().Object;
            var cmd = new ListCommand(factory);
            cmd.Source = NonDefaultRepoUrl2;
            cmd.Console = console;

            // Act
            var packages = cmd.GetPackages();

            // Assert
            Assert.AreEqual(2, packages.Count());
            Assert.AreEqual("NHibernate", packages.First().Id);
            Assert.AreEqual(new Version("1.2"), packages.First().Version);
            Assert.AreEqual("jQuery", packages.Last().Id);
            Assert.AreEqual(new Version("1.50"), packages.Last().Version);
        }

        [TestMethod]
        public void GetPackageFiltersAndCollapsesVersions() {
            // Arrange
            var factory = CreatePackageRepositoryFactory();
            var console = new Mock<IConsole>().Object;
            var cmd = new ListCommand(factory);
            cmd.Source = NonDefaultRepoUrl2;
            cmd.Console = console;
            cmd.Arguments.Add("NHibernate");

            // Act
            var packages = cmd.GetPackages();

            // Assert
            Assert.AreEqual(1, packages.Count());
            Assert.AreEqual("NHibernate", packages.First().Id);
            Assert.AreEqual(new Version("1.2"), packages.First().Version);
        }

        [TestMethod]
        public void GetPackageReturnsAllVersionsIfAllVersionsFlagIsSet() {
            // Arrange
            var factory = CreatePackageRepositoryFactory();
            var console = new Mock<IConsole>().Object;
            var cmd = new ListCommand(factory);
            cmd.Source = NonDefaultRepoUrl2;
            cmd.Console = console;
            cmd.AllVersions = true;

            // Act
            var packages = cmd.GetPackages();

            // Assert
            Assert.AreEqual(5, packages.Count());
            Assert.AreEqual("NHibernate", packages.ElementAt(0).Id);
            Assert.AreEqual(new Version("1.0"), packages.ElementAt(0).Version);
            Assert.AreEqual("NHibernate", packages.ElementAt(1).Id);
            Assert.AreEqual(new Version("1.1"), packages.ElementAt(1).Version);
            Assert.AreEqual("NHibernate", packages.ElementAt(2).Id);
            Assert.AreEqual(new Version("1.2"), packages.ElementAt(2).Version);
            Assert.AreEqual("jQuery", packages.ElementAt(3).Id);
            Assert.AreEqual(new Version("1.44"), packages.ElementAt(3).Version);
            Assert.AreEqual("jQuery", packages.ElementAt(4).Id);
            Assert.AreEqual(new Version("1.50"), packages.ElementAt(4).Version);
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

            //Nondefault Repository (Custom URL)
            MockPackageRepository nondefaultPackageRepository = new MockPackageRepository();
            var packageB = PackageUtility.CreatePackage("CustomUrlUsed", "1.0");
            nondefaultPackageRepository.AddPackage(packageB);

            // Repo with multiple versions
            var multiVersionRepo = new MockPackageRepository();
            multiVersionRepo.AddPackage(PackageUtility.CreatePackage("NHibernate", "1.0"));
            multiVersionRepo.AddPackage(PackageUtility.CreatePackage("NHibernate", "1.1"));
            multiVersionRepo.AddPackage(PackageUtility.CreatePackage("NHibernate", "1.2"));
            multiVersionRepo.AddPackage(PackageUtility.CreatePackage("jQuery", "1.44"));
            multiVersionRepo.AddPackage(PackageUtility.CreatePackage("jQuery", "1.50"));

            //Setup Factory
            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            packageRepositoryFactory.Setup(p => p.CreateRepository(It.IsAny<PackageSource>())).Returns<PackageSource>(s => {
                switch (s.Source) {
                    case NonDefaultRepoUrl1: return nondefaultPackageRepository;
                    case NonDefaultRepoUrl2: return multiVersionRepo;
                    default: return defaultPackageRepository;
                }
            });

            //Return the Factory
            return packageRepositoryFactory.Object;
        }
    }
}
