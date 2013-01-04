using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Moq;
using Xunit;
using Xunit.Extensions;
using NuGet.Test.Utility;

namespace NuGet.Test
{
    public class ZipPackageTest
    {
        [Fact]
        public void CtorWithStreamThrowsIfNull()
        {
            ExceptionAssert.ThrowsArgNull(() => new ZipPackage((Stream)null), "stream");
        }

        [Fact]
        public void CtorWithFileNameThrowsIfNullOrEmpty()
        {
            ExceptionAssert.ThrowsArgNullOrEmpty(() => new ZipPackage((string)null), "filePath");
            ExceptionAssert.ThrowsArgNullOrEmpty(() => new ZipPackage(String.Empty), "filePath");
        }

        [Fact]
        public void CtorWithStream()
        {
            // Arrange
            var builder = new PackageBuilder();
            builder.Id = "Package";
            builder.Version = new SemanticVersion("1.0");
            builder.Authors.Add("David");
            builder.Description = "This is a test package";
            builder.ReleaseNotes = "This is a release note.";
            builder.Copyright = "Copyright";
            builder.Files.AddRange(PackageUtility.CreateFiles(new[] { PathFixUtility.FixPath(@"lib\40\A.dll"), PathFixUtility.FixPath(@"content\foo") }));

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

            //Order is not gauranteed (or required) from GetFiles(), 
            //but we rely on the order for a few of the asserts, 
            //and it appears to not behave the same way on Mono,
            //so we call "order by" here to force a specific order.
            var files = package.GetFiles().OrderBy(k => k.Path).ToList();

            Assert.Equal(2, files.Count);
            Assert.Equal(PathFixUtility.FixPath(@"content\foo"), files[0].Path);
            Assert.Equal(PathFixUtility.FixPath(@"lib\40\A.dll"), files[1].Path);
            var assemblyReferences = package.AssemblyReferences.ToList();
            Assert.Equal(1, assemblyReferences.Count);
            Assert.Equal("A.dll", assemblyReferences[0].Name);
            Assert.Equal(new FrameworkName(".NETFramework", new Version("4.0")), assemblyReferences[0].TargetFramework);
            Assert.Equal("This is a release note.", package.ReleaseNotes);
        }

        [Theory]
        [InlineData(new[] { @"content\Foo.txt", @"lib\Bar.dll" }, new string[0])]
        [InlineData(new[] { @"content\net40-full\Foo.txt", @"lib\Bar.dll" }, new[] { ".NETFramework,Version=4.0" })]
        [InlineData(new[] { @"content\Foo.txt", @"lib\net35-client\Bar.dll" }, new[] { ".NETFramework,Version=v3.5,Profile=Client" })]
        [InlineData(new[] { @"content\net20\Foo.txt", @"lib\net45\Bar.dll", @"lib\sl4\Bar.dll" },
                    new[] { ".NETFramework,Version=v2.0", ".NETFramework,Version=v4.5", "Silverlight,Version=v4.0" })]
        public void GetSupportedFrameworkUsesFilesToDetermineTargetFramework(IEnumerable<string> files, IEnumerable<string> expectedFramework)
        {
            // Arrange
            files = files.Select(PathFixUtility.FixPath);
            var zipPackage = CreatePackage(files);

            // Act
            var targetFramework = zipPackage.GetSupportedFrameworks();

            // Assert
            Assert.Equal(expectedFramework.Select(s => new FrameworkName(s)), targetFramework);
        }

        public static IEnumerable<object[]> GetSupportedFrameworkUsesFrameworkAssembliesToDetermineTargetFrameworkData
        {
            get
            {
                yield return new object[] { new[] { new FrameworkAssemblyReference("System.Data") }, new FrameworkName[0] };
                yield return new object[] { new[] { new FrameworkAssemblyReference("System.Data", new[] { new FrameworkName(".NETFramework,Version=4.0") }) },
                                            new[] { new FrameworkName(".NETFramework,Version=4.0") } };
                yield return new object[] { new[] { new FrameworkAssemblyReference("System.Data.Client", new[] { new FrameworkName(".NETFramework,Version=2.0") }) , 
                                                    new FrameworkAssemblyReference("System.Data", new[] { new FrameworkName(".NETFramework,Version=3.5") }) },
                                            new[] { new FrameworkName(".NETFramework,Version=2.0"), new FrameworkName(".NETFramework,Version=3.5") } };

            }
        }

        [Theory]
        [PropertyData("GetSupportedFrameworkUsesFrameworkAssembliesToDetermineTargetFrameworkData")]
        public void GetSupportedFrameworkUsesFrameworkAssembliesToDetermineTargetFramework(IEnumerable<FrameworkAssemblyReference> assemblyReferences, IEnumerable<FrameworkName> expectedFramework)
        {
            // Arrange
            var zipPackage = CreatePackage(assemblyReferences: assemblyReferences);

            // Act
            var targetFramework = zipPackage.GetSupportedFrameworks();

            // Assert
            Assert.Equal(expectedFramework, targetFramework);
        }

        [Fact]
        public void GetSupportedFrameworkUsesFilesAndFrameworkAssembliesToDetermineTargetFramework()
        {
            // Arrange
            var files = new[] { PathFixUtility.FixPath(@"content\Foo.txt"), PathFixUtility.FixPath(@"lib\net40-full\Bar.dll"), PathFixUtility.FixPath(@"lib\net20\Qux.dll") };
            var assemblyReferences = new[] { new FrameworkAssemblyReference("System.Data.Client", new[] { new FrameworkName(".NETFramework,Version=2.0") }) };
            var zipPackage = CreatePackage(files: files, assemblyReferences: assemblyReferences);

            // Act
            var targetFramework = zipPackage.GetSupportedFrameworks();

            // Assert
            Assert.Equal(new[] { new FrameworkName(".NETFramework,Version=2.0"), new FrameworkName(".NETFramework,Version=4.0") }, targetFramework);
        }

        [Fact]
        public void GetSupportedFrameworkUsesCachedFileValues()
        {
            // This test is to ensure our alternate code path that uses cached file names has some coverage.
            // Arrange
            var files = new[] { PathFixUtility.FixPath(@"content\Foo.txt"), PathFixUtility.FixPath(@"lib\net40-full\Bar.dll"), PathFixUtility.FixPath(@"lib\net20\Qux.dll") };
            var assemblyReferences = new[] { new FrameworkAssemblyReference("System.Data.Client", new[] { new FrameworkName(".NETFramework,Version=2.0") }) };
            var zipPackage = CreatePackage(files: files, assemblyReferences: assemblyReferences, enableCaching: true);

            // Act
            var zipFiles = zipPackage.GetFiles();
            var targetFramework = zipPackage.GetSupportedFrameworks();

            // Assert
            Assert.NotNull(zipFiles);
            Assert.Equal(new[] { new FrameworkName(".NETFramework,Version=2.0"), new FrameworkName(".NETFramework,Version=4.0") }, targetFramework);
        }

        private ZipPackage CreatePackage(IEnumerable<string> files = null, IEnumerable<FrameworkAssemblyReference> assemblyReferences = null, bool enableCaching = false)
        {
            var packageBuilder = new PackageBuilder
            {
                Id = "Test-Package",
                Version = new SemanticVersion("1.0"),
                Description = "Test descr",
            };
            packageBuilder.Authors.Add("test");

            if (files != null)
            {
                packageBuilder.Files.AddRange(files.Select(CreateFile));
            }
            if (assemblyReferences != null)
            {
                packageBuilder.FrameworkReferences.AddRange(assemblyReferences);
            }

            var memoryStream = new MemoryStream();
            packageBuilder.Save(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            return new ZipPackage(memoryStream.ToStreamFactory(), enableCaching: enableCaching);
        }

        private static IPackageFile CreateFile(string path)
        {
            var file = new Mock<IPackageFile>();
            file.Setup(s => s.Path).Returns(path);
            file.Setup(s => s.GetStream()).Returns(Stream.Null);

            return file.Object;
        }
    }
}
