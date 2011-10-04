using System;
using System.Linq;
using Xunit;
using Moq;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Test.Mocks;

namespace NuGet.Test.NuGetCommandLine.Commands {
    
    public class ListCommandTests {
        private const string DefaultRepoUrl = "https://go.microsoft.com/fwlink/?LinkID=206669";
        private const string NonDefaultRepoUrl1 = "http://NotDefault1";
        private const string NonDefaultRepoUrl2 = "http://NotDefault2";

        [Fact]
        public void GetPackagesUsesSourceIfDefined() {
            // Arrange
            IPackageRepositoryFactory factory = CreatePackageRepositoryFactory();
            IConsole console = new Mock<IConsole>().Object;
            ListCommand cmd = new ListCommand(factory, GetSourceProvider());
            cmd.Console = console;
            cmd.Source.Add(NonDefaultRepoUrl1);

            // Act
            var packages = cmd.GetPackages();

            // Assert
            Assert.Equal("CustomUrlUsed", packages.Single().Id);
        }

        [Fact]
        public void GetPackagesUsesAggregateSourceIfNoSourceDefined() {
            // Arrange
            IPackageRepositoryFactory factory = CreatePackageRepositoryFactory();
            IConsole console = new Mock<IConsole>().Object;
            ListCommand cmd = new ListCommand(factory, GetSourceProvider());
            cmd.Console = console;

            // Act
            var packages = cmd.GetPackages();

            // Assert
            Assert.Equal(6, packages.Count());
            AssertPackage(new { Id = "AnotherTerm", Ver = "1.0" }, packages.ElementAt(0));
            AssertPackage(new { Id = "CustomUrlUsed", Ver = "1.0" }, packages.ElementAt(1));
            AssertPackage(new { Id = "DefaultUrlUsed", Ver = "1.0" }, packages.ElementAt(2));
            AssertPackage(new { Id = "jQuery", Ver = "1.50" }, packages.ElementAt(3));
            AssertPackage(new { Id = "NHibernate", Ver = "1.2" }, packages.ElementAt(4));
            AssertPackage(new { Id = "SearchPackage", Ver = "1.0" }, packages.ElementAt(5));
        }

        [Fact]
        public void GetPackageUsesSearchTermsIfPresent() {
            // Arrange
            IPackageRepositoryFactory factory = CreatePackageRepositoryFactory();
            IConsole console = new Mock<IConsole>().Object;
            ListCommand cmd = new ListCommand(factory, GetSourceProvider());
            cmd.Source.Add(DefaultRepoUrl);
            cmd.Console = console;
            cmd.Arguments.Add("SearchPackage");


            // Act
            var packages = cmd.GetPackages();

            // Assert
            Assert.Equal(1, packages.Count());
            Assert.Equal("SearchPackage", packages.First().Id);
        }

        [Fact]
        public void GetPackageCollapsesVersionsByDefault() {
            // Arrange
            var factory = CreatePackageRepositoryFactory();
            var console = new Mock<IConsole>().Object;
            var cmd = new ListCommand(factory, GetSourceProvider());
            cmd.Source.Add(NonDefaultRepoUrl2);
            cmd.Console = console;

            // Act
            var packages = cmd.GetPackages();

            // Assert
            Assert.Equal(2, packages.Count());
            AssertPackage(new { Id = "jQuery", Ver = "1.50" }, packages.ElementAt(0));
            AssertPackage(new { Id = "NHibernate", Ver = "1.2" }, packages.ElementAt(1));
        }

        [Fact]
        public void GetPackageFiltersAndCollapsesVersions() {
            // Arrange
            var factory = CreatePackageRepositoryFactory();
            var console = new Mock<IConsole>().Object;
            var cmd = new ListCommand(factory, GetSourceProvider());
            cmd.Source.Add(NonDefaultRepoUrl2);
            cmd.Console = console;
            cmd.Arguments.Add("NHibernate");

            // Act
            var packages = cmd.GetPackages();

            // Assert
            Assert.Equal(1, packages.Count());
            AssertPackage(new { Id = "NHibernate", Ver = "1.2" }, packages.ElementAt(0));
        }

        [Fact]
        public void GetPackageReturnsAllVersionsIfAllVersionsFlagIsSet() {
            // Arrange
            var factory = CreatePackageRepositoryFactory();
            var console = new Mock<IConsole>().Object;
            var cmd = new ListCommand(factory, GetSourceProvider());
            cmd.Source.Add(NonDefaultRepoUrl2);
            cmd.Console = console;
            cmd.AllVersions = true;

            // Act
            var packages = cmd.GetPackages();

            // Assert
            Assert.Equal(5, packages.Count());
            AssertPackage(new { Id = "jQuery", Ver = "1.44" }, packages.ElementAt(0));
            AssertPackage(new { Id = "jQuery", Ver = "1.50" }, packages.ElementAt(1));
            AssertPackage(new { Id = "NHibernate", Ver = "1.0" }, packages.ElementAt(2));
            AssertPackage(new { Id = "NHibernate", Ver = "1.1" }, packages.ElementAt(3));
            AssertPackage(new { Id = "NHibernate", Ver = "1.2" }, packages.ElementAt(4));
        }

        [Fact]
        public void GetPackageResolvesSources() {
            // Arrange
            var factory = CreatePackageRepositoryFactory();
            var console = new Mock<IConsole>().Object;
            var provider = new Mock<IPackageSourceProvider>();
            provider.Setup(c => c.LoadPackageSources()).Returns(new[] { new PackageSource(NonDefaultRepoUrl1, "Foo") });
            var cmd = new ListCommand(factory, provider.Object);
            cmd.Source.Add("Foo");
            cmd.Console = console;

            // Act
            var packages = cmd.GetPackages();

            // Assert
            AssertPackage(new { Id = "CustomUrlUsed", Ver = "1.0" }, packages.Single());
        }

        private static void AssertPackage(dynamic expected, IPackage package) {
            Assert.Equal(expected.Id, package.Id);
            Assert.Equal(new SemanticVersion(expected.Ver), package.Version);
        }

        private static IPackageRepositoryFactory CreatePackageRepositoryFactory() {
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
            packageRepositoryFactory.Setup(p => p.CreateRepository(It.IsAny<string>())).Returns<string>(s => {
                switch (s) {
                    case NonDefaultRepoUrl1: return nondefaultPackageRepository;
                    case NonDefaultRepoUrl2: return multiVersionRepo;
                    default: return defaultPackageRepository;
                }
            });

            //Return the Factory
            return packageRepositoryFactory.Object;
        }

        private static IPackageSourceProvider GetSourceProvider(params string[] sources) {
            var provider = new Mock<IPackageSourceProvider>();
            if (sources == null || !sources.Any()) {
                sources = new[] { DefaultRepoUrl, NonDefaultRepoUrl1, NonDefaultRepoUrl2 };
            }
            provider.Setup(c => c.LoadPackageSources()).Returns(sources.Select(c => new PackageSource(c)));

            return provider.Object;
        }
    }
}
