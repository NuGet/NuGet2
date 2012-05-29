using System;
using System.Globalization;
using System.IO;
using System.Linq;
using NuGet.Test.Mocks;
using Xunit;
using Xunit.Extensions;
using System.Runtime.Versioning;

namespace NuGet.Test
{
    public class PackageReferenceFileTest
    {
        [Fact]
        public void GetPackageReferencesIgnoresEntryIfIdIsNotPresent()
        {
            // Arrange
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id="""" version=""1.0"" />
</packages>";
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            var packageReferenceFile = new PackageReferenceFile(fileSystem, "packages.config");

            // Act
            var values = packageReferenceFile.GetPackageReferences();

            // Assert
            Assert.Empty(values);
        }

        [Fact]
        public void GetPackageReferencesThrowsIfVersionIsNotPresent()
        {
            // Arrange
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version="""" />
</packages>";
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            var packageReferenceFile = new PackageReferenceFile(fileSystem, "packages.config");

            // Act
            var values = packageReferenceFile.GetPackageReferences();

            // Assert
            Assert.Empty(values);
        }

        [Fact]
        public void GetPackageReferencesParseTargetFrameworkCorrectly()
        {
            // Arrange
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" targetFramework=""sl4"" />
  <package id=""B"" version=""1.0"" targetFramework=""net35-client"" />
  <package id=""C"" version=""1.0"" targetFramework=""winrt45"" />
  <package id=""D"" version=""1.0"" />
</packages>";
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            var packageReferenceFile = new PackageReferenceFile(fileSystem, "packages.config");

            // Act
            var values = packageReferenceFile.GetPackageReferences().ToList();

            // Assert
            Assert.Equal(4, values.Count);
            Assert.Equal(new FrameworkName("Silverlight, Version=4.0"), values[0].TargetFramework);
            Assert.Equal(new FrameworkName(".NETFramework, Version=3.5, Profile=Client"), values[1].TargetFramework);
            Assert.Equal(new FrameworkName(".NETCore, Version=4.5"), values[2].TargetFramework);
            Assert.Null(values[3].TargetFramework);
        }

        [Theory]
        [InlineData("abcd")]
        [InlineData("1.24.4%")]
        public void GetPackageReferencesThrowsIfVersionIsInvalid(string version)
        {
            // Arrange
            var config = String.Format(CultureInfo.InvariantCulture, @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""{0}"" />
</packages>", version);
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            var packageReferenceFile = new PackageReferenceFile(fileSystem, "packages.config");

            // Act
            var values = packageReferenceFile.GetPackageReferences();

            // Assert
            ExceptionAssert.Throws<InvalidDataException>(() => values.ToList(), "Unable to parse version value '" + version + "' from 'packages.config'.");
        }

        [Fact]
        public void GetPackageReferencesThrowsIfVersionSpecIsInvalid()
        {
            // Arrange
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.3.4"" allowedVersions=""1.23.4$-2.0"" />
</packages>";
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            var packageReferenceFile = new PackageReferenceFile(fileSystem, "packages.config");

            // Act
            var values = packageReferenceFile.GetPackageReferences();

            // Assert
            ExceptionAssert.Throws<InvalidDataException>(() => values.ToList(), "Unable to parse version value '1.23.4$-2.0' from 'packages.config'.");
        }

        [Fact]
        public void GetPackageReferencesFindLatestEntryCorrectly()
        {
            // Arrange
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.3.4"" />
  <package id=""A"" version=""2.5"" />
  <package id=""B"" version=""1.0"" />
  <package id=""C"" version=""2.1.4"" />
</packages>";
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            var packageReferenceFile = new PackageReferenceFile(fileSystem, "packages.config");

            // Act
            PackageName packageName = packageReferenceFile.FindEntryWithLatestVersionById("A");

            // Assert
            Assert.NotNull(packageName);
            Assert.Equal("A", packageName.Id);
            Assert.Equal(new SemanticVersion("2.5"), packageName.Version);
        }

        [Fact]
        public void GetPackageReferencesFindLatestPrereleaseEntryCorrectly()
        {
            // Arrange
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.3.4"" />
  <package id=""A"" version=""2.5-beta"" />
  <package id=""B"" version=""1.0"" />
  <package id=""C"" version=""2.1.4"" />
</packages>";
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            var packageReferenceFile = new PackageReferenceFile(fileSystem, "packages.config");

            // Act
            PackageName packageName = packageReferenceFile.FindEntryWithLatestVersionById("A");

            // Assert
            Assert.NotNull(packageName);
            Assert.Equal("A", packageName.Id);
            Assert.Equal(new SemanticVersion("2.5-beta"), packageName.Version);
        }

        [Fact]
        public void GetPackageReferencesFindTheOnlyVersionAsLatestVersion()
        {
            // Arrange
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.3.4"" />
  <package id=""A"" version=""2.5-beta"" />
  <package id=""B"" version=""1.0"" />
  <package id=""C"" version=""2.1.4"" />
</packages>";
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            var packageReferenceFile = new PackageReferenceFile(fileSystem, "packages.config");

            // Act
            PackageName packageName = packageReferenceFile.FindEntryWithLatestVersionById("B");

            // Assert
            Assert.NotNull(packageName);
            Assert.Equal("B", packageName.Id);
            Assert.Equal(new SemanticVersion("1.0"), packageName.Version);
        }

        [Fact]
        public void GetPackageReferencesReturnsNullForNonExistentId()
        {
            // Arrange
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.3.4"" />
  <package id=""A"" version=""2.5-beta"" />
  <package id=""B"" version=""1.0"" />
  <package id=""C"" version=""2.1.4"" />
</packages>";
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            var packageReferenceFile = new PackageReferenceFile(fileSystem, "packages.config");

            // Act
            PackageName packageName = packageReferenceFile.FindEntryWithLatestVersionById("nonexistentId");

            // Assert
            Assert.Null(packageName);
        }
    }
}
