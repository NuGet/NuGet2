using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Versioning;
using System.Threading;
using Moq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{
    public class PackageRepositoryExtensionsTest
    {
        [Fact]
        public void FindPackagesByIdUsesPackageLookupIfAvailable()
        {
            // Arrange
            var repository = new Mock<IPackageLookup>();
            repository.Setup(p => p.FindPackagesById("A")).Returns(new IPackage[0]).Verifiable();

            // Act
            PackageRepositoryExtensions.FindPackagesById(repository.As<IPackageRepository>().Object, "A");

            // Assert
            repository.Verify();
        }

        [Fact]
        public void ExistsMethodUsesIPackageLookupIfUnderlyingRepositorySupportsIt()
        {
            // Arrange
            var repository = new Mock<IPackageRepository>(MockBehavior.Strict);

            repository.As<IPackageLookup>()
                      .Setup(p => p.Exists("A", new SemanticVersion("1.0")))
                      .Returns(true)
                      .Verifiable();

            // Act
            bool result = PackageRepositoryExtensions.Exists(repository.Object, "A", new SemanticVersion("1.0"));

            // Assert
            Assert.True(result);
            repository.Verify();
        }

        [Fact]
        public void SearchUsesOptimalDataServiceCodePathIfServerDoesSupportServiceMethod()
        {
            // Arrange
            var client = new Mock<IHttpClient>();
            var context = new Mock<IDataServiceContext>();
            var query = new Mock<IDataServiceQuery<DataServicePackage>>();
            var repository = new Mock<DataServicePackageRepository>(MockBehavior.Strict, client.Object);
            repository.Object.Context = context.Object;
            context.Setup(m => m.SupportsServiceMethod(It.IsAny<string>())).Returns(true);
            context.Setup(m => m.CreateQuery<DataServicePackage>(It.IsAny<string>(), It.IsAny<Dictionary<string,object>>())).Returns(query.Object);
            query.Setup(q => q.GetRequest(It.IsAny<Expression>())).Returns((System.Data.Services.Client.DataServiceRequest)null);
            query.Setup(q => q.RequiresBatch(It.IsAny<Expression>())).Returns(false);
            query.Setup(q => q.CreateQuery<DataServicePackage>(It.IsAny<Expression>())).Returns(query.Object);
            query.Setup(q => q.GetEnumerator()).Returns(
                new List<DataServicePackage> { 
                    new DataServicePackage
                    {
                        Id = "B",
                        Description = "old and bad",
                    }
                }.GetEnumerator());

            // Act
            var abstractedRepository = (IPackageRepository)repository.Object;
            var results = abstractedRepository.FindPackagesById("B").ToList();

            // Assert
            Assert.Equal(1, results.Count);
            Assert.Equal("B", results[0].Id);
        }

        [Fact]
        public void FindPackagesByIdRecognizeICultureAwareRepositoryInterface()
        {
            var turkeyCulture = new CultureInfo("tr-TR");

            // Arrange
            var packages = new IPackage[] 
            { 
                PackageUtility.CreatePackage("YUI"), 
                PackageUtility.CreatePackage("DUI")
            };

            var repository = new Mock<IPackageRepository>();
            repository.Setup(p => p.GetPackages()).Returns(packages.AsQueryable());

            var savedCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                // simulate running on Turkish locale
                Thread.CurrentThread.CurrentCulture = turkeyCulture;

                // Act
                // notice the lowercase Turkish I character in the packageId to search for
                var foundPackages = PackageRepositoryExtensions.FindPackagesById(repository.Object, "yuı").ToList();

                // Assert
                Assert.Equal(1, foundPackages.Count);
                Assert.Equal("YUI", foundPackages[0].Id);
            }
            finally
            {
                // restore culture
                Thread.CurrentThread.CurrentCulture = savedCulture;
            }
        }

        [Fact]
        public void GetUpdatesReturnAllPackageVersionsWhenFlagIsSpecified()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0", new string[] { "hello" }));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "2.0", new string[] { "hello" }));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "3.0", new string[] { "hello" }));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "3.0-alpha", new string[] { "hello" }));

            var packages = new IPackage[] 
            {
                PackageUtility.CreatePackage("A", "1.5")
            };

            // Act
            var foundPackages = PackageRepositoryExtensions.GetUpdates(sourceRepository, packages, includePrerelease: true, targetFrameworks: Enumerable.Empty<FrameworkName>(),
                                                                       includeAllVersions: true).ToList();

            // Assert
            Assert.Equal(3, foundPackages.Count);

            Assert.Equal("A", foundPackages[0].Id);
            Assert.Equal(new SemanticVersion("2.0"), foundPackages[0].Version);

            Assert.Equal("A", foundPackages[1].Id);
            Assert.Equal(new SemanticVersion("3.0"), foundPackages[1].Version);

            Assert.Equal("A", foundPackages[2].Id);
            Assert.Equal(new SemanticVersion("3.0-alpha"), foundPackages[2].Version);
        }

        [Fact]
        public void GetUpdatesReturnPackageWhenSourcePackageHasNullTargetFrameworkInToolsFolder()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0", new string[] { "hello" }));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "2.0", new string[] { "hello", "world" }, tools: new string[] { "build\\install.ps1" }));

            var packages = new IPackage[] 
            {
                PackageUtility.CreatePackage("A", "1.5")
            };

            // Act
            var foundPackages = PackageRepositoryExtensions.GetUpdates(sourceRepository, packages, includePrerelease: true, targetFrameworks:new [] { new FrameworkName("NETFramework, Version=4.5") },
                                                                       includeAllVersions: true).ToList();

            // Assert
            Assert.Equal(1, foundPackages.Count);

            Assert.Equal("A", foundPackages[0].Id);
            Assert.Equal(new SemanticVersion("2.0"), foundPackages[0].Version);
        }   

        [Fact]
        public void GetUpdatesReturnPackagesConformingToVersionConstraints()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0", new string[] { "hello" }));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "2.0", new string[] { "hello" }));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "3.0", new string[] { "hello" }));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "3.0-alpha", new string[] { "hello" }));

            var packages = new IPackage[] 
            {
                PackageUtility.CreatePackage("A", "1.5")
            };

            // Act
            var foundPackages = PackageRepositoryExtensions.GetUpdates(
                sourceRepository,
                packages,
                includePrerelease: true,
                includeAllVersions: true,
                targetFrameworks: Enumerable.Empty<FrameworkName>(),
                versionConstraints: new[] { VersionUtility.ParseVersionSpec("(0.0,3.0)") }
                ).ToList();

            // Assert
            Assert.Equal(2, foundPackages.Count);

            Assert.Equal("A", foundPackages[0].Id);
            Assert.Equal(new SemanticVersion("2.0"), foundPackages[0].Version);

            Assert.Equal("A", foundPackages[1].Id);
            Assert.Equal(new SemanticVersion("3.0-alpha"), foundPackages[1].Version);
        }

        [Fact]
        public void GetUpdatesReturnPackagesUseCorrectConstraintsCorrespondingToIds()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0", new string[] { "hello" }));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "2.0", new string[] { "hello" }));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "3.0", new string[] { "hello" }));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "3.0-alpha", new string[] { "hello" }));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B", "1.0-alpha", new string[] { "hello" }));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B", "2.0", new string[] { "hello" }));

            var packages = new IPackage[] 
            {
                PackageUtility.CreatePackage("A", "1.5"),
                PackageUtility.CreatePackage("B", "1.0")
            };

            // Act
            var foundPackages = PackageRepositoryExtensions.GetUpdates(
                sourceRepository,
                packages,
                includePrerelease: true,
                includeAllVersions: false,
                targetFrameworks: Enumerable.Empty<FrameworkName>(),
                versionConstraints: new[] { VersionUtility.ParseVersionSpec("(0.0,3.0)"), VersionUtility.ParseVersionSpec("(2.0,)") }
                ).ToList();

            // Assert
            Assert.Equal(1, foundPackages.Count);

            Assert.Equal("A", foundPackages[0].Id);
            Assert.Equal(new SemanticVersion("3.0-alpha"), foundPackages[0].Version);
        }

        [Fact]
        public void GetUpdatesThrowsIfPackagdIdsAndVersionConstraintsHaveDifferentNumberOfElements()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0", new string[] { "hello" }));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "2.0", new string[] { "hello" }));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "3.0", new string[] { "hello" }));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "3.0-alpha", new string[] { "hello" }));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B", "1.0-alpha", new string[] { "hello" }));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B", "2.0", new string[] { "hello" }));

            var packages = new IPackage[] 
            {
                PackageUtility.CreatePackage("A", "1.5"),
                PackageUtility.CreatePackage("B", "1.0")
            };

            // Act
            Assert.Throws<ArgumentException>(() => PackageRepositoryExtensions.GetUpdates(
                sourceRepository,
                packages,
                includePrerelease: true,
                includeAllVersions: false,
                targetFrameworks: Enumerable.Empty<FrameworkName>(),
                versionConstraints: new[] { VersionUtility.ParseVersionSpec("(2.0,)") }
                ));
        }
    }
}