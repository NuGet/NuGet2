using System.Collections.Generic;
using System.Linq;
using Moq;
using NuGet.Commands;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test.NuGetCommandLine.Commands
{
    public class UpdateCommandTest
    {
        [Fact]
        public void UpdatePackageAddsPackagesToSharedPackageRepositoryWhenReferencesAreAdded()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var constraintProvider = NullConstraintProvider.Instance;
            var fileSystem = new MockFileSystem();
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            var projectSystem = new MockProjectSystem();
            var packages = new List<IPackage>();
            var package_A10 = PackageUtility.CreatePackage("A", "1.0", content: new[] { "1.txt" });
            var package_A12 = PackageUtility.CreatePackage("A", "1.2", content: new[] { "1.txt" });
            localRepository.Add(package_A10);
            sourceRepository.Add(package_A12);

            
            var sharedRepository = new Mock<ISharedPackageRepository>(MockBehavior.Strict);
            sharedRepository.Setup(s => s.AddPackage(package_A12)).Callback<IPackage>(p => packages.Add(p)).Verifiable();
            sharedRepository.Setup(s => s.GetPackages()).Returns(packages.AsQueryable());
            
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(s => s.CreateRepository(It.IsAny<string>())).Returns(sourceRepository);
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            packageSourceProvider.Setup(s => s.LoadPackageSources()).Returns(new[] { new PackageSource("foo-source") });

            var updateCommand = new UpdateCommand()
            {
                RepositoryFactory = repositoryFactory.Object,
                SourceProvider = packageSourceProvider.Object
            };

            // Act
            updateCommand.UpdatePackages(localRepository, fileSystem, sharedRepository.Object, sourceRepository, constraintProvider, pathResolver, projectSystem);

            // Assert
            sharedRepository.Verify();
        }

        [Fact]
        public void UpdatePackageUpdatesPackagesWithCommonDependency()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var constraintProvider = NullConstraintProvider.Instance;
            var fileSystem = new MockFileSystem();
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            var projectSystem = new MockProjectSystem();
            var packages = new List<IPackage>();

            var package_A10 = PackageUtility.CreatePackage("A", "1.0", content: new[] { "A.txt" }, dependencies: new[] { new PackageDependency("C", VersionUtility.ParseVersionSpec("1.0")) });
            var package_B10 = PackageUtility.CreatePackage("B", "1.0", content: new[] { "B.txt" }, dependencies: new[] { new PackageDependency("C", VersionUtility.ParseVersionSpec("1.0")) });
            var package_A12 = PackageUtility.CreatePackage("A", "1.2", content: new[] { "A.txt" }, dependencies: new[] { new PackageDependency("C", VersionUtility.ParseVersionSpec("1.0")) });
            var package_B20 = PackageUtility.CreatePackage("B", "2.0", content: new[] { "B.txt" });
            var package_C10 = PackageUtility.CreatePackage("C", "1.0", content: new[] { "C.txt" });
            localRepository.AddRange(new[] { package_A10, package_B10, package_C10});
            sourceRepository.AddRange(new[] { package_A12, package_B20 });

            var sharedRepository = new Mock<ISharedPackageRepository>(MockBehavior.Strict);
            sharedRepository.Setup(s => s.AddPackage(package_A12)).Callback<IPackage>(p => packages.Add(p)).Verifiable();
            sharedRepository.Setup(s => s.AddPackage(package_B20)).Callback<IPackage>(p => packages.Add(p)).Verifiable();
            sharedRepository.Setup(s => s.GetPackages()).Returns(packages.AsQueryable());
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(s => s.CreateRepository(It.IsAny<string>())).Returns(sourceRepository);
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            packageSourceProvider.Setup(s => s.LoadPackageSources()).Returns(new[] { new PackageSource("foo-source") });

            var updateCommand = new UpdateCommand()
            {
                RepositoryFactory = repositoryFactory.Object,
                SourceProvider = packageSourceProvider.Object
            };

            // Act
            updateCommand.UpdatePackages(localRepository, fileSystem, sharedRepository.Object, sourceRepository, constraintProvider, pathResolver, projectSystem);

            // Assert
            sharedRepository.Verify();
        }
    }
}
