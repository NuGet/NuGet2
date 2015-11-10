using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using NuGet.Test.Mocks;
using Xunit;
using Moq;

namespace NuGet.Test
{
    public class UnzippedPackageTest
    {
        [Fact]
        public void CtorThrowIfFileSystemIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new UnzippedPackage((IFileSystem)null, "jQuery"));
        }

        [Fact]
        public void CtorThrowIfPackageNameIsNull()
        {
            Assert.Throws<ArgumentException>(() => new UnzippedPackage(new MockFileSystem(), null));
            Assert.Throws<ArgumentException>(() => new UnzippedPackage(new MockFileSystem(), ""));
        }

        [Fact]
        public void MissingManifestFileThrows()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.CreateDirectory("jQuery.2.0.0");
            fileSystem.AddFile("jQuery.2.0.0.nupkg");

            // Act & Assert
            Assert.Throws(typeof(InvalidOperationException), () => new UnzippedPackage(fileSystem, "jQuery.2.0.0"));
        }

        [Fact]
        public void EnsureManifestIsParsedCorrectly()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("Jumpo.1.0.0.nupkg");
            fileSystem.CreateDirectory("Jumpo.1.0.0");
            fileSystem.AddFile("Jumpo.1.0.0\\Jumpo.1.0.0.nuspec", GetCompleteManifestContent());

            // Act
            var package = new UnzippedPackage(fileSystem, "Jumpo.1.0.0");

            // Assert
            Assert.Equal("Jumpo", package.Id);
            Assert.Equal(new SemanticVersion("1.0.0"), package.Version);
            Assert.Equal(1, package.Authors.Count());
            Assert.Equal("dotnetjunky", package.Authors.ElementAt(0));
            Assert.Equal(1, package.Owners.Count());
            Assert.Equal("Outercurve", package.Owners.ElementAt(0));
            Assert.Equal("http://www.nuget.com/license", package.LicenseUrl.ToString());
            Assert.Equal("http://www.nuget.com/", package.ProjectUrl.ToString());
            Assert.Equal("http://www.outercurve.com/", package.IconUrl.ToString());
            Assert.Equal(1, package.DependencySets.Count());
            Assert.Equal("bing", package.DependencySets.ElementAt(0).Dependencies.ElementAt(0).Id);
            Assert.Equal(new SemanticVersion("1.0-RC"), package.DependencySets.ElementAt(0).Dependencies.ElementAt(0).VersionSpec.MinVersion);
            Assert.True(package.DependencySets.ElementAt(0).Dependencies.ElementAt(0).VersionSpec.IsMinInclusive);
            Assert.Equal(null, package.DependencySets.ElementAt(0).Dependencies.ElementAt(0).VersionSpec.MaxVersion);
            Assert.False(package.DependencySets.ElementAt(0).Dependencies.ElementAt(0).VersionSpec.IsMaxInclusive);
            Assert.Equal("Jumpo Jet", package.Title);
            Assert.True(package.RequireLicenseAcceptance);
            Assert.Equal("My package description.", package.Description);
            Assert.Equal("This is jumpo package.", package.Summary);
            Assert.Equal("New jumpo.", package.ReleaseNotes);
            Assert.Equal("ar-EG", package.Language);
            Assert.Equal(" haha void ", package.Tags);
            Assert.Equal(1, package.FrameworkAssemblies.Count());
            Assert.Equal("System", package.FrameworkAssemblies.ElementAt(0).AssemblyName);
            Assert.Equal(1, package.FrameworkAssemblies.ElementAt(0).SupportedFrameworks.Count());
            Assert.Equal(
                new FrameworkName(".NETFramework", new Version("4.5")), 
                package.FrameworkAssemblies.ElementAt(0).SupportedFrameworks.ElementAt(0));
        }

        [Fact]
        public void GetStreamReturnsCorrectContent()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            AddPackage(fileSystem, "A", "1.0.0");

            var package = new UnzippedPackage(fileSystem, "A.1.0.0");

            // Act
            Stream stream = package.GetStream();

            // Assert
            Assert.NotNull(stream);
            string content = stream.ReadToEnd();
            Assert.Equal("aaa", content);
        }

        [Fact]
        public void GetStreamReturnsCorrectContentWhenNupkgFileIsNestedInsidePackageFolder()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.CreateDirectory("A.1.0.0");
            fileSystem.AddFile("A.1.0.0\\A.1.0.0.nupkg", "aaa");
            fileSystem.AddFile(
                "A.1.0.0\\A.1.0.0.nuspec",
                CreatePackageManifestContent("A", "1.0.0"));

            var package = new UnzippedPackage(fileSystem, "A.1.0.0");

            // Act
            Stream stream = package.GetStream();

            // Assert
            Assert.NotNull(stream);
            string content = stream.ReadToEnd();
            Assert.Equal("aaa", content);
        }

        [Fact]
        public void GetFilesReturnsCorrectFiles()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            AddPackage(fileSystem, "X", "2.0.0-alpha");

            fileSystem.AddFile(@"X.2.0.0-alpha\readme.txt");
            fileSystem.AddFile(@"X.2.0.0-alpha\content\jQuery.js");
            fileSystem.AddFile(@"X.2.0.0-alpha\lib\net45\jQuery.dll");
            fileSystem.AddFile(@"X.2.0.0-alpha\lib\sl3\nunit.exe");
            fileSystem.AddFile(@"X.2.0.0-alpha\tools\install.ps1");
            fileSystem.AddFile(@"X.2.0.0-alpha\tools\init.ps1");

            var package = new UnzippedPackage(fileSystem, "X.2.0.0-alpha");

            // Act
            IList<IPackageFile> files = package.GetFiles().OrderBy(p => p.Path).ToList();

            // Assert
            Assert.Equal(6, files.Count);
            Assert.Equal(@"content\jQuery.js", files[0].Path);
            Assert.Equal(@"lib\net45\jQuery.dll", files[1].Path);
            Assert.Equal(@"lib\sl3\nunit.exe", files[2].Path);
            Assert.Equal(@"readme.txt", files[3].Path);
            Assert.Equal(@"tools\init.ps1", files[4].Path);
            Assert.Equal(@"tools\install.ps1", files[5].Path);
        }

        [Fact]
        public void GetFilesDoesNotIncludePackageFile()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            AddPackage(fileSystem, "X", "2.0.0-alpha");

            fileSystem.AddFile(@"X.2.0.0-alpha\readme.txt");
            fileSystem.AddFile(@"X.2.0.0-alpha\x.nupkg");

            var package = new UnzippedPackage(fileSystem, "X.2.0.0-alpha");

            // Act
            IList<IPackageFile> files = package.GetFiles().OrderBy(p => p.Path).ToList();

            // Assert
            Assert.Equal(1, files.Count);
            Assert.Equal(@"readme.txt", files[0].Path);
        }

        [Fact]
        public void GetFilesDoesNotIncludePackageManifestFile()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            AddPackage(fileSystem, "X", "2.0.0-alpha");

            fileSystem.AddFile(@"X.2.0.0-alpha\readme.txt");
            fileSystem.AddFile(@"X.2.0.0-alpha\X.nuspec");

            var package = new UnzippedPackage(fileSystem, "X.2.0.0-alpha");

            // Act
            IList<IPackageFile> files = package.GetFiles().OrderBy(p => p.Path).ToList();

            // Assert
            Assert.Equal(1, files.Count);
            Assert.Equal(@"readme.txt", files[0].Path);
        }

        [Fact]
        public void GetFilesCanIncludeNonPackageManifestFile()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            AddPackage(fileSystem, "X", "2.0.0-alpha");

            fileSystem.AddFile(@"X.2.0.0-alpha\readme.txt");
            fileSystem.AddFile(@"X.2.0.0-alpha\y.nuspec");

            var package = new UnzippedPackage(fileSystem, "X.2.0.0-alpha");

            // Act
            IList<IPackageFile> files = package.GetFiles().OrderBy(p => p.Path).ToList();

            // Assert
            Assert.Equal(2, files.Count);
            Assert.Equal(@"readme.txt", files[0].Path);
        }

        [Fact]
        public void GetAssemblyReferencesReturnsCorrectFiles()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            AddPackage(fileSystem, "X", "2.0.0-alpha");

            fileSystem.AddFile(@"X.2.0.0-alpha\readme.txt");
            fileSystem.AddFile(@"X.2.0.0-alpha\content\jQuery.js");
            fileSystem.AddFile(@"X.2.0.0-alpha\lib\net45\jQuery.dll");
            fileSystem.AddFile(@"X.2.0.0-alpha\lib\net45\jQuery.resources.dll");
            fileSystem.AddFile(@"X.2.0.0-alpha\lib\net45\jQuery.dll.xml");
            fileSystem.AddFile(@"X.2.0.0-alpha\lib\sl3\nunit.exe");
            fileSystem.AddFile(@"X.2.0.0-alpha\lib\sl3\nunit.winmd");
            fileSystem.AddFile(@"X.2.0.0-alpha\tools\install.ps1");
            fileSystem.AddFile(@"X.2.0.0-alpha\tools\init.ps1");

            var package = new UnzippedPackage(fileSystem, "X.2.0.0-alpha");

            // Act
            IList<IPackageAssemblyReference> files = package.AssemblyReferences.OrderBy(p => p.Path).ToList();

            // Assert
            Assert.Equal(3, files.Count);
            Assert.Equal(@"lib\net45\jQuery.dll", files[0].Path);
            Assert.Equal(@"lib\sl3\nunit.exe", files[1].Path);
            Assert.Equal(@"lib\sl3\nunit.winmd", files[2].Path);
        }

        [Fact]
        public void GetSupportedFrameworksUsesFilesToDetermineFramework()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"X.2.0.0-alpha\readme.txt");
            fileSystem.AddFile(@"X.2.0.0-alpha\content\jQuery.js");
            fileSystem.AddFile(@"X.2.0.0-alpha\lib\net45\jQuery.dll");
            fileSystem.AddFile(@"X.2.0.0-alpha\lib\net45\jQuery.resources.dll");
            fileSystem.AddFile(@"X.2.0.0-alpha\lib\net45\jQuery.dll.xml");
            fileSystem.AddFile(@"X.2.0.0-alpha\lib\sl3\nunit.exe");
            fileSystem.AddFile(@"X.2.0.0-alpha\lib\sl3\nunit.winmd");
            fileSystem.AddFile(@"X.2.0.0-alpha\tools\install.ps1");
            fileSystem.AddFile(@"X.2.0.0-alpha\tools\init.ps1");

            AddPackage(fileSystem, "X", "2.0.0-alpha");
            var package = new UnzippedPackage(fileSystem, "X.2.0.0-alpha");

            // Act
            var supportedFramework = package.GetSupportedFrameworks();

            // Assert
            Assert.Equal(new[] { new FrameworkName(".NETFramework,Version=v4.5"), new FrameworkName("Silverlight,Version=v3.0") }, supportedFramework);
        }

        [Fact]
        public void GetSupportedFrameworksUsesFrameworkReferenceToDetermineFramework()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"X.2.0.0-alpha\X.2.0.0-alpha.nuspec", GetCompleteManifestContent());
            var package = new UnzippedPackage(fileSystem, "X.2.0.0-alpha");

            // Act
            var supportedFramework = package.GetSupportedFrameworks();

            // Assert
            Assert.Equal(new[] { new FrameworkName(".NETFramework,Version=v4.5") }, supportedFramework);
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
    <references>
      <reference file=""jQuery.dll"" />
      <reference file=""nunit.winmd"" />
    </references>
  </metadata>
</package>";
            return String.Format(template, id, version);
        }

        private string GetCompleteManifestContent()
        {
            return @"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd"">
  <metadata>
    <version>1.0.0</version>
    <authors>dotnetjunky</authors>
    <owners>Outercurve</owners>
    <licenseUrl>http://www.nuget.com/license</licenseUrl>
    <projectUrl>http://www.nuget.com</projectUrl>
    <iconUrl>http://www.outercurve.com</iconUrl>
    <dependencies>
      <dependency id=""bing"" version=""1.0-RC"" />
    </dependencies>
    <references>
      <reference file=""one"" />
      <reference file=""two"" />
    </references>
    <frameworkAssemblies>
      <frameworkAssembly assemblyName=""System"" targetFramework="".NETFramework4.5"" />
    </frameworkAssemblies>
    <id>Jumpo</id>
    <title>Jumpo Jet</title>
    <requireLicenseAcceptance>true</requireLicenseAcceptance>
    <description>My package description.</description>
    <summary>This is jumpo package.</summary>
    <releaseNotes>New jumpo.</releaseNotes>
    <copyright>Outercurve Foundation</copyright>
    <language>ar-EG</language>
    <tags>haha void</tags>
  </metadata>
  <files />
</package>";
        }
    }
}
