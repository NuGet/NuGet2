using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Runtime.Versioning;
using Moq;
using NuGet.Test.Mocks;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Test;
using Xunit;
using Xunit.Extensions;

namespace NuGet.PowerShell.Commands.Test
{
    using PackageUtility = NuGet.Test.PackageUtility;

    public class FindPackageCommandTest
    {
        [Fact]
        public void FindPackageFiltersByIdWhenSwitchIsSpecified()
        {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Filter = "pac";

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(1, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "Pack2", Version = new SemanticVersion("1.0") });
        }

        [Fact]
        public void FindPackageReturnsMaximumResultsWithFirstAndSkipParametersSet()
        {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);
            cmdlet.First = 2;
            cmdlet.Skip = 1;

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(3, result.Count());     // FindPackage always sets First = 30
            AssertPackageResultsEqual(result.ElementAt(0), new { Id = "P1", Version = new SemanticVersion("1.1") });
            AssertPackageResultsEqual(result.ElementAt(1), new { Id = "P3", Version = new SemanticVersion("1.0") });
            AssertPackageResultsEqual(result.ElementAt(2), new { Id = "Pack2", Version = new SemanticVersion("1.2") });
        }

        [Fact]
        public void FindPackageReturnsMaximumResultsWithFirstParameterSet()
        {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);
            cmdlet.First = 20;

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(4, result.Count());     // FindPackage always sets First = 30
            AssertPackageResultsEqual(result.ElementAt(0), new { Id = "P0", Version = new SemanticVersion("1.1") });
            AssertPackageResultsEqual(result.ElementAt(1), new { Id = "P1", Version = new SemanticVersion("1.1") });
            AssertPackageResultsEqual(result.ElementAt(2), new { Id = "P3", Version = new SemanticVersion("1.0") });
            AssertPackageResultsEqual(result.ElementAt(3), new { Id = "Pack2", Version = new SemanticVersion("1.2") });
        }

        [Fact]
        public void FindPackageFiltersRemoteByIdWhenSwitchIsSpecified()
        {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Filter = "pac";
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(1, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "Pack2", Version = new SemanticVersion("1.2") });
        }

        [Fact]
        public void FindPackageReturnsPackagesFilteredByIdWithUpdates()
        {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Updates = new SwitchParameter(isPresent: true);
            cmdlet.Filter = "Pac";

            // Act
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(packages.Count(), 1);
            AssertPackageResultsEqual(packages.First(), new { Id = "Pack2", Version = new SemanticVersion("1.2") });
        }

        [Theory]
        [InlineData(new object[] { false, false })]
        [InlineData(new object[] { false, true })]
        [InlineData(new object[] { true, false })]
        [InlineData(new object[] { true, true })]
        public void FindPackageReturnsUpdatesForServiceBasedRepositories(bool prerelease, bool allVersions)
        {
            // Arrange 
            var updates = new[] { 
                PackageUtility.CreatePackage("Pack2", "1.3"), 
                PackageUtility.CreatePackage("Pack2", "1.4")
            };
            var packageRepository = new Mock<IServiceBasedRepository>(MockBehavior.Strict);
            packageRepository.Setup(s => s.GetUpdates(It.IsAny<IEnumerable<IPackage>>(), prerelease, allVersions, It.IsAny<IEnumerable<FrameworkName>>(), It.IsAny<IEnumerable<IVersionSpec>>()))
                             .Returns(updates)
                             .Verifiable();

            var cmdlet = BuildCmdlet(repository: packageRepository.As<IPackageRepository>().Object);
            cmdlet.Source = "foo";
            cmdlet.Updates = true;
            cmdlet.Filter = "Pac";
            cmdlet.AllVersions = allVersions;
            cmdlet.IncludePrerelease = prerelease;

            // Act
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            packageRepository.Verify();
        }

        [Fact]
        public void FindPackageReturnsAllVersionsForSpecificPackage()
        {
            // Arrange 
            var packages = new[] { 
                PackageUtility.CreatePackage("Awesome", "0.1", description: "some desc"),
                PackageUtility.CreatePackage("Awesome", "0.4", description: "some desc"),
                PackageUtility.CreatePackage("Foobar", "0.4", description: "Awesome"),
                PackageUtility.CreatePackage("Not-Awesome", "0.6", description: "Awesome"),
            };

            var repository = GetPackageRepository(packages);
            var cmdlet = BuildCmdlet(repository: repository);
            cmdlet.Filter = "Awesome";
            cmdlet.Source = "foo";

            // Act
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(2, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "Awesome", Version = new SemanticVersion("0.1") });
            AssertPackageResultsEqual(result.Last(), new { Id = "Awesome", Version = new SemanticVersion("0.4") });
        }

        [Fact]
        public void FindPackageReturnsAllVersionsForSpecificPackageWhenSourceNameisUsed()
        {
            // Arrange 
            var packages = new[] { 
                PackageUtility.CreatePackage("Awesome", "0.1", description: "some desc"),
                PackageUtility.CreatePackage("Awesome", "0.4", description: "some desc"),
                PackageUtility.CreatePackage("Foobar", "0.4", description: "Awesome"),
                PackageUtility.CreatePackage("Not-Awesome", "0.6", description: "Awesome"),
            };

            var repository = GetPackageRepository(packages);
            var cmdlet = BuildCmdlet(repository: repository);
            cmdlet.Filter = "Awesome";
            cmdlet.Source = "foosource";

            // Act
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(2, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "Awesome", Version = new SemanticVersion("0.1") });
            AssertPackageResultsEqual(result.Last(), new { Id = "Awesome", Version = new SemanticVersion("0.4") });
        }

        [Fact]
        public void FindPackageReturnsPerformsPartialSearchesByDefault()
        {
            // Arrange 
            var packages = new[] { 
                PackageUtility.CreatePackage("Awesome", "0.1", description: "some desc"),
                PackageUtility.CreatePackage("Awesome", "0.4", description: "some desc"),
                PackageUtility.CreatePackage("AwesomeToo", "0.4", description: "Awesome Too desc"),
                PackageUtility.CreatePackage("Foobar", "0.4", description: "Awesome"),
                PackageUtility.CreatePackage("Not-Awesome", "0.6", description: "Awesome"),
            };

            var repository = GetPackageRepository(packages);
            var cmdlet = BuildCmdlet(repository: repository);
            cmdlet.Filter = "Awe";
            cmdlet.Source = "foo";

            // Act
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(3, result.Count());
            AssertPackageResultsEqual(result.ElementAt(0), new { Id = "Awesome", Version = new SemanticVersion("0.1") });
            AssertPackageResultsEqual(result.ElementAt(1), new { Id = "Awesome", Version = new SemanticVersion("0.4") });
            AssertPackageResultsEqual(result.ElementAt(2), new { Id = "AwesomeToo", Version = new SemanticVersion("0.4") });
        }

        [Fact]
        public void FindPackageReturnsPerformsPartialSearchesByDefaultAndSourceNameIsUsed()
        {
            // Arrange 
            var packages = new[] { 
                PackageUtility.CreatePackage("Awesome", "0.1", description: "some desc"),
                PackageUtility.CreatePackage("Awesome", "0.4", description: "some desc"),
                PackageUtility.CreatePackage("AwesomeToo", "0.4", description: "Awesome Too desc"),
                PackageUtility.CreatePackage("Foobar", "0.4", description: "Awesome"),
                PackageUtility.CreatePackage("Not-Awesome", "0.6", description: "Awesome"),
            };

            var repository = GetPackageRepository(packages);
            var cmdlet = BuildCmdlet(repository: repository);
            cmdlet.Filter = "Awe";
            cmdlet.Source = "foosource";

            // Act
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(3, result.Count());
            AssertPackageResultsEqual(result.ElementAt(0), new { Id = "Awesome", Version = new SemanticVersion("0.1") });
            AssertPackageResultsEqual(result.ElementAt(1), new { Id = "Awesome", Version = new SemanticVersion("0.4") });
            AssertPackageResultsEqual(result.ElementAt(2), new { Id = "AwesomeToo", Version = new SemanticVersion("0.4") });
        }

        [Fact]
        public void FindPackagePerformsExactMatchesIfExactMatchIsSpecified()
        {
            // Arrange 
            var packages = new[] { 
                PackageUtility.CreatePackage("Awesome", "0.1", description: "some desc"),
                PackageUtility.CreatePackage("Awesome", "0.4", description: "some desc"),
                PackageUtility.CreatePackage("AwesomeToo", "0.4", description: "Awesome Too desc"),
                PackageUtility.CreatePackage("Foobar", "0.4", description: "Awesome"),
                PackageUtility.CreatePackage("Not-Awesome", "0.6", description: "Awesome"),
            };

            var repository = GetPackageRepository(packages);
            var cmdlet = BuildCmdlet(repository: repository);
            cmdlet.ExactMatch = true;
            cmdlet.Filter = "Awesome";
            cmdlet.Source = "foo";

            // Act
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(2, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "Awesome", Version = new SemanticVersion("0.1") });
            AssertPackageResultsEqual(result.Last(), new { Id = "Awesome", Version = new SemanticVersion("0.4") });
        }

        [Fact]
        public void FindPackagePerformsExactMatchesIfExactMatchIsSpecifiedAndSourceNameIsUsed()
        {
            // Arrange 
            var packages = new[] { 
                PackageUtility.CreatePackage("Awesome", "0.1", description: "some desc"),
                PackageUtility.CreatePackage("Awesome", "0.4", description: "some desc"),
                PackageUtility.CreatePackage("AwesomeToo", "0.4", description: "Awesome Too desc"),
                PackageUtility.CreatePackage("Foobar", "0.4", description: "Awesome"),
                PackageUtility.CreatePackage("Not-Awesome", "0.6", description: "Awesome"),
            };

            var repository = GetPackageRepository(packages);
            var cmdlet = BuildCmdlet(repository: repository);
            cmdlet.ExactMatch = true;
            cmdlet.Filter = "Awesome";
            cmdlet.Source = "foosource";

            // Act
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(2, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "Awesome", Version = new SemanticVersion("0.1") });
            AssertPackageResultsEqual(result.Last(), new { Id = "Awesome", Version = new SemanticVersion("0.4") });
        }

        private static void AssertPackageResultsEqual(dynamic a, dynamic b)
        {
            if (a is PSObject)
            {
                a = (a as PSObject).BaseObject;
            }

            if (b is PSObject)
            {
                b = (b as PSObject).BaseObject;
            }

            Assert.Equal(a.Id, b.Id);
            Assert.Equal(a.Version, b.Version);
        }

        private static FindPackageCommand BuildCmdlet(bool isSolutionOpen = true, IPackageRepository repository = null)
        {
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();

            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(GetPackageManager);
            return new FindPackageCommand(
                GetRepositoryFactory(repository),
                GetSourceProvider(),
                TestUtils.GetSolutionManager(isSolutionOpen: isSolutionOpen),
                packageManagerFactory.Object,
                null);
        }

        private static IPackageRepository GetPackageRepository(IEnumerable<IPackage> packages = null)
        {
            var repository = new MockPackageRepository();
            packages = packages ?? new[] { PackageUtility.CreatePackage("P1", "1.4"), PackageUtility.CreatePackage("P6") };
            repository.AddRange(packages);
            return repository;
        }

        private static IPackageRepositoryFactory GetRepositoryFactory(IPackageRepository repository = null)
        {
            repository = repository ?? GetPackageRepository();

            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(c => c.CreateRepository("foosource")).Returns(repository);
            repositoryFactory.Setup(c => c.CreateRepository("foo")).Returns(repository);

            return repositoryFactory.Object;
        }

        private static IVsPackageManager GetPackageManager()
        {
            var fileSystem = new Mock<IFileSystem>();
            var localRepo = new Mock<ISharedPackageRepository>();
            var localPackages = new[] { PackageUtility.CreatePackage("P1", "0.9"), PackageUtility.CreatePackage("Pack2") };
            localRepo.Setup(c => c.GetPackages()).Returns(localPackages.AsQueryable());

            var remotePackages = new[] { PackageUtility.CreatePackage("P0", "1.1"), PackageUtility.CreatePackage("P1", "1.1"), 
                                         PackageUtility.CreatePackage("Pack2", "1.2"), PackageUtility.CreatePackage("P3") };
            var remoteRepo = new Mock<IPackageRepository>();
            remoteRepo.Setup(c => c.GetPackages()).Returns(remotePackages.AsQueryable());
            return new VsPackageManager(TestUtils.GetSolutionManager(), remoteRepo.Object, new Mock<IFileSystemProvider>().Object, fileSystem.Object, localRepo.Object, new Mock<IDeleteOnRestartManager>().Object, new Mock<VsPackageInstallerEvents>().Object);
        }

        private static IVsPackageSourceProvider GetSourceProvider()
        {
            Mock<IVsPackageSourceProvider> sourceProvider = new Mock<IVsPackageSourceProvider>();
            sourceProvider.Setup(c => c.ActivePackageSource).Returns(new PackageSource("foo", "foosource"));
            return sourceProvider.Object;
        }
    }
}