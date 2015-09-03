using System;
using System.Linq;
using Moq;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test.NuGetCommandLine.Commands
{

    public class ListCommandTests
    {
        private const string DefaultRepoUrl = "https://go.microsoft.com/fwlink/?LinkID=206669";
        private const string NonDefaultRepoUrl1 = "http://NotDefault1";
        private const string NonDefaultRepoUrl2 = "http://NotDefault2";

        [Fact]
        public void GetPackagesUsesSourceIfDefined()
        {
            // Arrange
            IPackageRepositoryFactory factory = CreatePackageRepositoryFactory();
            IConsole console = new Mock<IConsole>().Object;
            ListCommand cmd = new ListCommand();
            cmd.RepositoryFactory = factory;
            cmd.SourceProvider = GetSourceProvider();
            cmd.Console = console;
            cmd.Source.Add(NonDefaultRepoUrl1);

            // Act
            var packages = cmd.GetPackages();

            // Assert
            Assert.Equal("CustomUrlUsed", packages.Single().Id);
        }

        [Fact]
        public void GetPackagesUsesAggregateSourceIfNoSourceDefined()
        {
            // Arrange
            IPackageRepositoryFactory factory = CreatePackageRepositoryFactory();
            IConsole console = new Mock<IConsole>().Object;
            ListCommand cmd = new ListCommand()
            {
                RepositoryFactory = factory,
                SourceProvider = GetSourceProvider(),
                Console = console
            };

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
        public void GetPackageUsesSearchTermsIfPresent()
        {
            // Arrange
            IPackageRepositoryFactory factory = CreatePackageRepositoryFactory();
            IConsole console = new Mock<IConsole>().Object;
            ListCommand cmd = new ListCommand()
            {
                RepositoryFactory = factory,
                SourceProvider = GetSourceProvider()
            };
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
        public void GetPackageThatHasLicensesWithVerbositySetToDetailed()
        {
            // Arrange
            IPackageRepositoryFactory factory = CreatePackageRepositoryFactory();
            Mock<IConsole> consoleMock = new Mock<IConsole>();
            IConsole console = consoleMock.Object;

            consoleMock.Setup(x => x.PrintJustified(1, "License url: ftp://test/somelicense.txts")).Verifiable("could not find license url");
            ListCommand cmd = new ListCommand()
            {
                RepositoryFactory = factory,
                SourceProvider = GetSourceProvider()
            };
            cmd.Source.Add(DefaultRepoUrl);
            cmd.Console = console;
            cmd.Arguments.Add("SearchPackage");
            cmd.Verbosity = Verbosity.Detailed;

            // Act
            var packages = cmd.GetPackages();
            cmd.ExecuteCommand();

            // Assert
            var url = packages.First().LicenseUrl;
            Assert.Equal(1, packages.Count());
            Assert.Equal(new Uri("ftp://test/somelicense.txts"), packages.First().LicenseUrl);
            consoleMock.Verify();
        }

        [Fact]
        public void GetPackageCollapsesVersionsByDefault()
        {
            // Arrange
            var factory = CreatePackageRepositoryFactory();
            var console = new Mock<IConsole>().Object;
            var cmd = new ListCommand()
            {
                RepositoryFactory = factory,
                SourceProvider = GetSourceProvider()
            };
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
        public void GetPackageFiltersAndCollapsesVersions()
        {
            // Arrange
            var factory = CreatePackageRepositoryFactory();
            var console = new Mock<IConsole>().Object;
            var cmd = new ListCommand()
            {
                RepositoryFactory = factory,
                SourceProvider = GetSourceProvider()
            };
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
        public void GetPackageReturnsAllVersionsIfAllVersionsFlagIsSet()
        {
            // Arrange
            var factory = CreatePackageRepositoryFactory();
            var console = new Mock<IConsole>().Object;
            var cmd = new ListCommand()
            {
                RepositoryFactory = factory,
                SourceProvider = GetSourceProvider()
            };
            cmd.Source.Add(NonDefaultRepoUrl2);
            cmd.Console = console;
            cmd.AllVersions = true;

            // Act
            var packages = cmd.GetPackages();

            // Assert
            Assert.Equal(5, packages.Count());
            // TODO: Wrong order of packages
            AssertPackage(new { Id = "jQuery", Ver = "1.50" }, packages.ElementAt(0));
            AssertPackage(new { Id = "JQuery", Ver = "1.44" }, packages.ElementAt(1));
            AssertPackage(new { Id = "NHibernate", Ver = "1.0" }, packages.ElementAt(2));
            AssertPackage(new { Id = "NHibernate", Ver = "1.1" }, packages.ElementAt(3));
            AssertPackage(new { Id = "NHibernate", Ver = "1.2" }, packages.ElementAt(4));
        }

        [Fact]
        public void GetPackageFiltersPackagesUsingIsLatestIfRepositoryDoesNotSupportPrerelease()
        {
            // Arrange
            var package = new Mock<IPackage>(MockBehavior.Strict);
            package.SetupGet(p => p.Id).Returns("A");
            package.SetupGet(p => p.Version).Returns(new SemanticVersion("1.0.0"));
            package.SetupGet(p => p.IsLatestVersion).Returns(true).Verifiable();
            package.SetupGet(p => p.Listed).Returns(true);
            package.SetupGet(p => p.IsAbsoluteLatestVersion).Throws(new Exception("Repository does not support this property."));

            var repository = new Mock<IPackageRepository>(MockBehavior.Strict);
            repository.SetupGet(r => r.SupportsPrereleasePackages).Returns(false).Verifiable();
            repository.Setup(r => r.GetPackages()).Returns(new[] { package.Object }.AsQueryable());
            var factory = new Mock<IPackageRepositoryFactory>(MockBehavior.Strict);
            factory.Setup(f => f.CreateRepository(DefaultRepoUrl)).Returns(repository.Object);

            var cmd = new ListCommand()
            {
                RepositoryFactory = factory.Object,
                SourceProvider = GetSourceProvider(),
                Console = new Mock<IConsole>().Object,
                Prerelease = true
            };
            cmd.Source.Add(DefaultRepoUrl);

            // Act
            var packages = cmd.GetPackages();

            // Assert
            Assert.Equal(1, packages.Count());
            AssertPackage(new { Id = "A", Ver = "1.0.0" }, packages.First());
            package.Verify();
            repository.Verify();
        }

        [Fact]
        public void GetPackageDoesNotShowUnlistedPackagesOrPackagesWithLegacyDates()
        {
            // Arrange
            var packageA = new Mock<IPackage>(MockBehavior.Strict);
            packageA.SetupGet(p => p.Id).Returns("A");
            packageA.SetupGet(p => p.Version).Returns(new SemanticVersion("1.0.0"));
            packageA.SetupGet(p => p.IsLatestVersion).Returns(true);
            packageA.SetupGet(pA => pA.Listed).Returns(false);
            packageA.SetupGet(p => p.Published).Returns(DateTime.Now);

            var packageB = new Mock<IPackage>(MockBehavior.Strict);
            packageB.SetupGet(p => p.Id).Returns("B");
            packageB.SetupGet(p => p.Version).Returns(new SemanticVersion("1.0.2"));
            packageB.SetupGet(pB => pB.Listed).Returns(true);
            packageB.SetupGet(p => p.IsLatestVersion).Returns(true);


            var packageC = new Mock<IPackage>(MockBehavior.Strict);
            packageC.SetupGet(p => p.Id).Returns("C");
            packageC.SetupGet(p => p.Version).Returns(new SemanticVersion("1.0.0"));
            packageC.SetupGet(p => p.IsLatestVersion).Returns(true);
            packageC.SetupGet(pC => pC.Listed).Returns(false);
            packageC.SetupGet(p => p.Published).Returns(new DateTime(1900, 1, 1, 0, 0, 0));

            var repository = new MockPackageRepository { packageA.Object, packageB.Object, packageC.Object };
            var factory = new Mock<IPackageRepositoryFactory>(MockBehavior.Strict);
            factory.Setup(f => f.CreateRepository(DefaultRepoUrl)).Returns(repository);

            var cmd = new ListCommand()
            {
                RepositoryFactory = factory.Object, 
                SourceProvider = GetSourceProvider(),
                Console = new Mock<IConsole>().Object,
            };
            cmd.Source.Add(DefaultRepoUrl);

            // Act
            var packages = cmd.GetPackages();

            // Assert
            Assert.Equal(2, packages.Count());
            AssertPackage(new { Id = "A", Ver = "1.0.0" }, packages.First());
            AssertPackage(new { Id = "B", Ver = "1.0.2" }, packages.Last());
        }

        [Fact]
        public void GetPackageUsesIsAbsoluteLatestVersionIfPrereleaseIfSpecifiedAndRespositoriesSupportsPrerelease()
        {
            // Arrange
            var packageA = new Mock<IPackage>(MockBehavior.Strict);
            packageA.SetupGet(p => p.Id).Returns("A");
            packageA.SetupGet(p => p.Version).Returns(new SemanticVersion("1.0.0"));
            packageA.SetupGet(p => p.IsAbsoluteLatestVersion).Returns(true).Verifiable();
            packageA.SetupGet(pA => pA.Listed).Returns(false);
            packageA.SetupGet(p => p.Published).Returns(DateTime.Now);

            var packageB = new Mock<IPackage>(MockBehavior.Strict);
            packageB.SetupGet(p => p.Id).Returns("B");
            packageB.SetupGet(p => p.Version).Returns(new SemanticVersion("1.0.2"));
            packageB.SetupGet(pB => pB.Listed).Returns(true);
            packageB.SetupGet(p => p.IsAbsoluteLatestVersion).Returns(false).Verifiable();


            var packageC = new Mock<IPackage>(MockBehavior.Strict);
            packageC.SetupGet(p => p.Id).Returns("C");
            packageC.SetupGet(p => p.Version).Returns(new SemanticVersion("1.0.0"));
            packageC.SetupGet(p => p.IsAbsoluteLatestVersion).Returns(true).Verifiable();
            packageC.SetupGet(pC => pC.Listed).Returns(true);

            var repository = new MockPackageRepository { packageA.Object, packageB.Object, packageC.Object };
            var factory = new Mock<IPackageRepositoryFactory>(MockBehavior.Strict);
            factory.Setup(f => f.CreateRepository(DefaultRepoUrl)).Returns(repository);

            var cmd = new ListCommand()
            {
                RepositoryFactory = factory.Object,
                SourceProvider = GetSourceProvider(),
                Console = new Mock<IConsole>().Object,
                Prerelease = true
            };
            cmd.Source.Add(DefaultRepoUrl);

            // Act
            var packages = cmd.GetPackages();

            // Assert
            Assert.Equal(2, packages.Count());
            AssertPackage(new { Id = "A", Ver = "1.0.0" }, packages.First());
            AssertPackage(new { Id = "C", Ver = "1.0.0" }, packages.Last());
        }

        [Fact]
        public void GetPackageResolvesSources()
        {
            // Arrange
            var factory = CreatePackageRepositoryFactory();
            var console = new Mock<IConsole>().Object;
            var provider = new Mock<IPackageSourceProvider>();
            provider.Setup(c => c.LoadPackageSources()).Returns(new[] { new PackageSource(NonDefaultRepoUrl1, "Foo") });
            var cmd = new ListCommand()
            {
                RepositoryFactory = factory,
                SourceProvider = provider.Object
            };
            cmd.Source.Add("Foo");
            cmd.Console = console;

            // Act
            var packages = cmd.GetPackages();

            // Assert
            AssertPackage(new { Id = "CustomUrlUsed", Ver = "1.0" }, packages.Single());
        }

        private static void AssertPackage(dynamic expected, IPackage package)
        {
            Assert.Equal(expected.Id, package.Id);
            Assert.Equal(new SemanticVersion(expected.Ver), package.Version);
        }

        private static IPackageRepositoryFactory CreatePackageRepositoryFactory()
        {
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
            // different case is intended to test PackageEqualityComparer.Id
            multiVersionRepo.AddPackage(PackageUtility.CreatePackage("JQuery", "1.44"));
            multiVersionRepo.AddPackage(PackageUtility.CreatePackage("jQuery", "1.50"));

            //Setup Factory
            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            packageRepositoryFactory.Setup(p => p.CreateRepository(It.IsAny<string>())).Returns<string>(s =>
            {
                switch (s)
                {
                    case NonDefaultRepoUrl1: return nondefaultPackageRepository;
                    case NonDefaultRepoUrl2: return multiVersionRepo;
                    default: return defaultPackageRepository;
                }
            });

            //Return the Factory
            return packageRepositoryFactory.Object;
        }

        private static IPackageSourceProvider GetSourceProvider(params string[] sources)
        {
            var provider = new Mock<IPackageSourceProvider>();
            if (sources == null || !sources.Any())
            {
                sources = new[] { DefaultRepoUrl, NonDefaultRepoUrl1, NonDefaultRepoUrl2 };
            }
            provider.Setup(c => c.LoadPackageSources()).Returns(sources.Select(c => new PackageSource(c)));

            return provider.Object;
        }
    }
}
