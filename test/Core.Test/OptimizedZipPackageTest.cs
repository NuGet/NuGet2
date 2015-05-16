using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Moq;
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
        public void DoNotReuseExpandedFolderIfLastModifedTimeChanged()
        {
            // Arrange
            var ms = GetPackageStream();

            var fileSystem = new MockFileSystem("x:\\");
            fileSystem.AddFile("pam.nupkg", ms);

            var expandedFileSystem = new MockFileSystem("y:\\");

            // Act
            var ozp1 = new TestableOptimizedZipPackage(fileSystem, "pam.nupkg", expandedFileSystem, forceUseCache: true);
            ozp1.GetFiles().ToList();

            // add some delay to make sure the time from DateTime.Now will be different
            System.Threading.Thread.Sleep(100);

            // now add the file again to simulate file change
            ms.Seek(0, SeekOrigin.Begin);
            fileSystem.AddFile("pam.nupkg", ms);

            var ozp2 = new TestableOptimizedZipPackage(fileSystem, "pam.nupkg", expandedFileSystem, forceUseCache: true);
            ozp2.GetFiles().ToList();

            // Assert
            Assert.True(ozp1.HasCalledExpandedFolderPath);
            Assert.True(ozp2.HasCalledExpandedFolderPath);
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
            ozp.GetFiles().ToList();

            Assert.True(expandedFileSystem.FileExists("random\\content\\foo"));
            Assert.True(expandedFileSystem.FileExists("random\\lib\\40\\A.dll"));

            ozp.Reset();

            // Act
            ozp.GetFiles().ToList();

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
        public void AssemblyReferencesIsFilteredCorrectlyWhenReferenceIsEmpty()
        {
            // Arrange
            var files = new IPackageFile[] {
                CreatePackageFile(@"lib\net40\one.dll"),
            };

            var references = new PackageReferenceSet[] {
            };

            var ms = GetPackageStream(files, references);

            var fileSystem = new MockFileSystem("x:\\");
            fileSystem.AddFile("pam.nupkg", ms);

            var expandedFileSystem = new MockFileSystem("y:\\");

            var ozp = new TestableOptimizedZipPackage(fileSystem, "pam.nupkg", expandedFileSystem);

            // Act
            var assemblies = ozp.AssemblyReferences.ToList();

            // Assert
            Assert.Equal(1, assemblies.Count);
            Assert.Equal(@"lib\net40\one.dll", assemblies[0].Path);
        }

        [Fact]
        public void AssemblyReferencesIsNotFilteredAccordingToTargetFramework()
        {
            // Arrange
            var files = new IPackageFile[] {
                CreatePackageFile(@"lib\net40\one.dll"),
                CreatePackageFile(@"lib\net40\two.dll"),

                CreatePackageFile(@"lib\sl30\one.dll"),
                CreatePackageFile(@"lib\sl30\two.dll"),

                CreatePackageFile(@"lib\net45\foo.dll"),
                CreatePackageFile(@"lib\net45\bar.dll")
            };

            var references = new PackageReferenceSet[] {
                new PackageReferenceSet(
                    new FrameworkName("Silverlight, Version=2.0"),
                    new [] { "two.dll" }),

                new PackageReferenceSet(
                    new FrameworkName(".NET, Version=4.5"),
                    new string[] { "foo.dll", "bar.dll" }),
            };

            var ms = GetPackageStream(files, references);

            var fileSystem = new MockFileSystem("x:\\");
            fileSystem.AddFile("pam.nupkg", ms);

            var expandedFileSystem = new MockFileSystem("y:\\");

            var ozp = new TestableOptimizedZipPackage(fileSystem, "pam.nupkg", expandedFileSystem);

            // Act
            var assemblies = ozp.AssemblyReferences.OrderBy(p => p.Path).ToList();

            // Assert
            Assert.Equal(6, assemblies.Count);
            Assert.Equal(@"lib\net40\one.dll", assemblies[0].Path);
            Assert.Equal(@"lib\net40\two.dll", assemblies[1].Path);
            Assert.Equal(@"lib\net45\bar.dll", assemblies[2].Path);
            Assert.Equal(@"lib\net45\foo.dll", assemblies[3].Path);
            Assert.Equal(@"lib\sl30\one.dll", assemblies[4].Path);
            Assert.Equal(@"lib\sl30\two.dll", assemblies[5].Path);
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
        public void DoNotSkipExistingFilesWhileExpandingFiles()
        {
            // Arrange
            var ms = GetPackageStream();

            var fileSystem = new MockFileSystem("x:\\");
            fileSystem.AddFile("pam.nupkg", ms);

            var expandedFileSystem = new MockFileSystem("y:\\");
            expandedFileSystem.AddFile("random\\content\\foo", "happy new year");

            var ozp = new TestableOptimizedZipPackage(fileSystem, "pam.nupkg", expandedFileSystem);

            // Act
            ozp.GetFiles().ToList();

            // Assert
            Assert.True(expandedFileSystem.FileExists("random\\content\\foo"));
            Assert.True(expandedFileSystem.FileExists("random\\lib\\40\\A.dll"));
            Assert.Equal("content\\foo", expandedFileSystem.ReadAllText("random\\content\\foo"));
        }

        [Fact]
        public void DoNotOverwriteExistingFilesWhileExpandingFilesIfContentsAreEqual()
        {
            // Arrange
            var ms = GetPackageStream();

            var fileSystem = new MockFileSystem("x:\\");
            fileSystem.AddFile("pam.nupkg", ms);

            var expandedFileSystem = new Mock<MockFileSystem>("y:\\")
            {
                CallBase = true
            };
            expandedFileSystem.Object.AddFile("random\\content\\foo", "content\\foo");

            expandedFileSystem.Setup(f => f.CreateFile("random\\content\\foo"))
                              .Throws(new InvalidOperationException());

            var ozp = new TestableOptimizedZipPackage(
                fileSystem, "pam.nupkg", expandedFileSystem.Object);

            // Act
            ozp.GetFiles().ToList();

            // Assert
            Assert.True(expandedFileSystem.Object.FileExists("random\\content\\foo"));
            Assert.True(expandedFileSystem.Object.FileExists("random\\lib\\40\\A.dll"));
            Assert.Equal("content\\foo", expandedFileSystem.Object.ReadAllText("random\\content\\foo"));
        }

        private static MemoryStream GetPackageStream(
            IEnumerable<IPackageFile> files = null,
            IEnumerable<PackageReferenceSet> references = null)
        {
            var builder = new PackageBuilder();
            builder.Id = "Package";
            builder.Version = new SemanticVersion("1.0");
            builder.Authors.Add("dotnetjunky");
            builder.Description = "This is a test package";
            builder.ReleaseNotes = "This is a release note.";
            builder.Copyright = "Copyright";
            if (files != null)
            {
                builder.Files.AddRange(files);
            }
            else
            {
                builder.Files.AddRange(
                    PackageUtility.CreateFiles(
                        new[] { PathFixUtility.FixPath(@"lib\40\A.dll"), PathFixUtility.FixPath(@"content\foo") }
                    ));
            }

            builder.FrameworkReferences.AddRange(
                new[] { new FrameworkAssemblyReference("A", new[] { VersionUtility.ParseFrameworkName("sl50", useManagedCodeConventions: false) }),
                        new FrameworkAssemblyReference("B", new[] { VersionUtility.ParseFrameworkName("windows8", useManagedCodeConventions: false) })
                      });
            if (references != null)
            {
                builder.PackageAssemblyReferences.AddRange(references);
            }
            
            var ms = new MemoryStream();
            builder.Save(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        private class TestableOptimizedZipPackage : OptimizedZipPackage
        {
            public TestableOptimizedZipPackage(IFileSystem fileSystem, string packagePath, IFileSystem expandedFileSystem, bool forceUseCache = false)
                : base(fileSystem, packagePath, expandedFileSystem, forceUseCache)
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

        private static IPackageFile CreatePackageFile(string name)
        {
            var file = new Mock<IPackageFile>();
            file.SetupGet(f => f.Path).Returns(name);
            file.Setup(f => f.GetStream()).Returns(new MemoryStream());

            string effectivePath;
            var fx = VersionUtility.ParseFrameworkNameFromFilePath(name, useManagedCodeConventions: false, effectivePath: out effectivePath);
            file.SetupGet(f => f.EffectivePath).Returns(effectivePath);
            file.SetupGet(f => f.TargetFramework).Returns(fx);

            return file.Object;
        }
    }
}