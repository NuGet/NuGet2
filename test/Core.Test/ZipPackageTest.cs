using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Xunit;

namespace NuGet.Test {
    
    public class ZipPackageTest {
        [Fact]
        public void CtorWithStreamThrowsIfNull() {
            ExceptionAssert.ThrowsArgNull(() => new ZipPackage((Stream)null), "stream");
        }

        [Fact]
        public void CtorWithFileNameThrowsIfNullOrEmpty() {
            ExceptionAssert.ThrowsArgNullOrEmpty(() => new ZipPackage((string)null), "fileName");
            ExceptionAssert.ThrowsArgNullOrEmpty(() => new ZipPackage(String.Empty), "fileName");
        }

        [Fact]
        public void CtorWithStream() {
            // Arrange
            var builder = new PackageBuilder();
            builder.Id = "Package";
            builder.Version = new SemanticVersion("1.0");
            builder.Authors.Add("David");
            builder.Description = "This is a test package";
            builder.ReleaseNotes = "This is a release note.";
            builder.Copyright = "Copyright";
            builder.Files.AddRange(PackageUtility.CreateFiles(new[] { @"lib\40\A.dll", @"content\foo" }));

            var ms = new MemoryStream();
            builder.Save(ms);
            ms.Seek(0, SeekOrigin.Begin);

            // Act
            var package = new ZipPackage(ms);

            // Assert
            Assert.Equal("Package", package.Id);
            Assert.Equal(new SemanticVersion("1.0"), package.Version);
            Assert.Equal("David", package.Authors.First());
            Assert.Equal("Copyright", package.Copyright);
            var files = package.GetFiles().ToList();
            Assert.Equal(2, files.Count);
            Assert.Equal(@"content\foo", files[0].Path);
            Assert.Equal(@"lib\40\A.dll", files[1].Path);
            var assemblyReferences = package.AssemblyReferences.ToList();
            Assert.Equal(1, assemblyReferences.Count);
            Assert.Equal("A.dll", assemblyReferences[0].Name);
            Assert.Equal(new FrameworkName(".NETFramework", new Version("4.0")), assemblyReferences[0].TargetFramework);
            Assert.Equal("This is a release note.", package.ReleaseNotes);
        }

        [Fact]
        public void IsAssemblyReferenceReturnsFalseIfFileDoesNotStartWithLib() {
            // Arrange
            var file = new PhysicalPackageFile { TargetPath = @"content\foo.dll" };
            IEnumerable<string> references = null;

            // Act and Assert
            Assert.False(ZipPackage.IsAssemblyReference(file, references));
        }

        [Fact]
        public void IsAssemblyReferenceReturnsFalseIfFileExtensionIsNotAReferenceItem() {
            // Arrange
            var file = new PhysicalPackageFile { TargetPath = @"lib\foo.txt" };
            IEnumerable<string> references = null;

            // Act and Assert
            Assert.False(ZipPackage.IsAssemblyReference(file, references));
        }

        [Fact]
        public void IsAssemblyReferenceReturnsFalseIfFileIsAResourceAssembly() {
            // Arrange
            var file = new PhysicalPackageFile { TargetPath = @"lib\NuGet.resources.dll" };
            IEnumerable<string> references = null;

            // Act and Assert
            Assert.False(ZipPackage.IsAssemblyReference(file, references));
        }

        [Fact]
        public void IsAssemblyReferenceReturnsTrueIfFileIsAReferenceItemInLib() {
            // Arrange
            var file = new PhysicalPackageFile { TargetPath = @"lib\NuGet.Core.dll" };
            IEnumerable<string> references = null;

            // Act and Assert
            Assert.True(ZipPackage.IsAssemblyReference(file, references));
        }

        [Fact]
        public void IsAssemblyReferenceReturnsFalseIfFileIsNotListedInReferences() {
            // Arrange
            var file = new PhysicalPackageFile { TargetPath = @"lib\NuGet.Core.dll" };
            IEnumerable<string> references = new[] {
                "NuGet.VisualStudio.dll",
                "NuGet.CommandLine.dll"
            };

            // Act and Assert
            Assert.False(ZipPackage.IsAssemblyReference(file, references));
        }

        [Fact]
        public void IsAssemblyReferenceReturnsTrueIfFileIsListedInReferences() {
            // Arrange
            var file = new PhysicalPackageFile { TargetPath = @"lib\NuGet.Core.dll" };
            IEnumerable<string> references = new[] {
                "NuGet.VisualStudio.dll",
                "NuGet.CommandLine.dll",
                "NuGet.Core.dll",
            };

            // Act and Assert
            Assert.True(ZipPackage.IsAssemblyReference(file, references));
        }
    }
}
