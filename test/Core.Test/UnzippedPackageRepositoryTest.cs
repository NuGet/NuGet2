using System;
using System.IO;
using System.Linq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{
    public class UnzippedPackageRepositoryTest
    {
        [Fact]
        public void SupportsPrereleasePackagesReturnsTrue()
        {
            // Arrange
            var fileSystem = new MockFileSystem("c:\\");
            var pathResolver = new DefaultPackagePathResolver("c:\\");

            // Act
            var repository = new UnzippedPackageRepository(pathResolver, fileSystem);

            // Assert
            Assert.True(repository.SupportsPrereleasePackages);
        }

        [Fact]
        public void GetPackagesReturnsAllValidPackages()
        {
            // Arrange
            var fileSystem = new MockFileSystem("c:\\");
            AddPackage(fileSystem, "A", "1.0");
            AddPackage(fileSystem, "B", "1.0-alpha");
            AddPackage(fileSystem, "C", "2.0.1-RC");
            AddPackage(fileSystem, "D", "3.0");

            // these are invalid packages (missing corresponding directory)
            fileSystem.AddFile("AA.2.0.nupkg");
            fileSystem.AddFile("BB.4.0.nupkg");
            fileSystem.CreateDirectory("BB.3.0");

            var pathResolver = new DefaultPackagePathResolver("c:\\");
            var repository = new UnzippedPackageRepository(pathResolver, fileSystem);

            // Act
            var packages = repository.GetPackages().ToList();

            // Assert
            Assert.Equal(4, packages.Count);
            AssertPackage(packages[0], "A", new SemanticVersion("1.0"));
            AssertPackage(packages[1], "B", new SemanticVersion("1.0.0-alpha"));
            AssertPackage(packages[2], "C", new SemanticVersion("2.0.1-RC"));
            AssertPackage(packages[3], "D", new SemanticVersion("3.0"));
        }

        [Fact]
        public void FindPackageFindTheRightPackage()
        {
            // Arrange
            var fileSystem = new MockFileSystem("c:\\");
            AddPackage(fileSystem, "A", "1.0");
            AddPackage(fileSystem, "B", "1.0-alpha");
            AddPackage(fileSystem, "C", "2.0.1-RC");
            AddPackage(fileSystem, "D", "3.0");

            // these are invalid packages (missing corresponding directory)
            fileSystem.AddFile("AA.2.0.nupkg");
            fileSystem.AddFile("BB.4.0.nupkg");
            fileSystem.CreateDirectory("BB.3.0");

            var pathResolver = new DefaultPackagePathResolver("c:\\");
            var repository = new UnzippedPackageRepository(pathResolver, fileSystem);

            // Act
            var packageA = repository.FindPackage("A", new SemanticVersion("1.0"));
            var packageB = repository.FindPackage("B", new SemanticVersion("1.0-alpha"));

            // Assert
            AssertPackage(packageA, "A", new SemanticVersion("1.0"));
            AssertPackage(packageB, "B", new SemanticVersion("1.0.0-alpha"));
        }

        [Fact]
        public void FindPackageReturnsNullForNonExistingPackage()
        {
            // Arrange
            var fileSystem = new MockFileSystem("c:\\");
            AddPackage(fileSystem, "A", "1.0");
            AddPackage(fileSystem, "B", "1.0-alpha");
            AddPackage(fileSystem, "C", "2.0.1-RC");
            AddPackage(fileSystem, "D", "3.0");

            // these are invalid packages (missing corresponding directory)
            fileSystem.AddFile("AA.2.0.nupkg");
            fileSystem.AddFile("BB.4.0.nupkg");
            fileSystem.CreateDirectory("BB.3.0");

            var pathResolver = new DefaultPackagePathResolver("c:\\");
            var repository = new UnzippedPackageRepository(pathResolver, fileSystem);

            // Act
            var packageA = repository.FindPackage("A", new SemanticVersion("2.0"));
            var packageB = repository.FindPackage("BBB", new SemanticVersion("1.0-alpha"));

            // Assert
            Assert.Null(packageA);
            Assert.Null(packageB);
        }

        [Fact]
        public void GetPackagePublishedTime()
        {
            // Arrange
            var fileSystem = new MockFileSystem("c:\\");
            AddPackage(fileSystem, "A", "1.0");

            // Act
            var pathResolver = new DefaultPackagePathResolver("c:\\");
            var repository = new UnzippedPackageRepository(pathResolver, fileSystem);
            var packages = repository.GetPackages().ToList();

            // Assert            
            var time = fileSystem.GetLastModified(@"A.1.0\A.1.0.nuspec");
            Assert.Equal(time, packages[0].Published);
        }

        private void AssertPackage(IPackage package, string id, SemanticVersion version)
        {
            Assert.NotNull(package);
            Assert.Equal(id, package.Id, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(version, package.Version);
        }

        private void AddPackage(MockFileSystem fileSystem, string id, string version)
        {
            string packageName = id + "." + version;
            fileSystem.AddFile(packageName + Constants.PackageExtension, "aaa");
            fileSystem.CreateDirectory(packageName);
            fileSystem.AddFile(
                Path.Combine(packageName, packageName + Constants.ManifestExtension),
                CreatePackageManifestContent(id, version));
        }

        private string CreatePackageManifestContent(string id, string version)
        {
            string template = @"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>{0}</id>
    <version>{1}</version>
    <authors>dotnetjunky</authors>
    <owners />    
    <title />
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>My package description.</description>
  </metadata>
</package>";
            return String.Format(template, id, version);
        }
    }
}
