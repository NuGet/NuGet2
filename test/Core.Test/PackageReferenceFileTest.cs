using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using NuGet.Resources;
using NuGet.Test.Mocks;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test
{
    public class PackageReferenceFileTest
    {
        [Fact]
        public void ConstructorNormalizeProjectName()
        {
            // Arrange
            var fileSystem = new MockFileSystem("x:\\");
            fileSystem.AddFile("packages.project_with_space.config", "");

            // Act
            var packageReferenceFile = new PackageReferenceFile(
                fileSystem, "packages.config", "project with space");

            // Assert
            Assert.Equal("x:\\packages.project_with_space.config", packageReferenceFile.FullPath);
        }

        [Fact]
        public void GetPackageReferencesNormalizeProjectName()
        {
            // Arrange
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""luan"" version=""1.0"" />
</packages>";

            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            fileSystem.AddFile("packages.project_with_space.config", config);
            var packageReferenceFile = new PackageReferenceFile(fileSystem, "packages.config", "project with space");

            // Act
            var values = packageReferenceFile.GetPackageReferences().ToArray();

            // Assert
            Assert.Equal(1, values.Length);
            Assert.Equal("luan", values[0].Id);
        }

        [Fact]
        public void GetPackageReferencesReadFromProjectConfigFileIfPresent()
        {
            // Arrange
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""luan"" version=""1.0"" />
</packages>";

            var projectConfig = @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""an"" version=""1.0"" />
</packages>";
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            fileSystem.AddFile("packages.project.config", projectConfig);
            var packageReferenceFile = new PackageReferenceFile(fileSystem, "packages.config", "project");

            // Act
            var values = packageReferenceFile.GetPackageReferences().ToArray();

            // Assert
            Assert.Equal(1, values.Length);
            Assert.Equal("an", values[0].Id);
        }

        [Fact]
        public void GetPackageReferencesReadFromConfigFileIfProjectConfigFileDoesNotExist()
        {
            // Arrange
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""luan"" version=""1.0"" />
</packages>";

            var projectConfig = @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""an"" version=""1.0"" />
</packages>";
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            fileSystem.AddFile("packages.project.config", projectConfig);
            var packageReferenceFile = new PackageReferenceFile(fileSystem, "packages.config", "chocolate");

            // Act
            var values = packageReferenceFile.GetPackageReferences().ToArray();

            // Assert
            Assert.Equal(1, values.Length);
            Assert.Equal("luan", values[0].Id);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetPackageReferencesIgnoresEntryIfIdIsNotPresent(bool requireVersion)
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
            var values = packageReferenceFile.GetPackageReferences(requireVersion);

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
            ExceptionAssert.Throws<InvalidDataException>(() => values.ToList(), "Unable to parse version value '' from 'packages.config'.");
        }

        [Fact]
        public void GetPackageReturnsReferencesWithEmptyVersionsWhenRequiredVersionIsFalse()
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
            var values = packageReferenceFile.GetPackageReferences(requireVersion: false);

            // Assert
            Assert.Equal(1, values.Count());
            Assert.Equal("A", values.First().Id);
            Assert.Null(values.First().Version);
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

        [Theory]
        [InlineData("foo")]
        [InlineData("bar")]
        [InlineData("baz")]
        [InlineData("on")]
        [InlineData("off")]
        [InlineData("yes")]
        [InlineData("no")]
        [InlineData("0")]
        [InlineData("1")]
        public void GetPackageReferencesThrowsIfDevelopmentFlagIsInvalid(string text)
        {
            // Arrange
            var configFormat = @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.3.4"" developmentDependency=""{0}"" />
</packages>";

            var config = string.Format(CultureInfo.InvariantCulture, configFormat, text);
            var expectedMessage = string.Format(CultureInfo.InvariantCulture, "Unable to parse developmentDependency value '{0}' from 'packages.config'.", text);

            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            var packageReferenceFile = new PackageReferenceFile(fileSystem, "packages.config");

            // Act
            var exception = Record.Exception(() => packageReferenceFile.GetPackageReferences().ToList());

            // Assert
            Assert.IsType<InvalidDataException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Theory]
        [InlineData("foo")]
        [InlineData("bar")]
        [InlineData("baz")]
        [InlineData("on")]
        [InlineData("off")]
        [InlineData("yes")]
        [InlineData("no")]
        [InlineData("0")]
        [InlineData("1")]
        public void GetPackageReferencesThrowsIfRequireReinstallationFlagIsInvalid(string text)
        {
            // Arrange
            var configFormat = @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.3.4"" requireReinstallation=""{0}"" />
</packages>";

            var config = string.Format(CultureInfo.InvariantCulture, configFormat, text);
            var expectedMessage = string.Format(CultureInfo.CurrentCulture, NuGetResources.ReferenceFile_InvalidRequireReinstallationFlag, text, "packages.config");

            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            var packageReferenceFile = new PackageReferenceFile(fileSystem, "packages.config");

            // Act
            var exception = Record.Exception(() => packageReferenceFile.GetPackageReferences().ToList());

            // Assert
            Assert.IsType<InvalidDataException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void GetPackageReferencesParsePackageRequireReinstallationAndReturnsTrue()
        {
            // Arrange
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" targetFramework=""sl4"" requireReinstallation=""true"" />
  <package id=""B"" version=""1.0"" targetFramework=""sl4"" requireReinstallation=""TruE"" />
  <package id=""C"" version=""1.0"" targetFramework=""sl4"" requireReinstallation=""TRUE"" />
</packages>";

            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            var packageReferenceFile = new PackageReferenceFile(fileSystem, "packages.config");

            // Act
            var values = packageReferenceFile.GetPackageReferences().ToList();

            // Assert
            Assert.Equal(3, values.Count);
            Assert.True(values[0].RequireReinstallation);
            Assert.True(values[1].RequireReinstallation);
            Assert.True(values[2].RequireReinstallation);
        }

        [Fact]
        public void GetPackageReferencesParsePackageRequireReinstallationAndReturnsTrueAndFalse()
        {
            // Arrange
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0""  targetFramework=""net45"" requireReinstallation=""false"" />
  <package id=""B"" version=""1.0""  targetFramework=""net45""/>
  <package id=""B"" version=""1.0""  targetFramework=""net45"" requireReinstallation=""true""/>
</packages>";

            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            var packageReferenceFile = new PackageReferenceFile(fileSystem, "packages.config");

            // Act
            var values = packageReferenceFile.GetPackageReferences().ToList();

            // Assert
            Assert.Equal(3, values.Count);
            Assert.False(values[0].RequireReinstallation);
            Assert.False(values[1].RequireReinstallation);
            Assert.True(values[2].RequireReinstallation);
        }

        [Fact]
        public void GetPackageReferenceParsesRequireReinstallationWithDifferentCasing()
        {
            // Arrange
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0""  targetFramework=""net45"" RequireReINSTALLation=""false"" />
</packages>";

            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            var packageReferenceFile = new PackageReferenceFile(fileSystem, "packages.config");

            // Act
            var values = packageReferenceFile.GetPackageReferences().ToList();

            // Assert
            Assert.Equal(1, values.Count);
            Assert.False(values[0].RequireReinstallation);
        }

        [Fact]
        public void MarkEntryForReinstallationMarksPackagesProperly()
        {
            // Arrange
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" targetFramework=""net45""/>
  <package id=""B"" version=""1.0"" targetFramework=""net45""/>
</packages>";

            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            var packageReferenceFile = new PackageReferenceFile(fileSystem, "packages.config");

            // Act
            packageReferenceFile.MarkEntryForReinstallation("A", new SemanticVersion("1.0"), new FrameworkName(".NETFramework, Version=4.5"), true);
            var packageReferences = packageReferenceFile.GetPackageReferences().ToList();

            // Assert
            Assert.Equal(2, packageReferences.Count);
            Assert.True(packageReferences[0].RequireReinstallation);
            Assert.False(packageReferences[1].RequireReinstallation);
        }
    }
}
