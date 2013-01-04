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
    }
}
