using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Moq;
using NuGet.Dialog.Providers;
using NuGet.Test;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Dialog.Test
{

    public class UpdatesTreeNodeTest
    {
        [Fact]
        public void PropertyNameIsCorrect()
        {

            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            MockPackageRepository sourceRepository = new MockPackageRepository();

            string category = "Mock node";
            UpdatesTreeNode node = CreateUpdatesTreeNode(localRepository, sourceRepository, includePrerelease: true, category: category);

            // Act & Assert
            Assert.Equal(category, node.Name);
        }

        [Fact]
        public void GetPackagesReturnsUpdatesForPackages()
        {
            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            MockPackageRepository sourceRepository = new MockPackageRepository();

            UpdatesTreeNode node = CreateUpdatesTreeNode(localRepository, sourceRepository, includePrerelease: false);

            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));

            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.5"));

            // Act
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            // Assert
            Assert.Equal(1, packages.Count);
            AssertPackage(packages[0], "A", "1.5");
        }

        [Fact]
        public void GetPackagesReturnsNoResultsIfPackageDoesNotExistInSourceRepository()
        {
            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            MockPackageRepository sourceRepository = new MockPackageRepository();

            UpdatesTreeNode node = CreateUpdatesTreeNode(localRepository, sourceRepository, includePrerelease: false);

            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));

            sourceRepository.AddPackage(PackageUtility.CreatePackage("B", "1.5"));

            // Act
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            // Assert
            Assert.Equal(0, packages.Count);
        }

        [Fact]
        public void GetPackagesIngoresLowerVersions()
        {

            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            MockPackageRepository sourceRepository = new MockPackageRepository();

            UpdatesTreeNode node = CreateUpdatesTreeNode(localRepository, sourceRepository, includePrerelease: false);

            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));

            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.5"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "0.9"));

            // Act
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            // Assert
            Assert.Equal(1, packages.Count);
            AssertPackage(packages[0], "A", "1.5");
        }

        [Fact]
        public void GetPackagesReturnsUpdatesForEachPackageFoundInTheSourceRepository()
        {

            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            MockPackageRepository sourceRepository = new MockPackageRepository();

            UpdatesTreeNode node = CreateUpdatesTreeNode(localRepository, sourceRepository, includePrerelease: false);

            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            localRepository.AddPackage(PackageUtility.CreatePackage("B", "1.0"));

            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.5"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.9"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B", "2.0"));

            // Act
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            // Assert
            Assert.Equal(2, packages.Count);
            AssertPackage(packages[0], "A", "1.9");
            AssertPackage(packages[1], "B", "2.0");
        }

        [Fact]
        public void GetPackagesReturnsPrereleasePackagesIfIncludePrereleaseIsTrue()
        {
            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            MockPackageRepository sourceRepository = new MockPackageRepository();

            UpdatesTreeNode node = CreateUpdatesTreeNode(localRepository, sourceRepository, includePrerelease: true);

            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            localRepository.AddPackage(PackageUtility.CreatePackage("B", "1.0"));

            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.5"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.9-alpha"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B", "2.0"));

            // Act
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            // Assert
            Assert.Equal(2, packages.Count);
            AssertPackage(packages[0], "A", "1.9-alpha");
            AssertPackage(packages[1], "B", "2.0");
        }

        [Fact]
        public void GetPackagesOnlyReturnPackagesCompatibleWithTheProjects()
        {
            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            localRepository.AddPackage(PackageUtility.CreatePackage("B", "1.0"));

            MockPackageRepository sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.5"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.9", assemblyReferences: new string[] { "lib\\sl4\\a.dll" }));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B", "2.0", assemblyReferences: new string[] { "lib\\net20\\b.dll" }));

            PackagesProviderBase provider = new MockPackagesProvider(new string[] { ".NETFramework,Version=3.0" });

            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            var node = new UpdatesTreeNode(provider, "Mock", parentTreeNode, localRepository, sourceRepository);

            // Act
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            // Assert
            Assert.Equal(2, packages.Count);
            AssertPackage(packages[0], "A", "1.5");
            AssertPackage(packages[1], "B", "2.0");
        }

        [Fact]
        public void GetPackagesFindsUpdatesForFilteredSetOfPackages()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("Foo", "1.0");
            var packageB = PackageUtility.CreatePackage("Qux", "1.0");
            var localRepository = new MockPackageRepository { packageA, packageB };
            IEnumerable<IPackage> actual = null;

            var sourceRepository = new Mock<IServiceBasedRepository>(MockBehavior.Strict);
            sourceRepository.Setup(s => s.GetUpdates(It.IsAny<IEnumerable<IPackage>>(), true, false, It.IsAny<IEnumerable<FrameworkName>>()))
                            .Returns(new[] { PackageUtility.CreatePackage("Foo", "1.1") })
                            .Callback((IEnumerable<IPackage> a, bool includePrerelease, bool includeAllVersions, IEnumerable<FrameworkName> frameworks) => actual = a)
                            .Verifiable();

            PackagesProviderBase provider = new MockPackagesProvider(new string[] { ".NETFramework,Version=3.0" });

            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            var node = new UpdatesTreeNode(provider, "Mock", parentTreeNode, localRepository, sourceRepository.As<IPackageRepository>().Object);

            // Act
            var result = node.GetPackages(searchTerm: "Foo", allowPrereleaseVersions: true).ToList();

            // Assert
            sourceRepository.Verify();
            Assert.Equal(new[] { packageA }, actual);
            AssertPackage(result.Single(), "Foo", "1.1");
        }

        private static void AssertPackage(IPackage package, string id, string version = null)
        {
            Assert.NotNull(package);
            Assert.Equal(id, package.Id);
            if (version != null)
            {
                Assert.Equal(new SemanticVersion(version), package.Version);
            }
        }

        private static UpdatesTreeNode CreateUpdatesTreeNode(IPackageRepository localRepository, IPackageRepository sourceRepository, bool includePrerelease, string category = "Mock node")
        {
            PackagesProviderBase provider = new MockPackagesProvider();
            provider.IncludePrerelease = includePrerelease;
            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            return new UpdatesTreeNode(provider, category, parentTreeNode, localRepository, sourceRepository);
        }
    }
}
