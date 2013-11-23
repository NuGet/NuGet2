using System;
using System.Linq;
using System.Runtime.Versioning;
using Moq;
using NuGet;
using NuGet.WebMatrix.Tests.Utilities;
using Xunit;

namespace NuGet.WebMatrix.DependentTests.CoreTests
{
    public class WebProjectManagerTest
    {
        [Fact]
        public void GetRemotePackagesTest()
        {
            // Arrange
            var siteRoot = "x:\\";
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockServiceBasePackageRepository();
            var projectSystem = new WebProjectSystem(siteRoot);
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(sourceRepository, pathResolver, projectSystem, localRepository);

            var net40 = new FrameworkName(".NETFramework", new Version("4.0.0.0"));
            var net45 = new FrameworkName(".NETFramework", new Version("4.5.0.0"));

            IPackage packageA = PackageFactory.Create("A", new Version("1.0"), null, new FrameworkName[]{ net40 });
            IPackage packageB = PackageFactory.Create("B", new Version("1.0"), null, new FrameworkName[]{ net45 });
            IPackage packageC = PackageFactory.Create("C", new Version("1.0"));

            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageC);

            // NOTE THAT target framework used by WebProjectManager would be .NETFramework, Version=4.0.0.0
            var webProjectManager = new WebProjectManager(projectManager, siteRoot);

            // Act
            var packages = webProjectManager.GetRemotePackages(null, false).ToList();

            // Assert
            Assert.Equal(2, packages.Count);
            Assert.Equal(packageA, packages[0]);
            Assert.Equal(packageC, packages[1]);
        }

        [Fact]
        public void GetPackagesWithUpdatesTest()
        {
            // Arrange
            var siteRoot = "x:\\";
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockServiceBasePackageRepository();
            var projectSystem = new WebProjectSystem(siteRoot);
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(sourceRepository, pathResolver, projectSystem, localRepository);

            var net40 = new FrameworkName(".NETFramework", new Version("4.0.0.0"));
            var net45 = new FrameworkName(".NETFramework", new Version("4.5.0.0"));

            IPackage packageA1 = PackageFactory.Create("A", new Version("1.0"), null);
            IPackage packageA2 = PackageFactory.Create("A", new Version("2.0"), null, new FrameworkName[] { net45 });
            IPackage packageB1 = PackageFactory.Create("B", new Version("1.0"), null);
            IPackage packageB2 = PackageFactory.Create("B", new Version("2.0"), null, new FrameworkName[] { net40 });

            sourceRepository.AddPackage(packageA1);
            sourceRepository.AddPackage(packageA2);
            sourceRepository.AddPackage(packageB1);
            sourceRepository.AddPackage(packageB2);

            localRepository.AddPackage(packageA1);
            localRepository.AddPackage(packageB1);

            // NOTE THAT target framework used by WebProjectManager would be .NETFramework, Version=4.0.0.0
            var webProjectManager = new WebProjectManager(projectManager, siteRoot);

            // Act
            var packages = webProjectManager.GetPackagesWithUpdates(null, false).ToList();

            // Assert
            Assert.Equal(1, packages.Count);
            Assert.Equal(packageB2, packages[0]);
        }
    }
}
