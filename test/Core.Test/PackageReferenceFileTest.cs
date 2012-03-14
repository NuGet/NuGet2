using System;
using System.Globalization;
using System.IO;
using System.Linq;
using NuGet.Test.Mocks;
using Xunit;
using Xunit.Extensions;

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
        public void GetEntryVersionReturnsFalseForNonExistingId()
        {
            // Arrange
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.3.4"" />
</packages>";
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            var packageReferenceFile = new PackageReferenceFile(fileSystem, "packages.config");

            // Act
            SemanticVersion version;
            var result = packageReferenceFile.GetEntryVersion("B", out version);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetEntryVersionReturnsTrueAndCorrectVersion()
        {
            // Arrange
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.3.4"" />
</packages>";
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            var packageReferenceFile = new PackageReferenceFile(fileSystem, "packages.config");

            // Act
            SemanticVersion version;
            var result = packageReferenceFile.GetEntryVersion("A", out version);

            // Assert
            Assert.True(result);
            Assert.Equal(new SemanticVersion("1.3.4"), version);
        }
    }
}
