using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Moq;
using Moq.Protected;
using Xunit;
using Xunit.Extensions;
using NuGet.Test.Utility;

namespace NuGet.Test
{
    public class LocalPackageTest
    {
        [Fact]
        public void IsAssemblyReferenceReturnsFalseIfFileDoesNotStartWithLib()
        {
            // Arrange
            var file = PathFixUtility.FixPath(@"content\foo.dll");

            // Act and Assert
            Assert.False(LocalPackage.IsAssemblyReference(file));
        }

        [Fact]
        public void IsAssemblyReferenceReturnsFalseIfFileExtensionIsNotAReferenceItem()
        {
            // Arrange
            var file = PathFixUtility.FixPath(@"lib\foo.txt");

            // Act and Assert
            Assert.False(LocalPackage.IsAssemblyReference(file));
        }

        [Fact]
        public void IsAssemblyReferenceReturnsFalseIfFileIsAResourceAssembly()
        {
            // Arrange
            var file = PathFixUtility.FixPath(@"lib\NuGet.resources.dll");

            // Act and Assert
            Assert.False(LocalPackage.IsAssemblyReference(file));
        }

        [Fact]
        public void IsAssemblyReferenceReturnsTrueIfFileIsAReferenceItemInLib()
        {
            // Arrange
            var file = PathFixUtility.FixPath(@"lib\NuGet.Core.dll");

            // Act and Assert
            Assert.True(LocalPackage.IsAssemblyReference(file));
        }

        [Fact]
        public void IsAssemblyReferenceReturnsTrueForWinMDFileInLib()
        {
            // Arrange
            var file = PathFixUtility.FixPath(@"lib\NuGet.Core.WINMD");

            // Act and Assert
            Assert.True(LocalPackage.IsAssemblyReference(file));
        }

        [Fact]
        public void AssemblyReferences_ThrowsIfQuirksModeIsEnabledAndLibPathsDoNotMapToTargetFramework()
        {
            // Arrange
            var testableLocalPackage = new TestableLocalPackage
            {
                PackageType = PackageType.Managed,
                SettableAssemblyReference = new[] 
                { 
                    CreateAssemblyReference(@"lib\Bar.dll"),
                    CreateAssemblyReference(@"lib\net40\Foo.dll", "net40"),
                    CreateAssemblyReference(@"lib\Baz.dll"),
                }
            };

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => testableLocalPackage.AssemblyReferences.ToList(),
                @"The following paths do not map to a well-known target framework: lib\Bar.dll, lib\Baz.dll.");
        }

        private static IPackageAssemblyReference CreateAssemblyReference(string path, string targetFramework = null)
        {
            var reference = new Mock<IPackageAssemblyReference>();
            reference.SetupGet(r => r.Path)
                .Returns(path);

            if (targetFramework != null)
            {
                reference.SetupGet(r => r.TargetFramework)
                    .Returns(VersionUtility.ParseFrameworkName(targetFramework, useManagedCodeConventions: true));
            }

            return reference.Object;
        }

        //[Fact]
        //public void IsAssemblyReferenceReturnsFalseIfFileIsNotListedInReferences()
        //{
        //    // Arrange
        //    var file = new PhysicalPackageFile { TargetPath = PathFixUtility.FixPath(@"lib\NuGet.Core.dll") };
        //    IEnumerable<string> references = new[] {
        //        "NuGet.VisualStudio.dll",
        //        "NuGet.CommandLine.dll"
        //    };

        //    // Act and Assert
        //    Assert.False(LocalPackage.IsAssemblyReference(file, references));
        //}

        //[Fact]
        //public void IsAssemblyReferenceReturnsTrueIfFileIsListedInReferences()
        //{
        //    // Arrange
        //    var file = new PhysicalPackageFile { TargetPath = PathFixUtility.FixPath(@"lib\NuGet.Core.dll") };
        //    IEnumerable<string> references = new[] {
        //        "NuGet.VisualStudio.dll",
        //        "NuGet.CommandLine.dll",
        //        "NuGet.Core.dll",
        //    };

        //    // Act and Assert
        //    Assert.True(LocalPackage.IsAssemblyReference(file, references));
        //}

        private class TestableLocalPackage : LocalPackage
        {
            public IEnumerable<IPackageAssemblyReference> SettableAssemblyReference { get; set; }

            public IEnumerable<IPackageFile> SettableFiles { get; set; }

            public override Stream GetStream()
            {
                throw new NotImplementedException();
            }

            protected override IEnumerable<IPackageFile> GetFilesBase()
            {
                return SettableFiles;
            }

            protected override IEnumerable<IPackageAssemblyReference> GetAssemblyReferencesCore()
            {
                return SettableAssemblyReference;
            }

            public override void ExtractContents(IFileSystem fileSystem, string extractPath)
            {
                throw new NotImplementedException();
            }
        }
    }
}
