using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using NuGet.Test.Mocks;
using NuGet.Test.Utility;
using Xunit;

namespace NuGet.Test
{
    public class OptimizedZipPackageTest
    {
        [Fact]
        public void TestingCtorWithFileSystemAndPackagePath()
        {
            // Arrange
            var ms = GetPackageStream();

            var fileSystem = new MockFileSystem("x:\\");
            fileSystem.AddFile("pam.nupkg", ms);

            // Act
            var ozp = new OptimizedZipPackage(fileSystem, "pam.nupkg", new MockFileSystem("y:\\"));

            // Assert
            Assert.Equal("Package", ozp.Id);
            Assert.Equal(new SemanticVersion("1.0"), ozp.Version);
            Assert.Equal("This is a test package", ozp.Description);
            Assert.Equal("This is a release note.", ozp.ReleaseNotes);
            Assert.Equal("Copyright", ozp.Copyright);
            Assert.Equal("dotnetjunky", ozp.Authors.First());

            // Order is not gauranteed (or required) from GetFiles(), 
            // but we rely on the order for a few of the asserts, 
            // and it appears to not behave the same way on Mono,
            // so we call "order by" here to force a specific order.
            var files = ozp.GetFiles().OrderBy(k => k.Path).ToList();

            Assert.Equal(2, files.Count);
            Assert.Equal(PathFixUtility.FixPath(@"content\foo"), files[0].Path);
            Assert.Equal(PathFixUtility.FixPath(@"lib\40\A.dll"), files[1].Path);

            var assemblyReferences = ozp.AssemblyReferences.ToList();
            Assert.Equal(1, assemblyReferences.Count);
            Assert.Equal("A.dll", assemblyReferences[0].Name);
            Assert.Equal(new FrameworkName(".NETFramework", new Version("4.0")), assemblyReferences[0].TargetFramework);

            var supportedReferences = ozp.GetSupportedFrameworks().OrderBy(p => p.FullName).ToList();
            Assert.Equal(3, supportedReferences.Count);
            Assert.Equal(new FrameworkName(".NETFramework", new Version("4.0")), supportedReferences[0]);
            Assert.Equal(new FrameworkName("Silverlight", new Version("5.0")), supportedReferences[1]);
            Assert.Equal(new FrameworkName("Windows", new Version("8.0")), supportedReferences[2]);
        }

        [Fact]
        public void CallingCtorDoesNotExpandFiles()
        {
            // Arrange
            var ms = GetPackageStream();

            var fileSystem = new MockFileSystem("x:\\");
            fileSystem.AddFile("pam.nupkg", ms);

            var expandedFileSystem = new MockFileSystem();

            // Act
            var ozp = new TestableOptimizedZipPackage(fileSystem, "pam.nupkg", expandedFileSystem);

            // Assert
            Assert.False(ozp.HasCalledExpandedFolderPath);
            Assert.Equal(0, expandedFileSystem.Paths.Count);
        }

        [Fact]
        public void CallingGetFilesExpandFilesIntoSpecifiedFileSystem()
        {
            // Arrange
            var ms = GetPackageStream();

            var fileSystem = new MockFileSystem("x:\\");
            fileSystem.AddFile("pam.nupkg", ms);

            var expandedFileSystem = new MockFileSystem("y:\\");

            var ozp = new TestableOptimizedZipPackage(fileSystem, "pam.nupkg", expandedFileSystem);

            // Act
            var files = ozp.GetFiles().ToList();

            // Assert
            Assert.Equal(2, files.Count);
            Assert.True(expandedFileSystem.FileExists("random\\content\\foo"));
            Assert.True(expandedFileSystem.FileExists("random\\lib\\40\\A.dll"));
        }

        [Fact]
        public void CallingGetFilesTwiceDoesNotExpandFilesIntoSpecifiedFileSystemAgain()
        {
            // Arrange
            var ms = GetPackageStream();

            var fileSystem = new MockFileSystem("x:\\");
            fileSystem.AddFile("pam.nupkg", ms);

            var expandedFileSystem = new MockFileSystem("y:\\");

            var ozp = new TestableOptimizedZipPackage(fileSystem, "pam.nupkg", expandedFileSystem);
            var files = ozp.GetFiles().ToList();

            Assert.True(expandedFileSystem.FileExists("random\\content\\foo"));
            Assert.True(expandedFileSystem.FileExists("random\\lib\\40\\A.dll"));

            ozp.Reset();

            // Act
            files = ozp.GetFiles().ToList();

            // Assert
            Assert.False(ozp.HasCalledExpandedFolderPath);
        }

        [Fact]
        public void CallingAssemblyReferencesExpandFilesIntoSpecifiedFileSystem()
        {
            // Arrange
            var ms = GetPackageStream();

            var fileSystem = new MockFileSystem("x:\\");
            fileSystem.AddFile("pam.nupkg", ms);

            var expandedFileSystem = new MockFileSystem("y:\\");

            var ozp = new TestableOptimizedZipPackage(fileSystem, "pam.nupkg", expandedFileSystem);

            // Act
            var files = ozp.AssemblyReferences.ToList();

            // Assert
            Assert.Equal(1, files.Count);
            Assert.True(expandedFileSystem.FileExists("random\\content\\foo"));
            Assert.True(expandedFileSystem.FileExists("random\\lib\\40\\A.dll"));
        }

        [Fact]
        public void CallingGetSupportedFrameworksExpandFilesIntoSpecifiedFileSystem()
        {
            // Arrange
            var ms = GetPackageStream();

            var fileSystem = new MockFileSystem("x:\\");
            fileSystem.AddFile("pam.nupkg", ms);

            var expandedFileSystem = new MockFileSystem("y:\\");

            var ozp = new TestableOptimizedZipPackage(fileSystem, "pam.nupkg", expandedFileSystem);

            // Act
            var files = ozp.GetSupportedFrameworks().ToList();

            // Assert
            Assert.Equal(3, files.Count);
            Assert.True(expandedFileSystem.FileExists("random\\content\\foo"));
            Assert.True(expandedFileSystem.FileExists("random\\lib\\40\\A.dll"));
        }

        [Fact]
        public void IsValidIsTrueWhenNupkgFileIsDeleted()
        {
            // Arrange
            var ms = GetPackageStream();

            var fileSystem = new MockFileSystem("x:\\");
            fileSystem.AddFile("pam.nupkg", ms);

            var expandedFileSystem = new MockFileSystem("y:\\");

            var ozp = new TestableOptimizedZipPackage(fileSystem, "pam.nupkg", expandedFileSystem);
            Assert.True(ozp.IsValid);

            // Act
            fileSystem.DeleteFile("pam.nupkg");

            // Assert
            Assert.False(ozp.IsValid);
        }

        [Fact]
        public void SkipExistingFilesWhileExpandingFiles()
        {
            // Arrange
            var ms = GetPackageStream();

            var fileSystem = new MockFileSystem("x:\\");
            fileSystem.AddFile("pam.nupkg", ms);

            var expandedFileSystem = new MockFileSystem("y:\\");
            expandedFileSystem.AddFile("random\\content\\foo", "happy new year");

            var ozp = new TestableOptimizedZipPackage(fileSystem, "pam.nupkg", expandedFileSystem);

            // Act
            var files = ozp.GetFiles().ToList();

            // Assert
            Assert.True(expandedFileSystem.FileExists("random\\content\\foo"));
            Assert.True(expandedFileSystem.FileExists("random\\lib\\40\\A.dll"));
            Assert.Equal("happy new year", expandedFileSystem.ReadAllText("random\\content\\foo"));
        }

        private static MemoryStream GetPackageStream()
        {
            var builder = new PackageBuilder();
            builder.Id = "Package";
            builder.Version = new SemanticVersion("1.0");
            builder.Authors.Add("dotnetjunky");
            builder.Description = "This is a test package";
            builder.ReleaseNotes = "This is a release note.";
            builder.Copyright = "Copyright";
            builder.Files.AddRange(
                PackageUtility.CreateFiles(
                    new[] { PathFixUtility.FixPath(@"lib\40\A.dll"), PathFixUtility.FixPath(@"content\foo") }
                ));

            builder.FrameworkReferences.AddRange(
                new[] { new FrameworkAssemblyReference("A", new[] { VersionUtility.ParseFrameworkName("sl50") }),
                        new FrameworkAssemblyReference("B", new[] { VersionUtility.ParseFrameworkName("windows8") })
                      });
            
            var ms = new MemoryStream();
            builder.Save(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        private class TestableOptimizedZipPackage : OptimizedZipPackage
        {
            public TestableOptimizedZipPackage(IFileSystem fileSystem, string packagePath, IFileSystem expandedFileSystem)
                : base(fileSystem, packagePath, expandedFileSystem)
            {
            }

            public bool HasCalledExpandedFolderPath { get; private set; }

            public void Reset()
            {
                HasCalledExpandedFolderPath = false;
            }

            protected override string GetExpandedFolderPath()
            {
                HasCalledExpandedFolderPath = true;
                return "random";
            }
        }
    }
}