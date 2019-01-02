using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Xml.Linq;
using Moq;
using NuGet.Runtime;
using NuGet.Test.Mocks;
using Xunit;
using Xunit.Extensions;

namespace NuGet.VisualStudio.Test
{
    public class FrameworkAssemblyResolverTest
    {
        private const string MockFileSystemRoot = @"C:\MockFileSystem\";

        [Theory]
        [InlineData("Silverlight,Version=v3.0")]
        [InlineData(".NETCore,Version=v4.5")]
        [InlineData("Silverlight,Version=v4.0,Profile=WindowsPhone71")]
        [InlineData(".NETPortable,Version=v4.0,Profile=Profile1")]
        public void IsHigherAssemblyVersionInFrameworkReturnsFalseForNonNETFramework(string targetFramework)
        {
            // mscorlib of version greater than 1.0.0.0 is definitely available in all the frameworks passed in as InlineData
            // And, yet the result should be false. That is the test
            Assert.False(IsHigherAssemblyVersionInFrameworkHelper("mscorlib", new Version("1.0.0.0"), new FrameworkName(targetFramework)));
        }

        [Fact]
        public void IsHigherAssemblyVersionInFrameworkReturnsFalseForNonFrameworkAssemblies()
        {
            // Arrange
            // Create a file which does not contain System.Web.Razor
            var version = new Version("3.0.0.0");
            var targetFrameworkName = new FrameworkName(".NETFramework,Version=v4.5");
            var mockFileSystem = GetMockFileSystem("System.Net", version, targetFrameworkName);

            // Act & Assert
            Assert.False(IsHigherAssemblyVersionInFrameworkHelper(mockFileSystem, "System.Web.Razor", version, targetFrameworkName));
        }

        [Fact]
        public void IsHigherAssemblyVersionInFrameworkWorksForNETClientProfile()
        {
            Assert.True(IsHigherAssemblyVersionInFrameworkHelper("System.Net", new Version("3.5.0.0"), new FrameworkName(".NETFramework,Version=v4.0,Profile=Client")));
        }

        [Fact]
        public void IsHigherAssemblyVersionInFrameworkReturnsFalseWhenFrameworkAssemblyVersionIsEqual()
        {
            Assert.False(IsHigherAssemblyVersionInFrameworkHelper("System.Net", new Version("3.5.0.0"), new FrameworkName(".NETFramework,Version=v3.5,Profile=Client")));
        }

        [Fact]
        public void IsHigherAssemblyVersionInFrameworkReturnsFalseWhenFrameworkAssemblyVersionIsSmaller()
        {
            Assert.False(IsHigherAssemblyVersionInFrameworkHelper("System.Net", new Version("4.0.0.0"), new FrameworkName(".NETFramework,Version=v3.5,Profile=Client")));
        }

        [Fact]
        public void IsHigherAssemblyVersionInFrameworkReturnsFalseForInvalidProfile()
        {
            // Arrange
            // Create a file which contains a valid profile
            var assemblyName = "System.Net";
            var version = new Version("3.5.0.0");
            var mockFileSystem = GetMockFileSystem(assemblyName, version, new FrameworkName(".NETFramework,Version=v4.0,Profile=Client"));

            // Act & Assert
            Assert.False(IsHigherAssemblyVersionInFrameworkHelper(mockFileSystem, assemblyName, version, new FrameworkName(".NETFramework,Version=v4.0,Profile=Invalid")));
        }

        [Fact]
        public void GetFrameworkAssembliesReturnsEmptyListWhenFileDoesNotExist()
        {
            // Arrange
            // Pass in a fileSystem with no file in it. An empty list should be returned
            var mockFileSystem = new MockFileSystem(MockFileSystemRoot);

            // Act & Assert
            Assert.Empty(FrameworkAssemblyResolver.GetFrameworkAssemblies(mockFileSystem));
        }

        [Fact]
        public void GetFrameworkAssembliesReturnsEmptyListWhenFileIsInvalid()
        {
            // Arrange
            // In the content below, after a valid entry for 'System.Net' there is an invalid entry for 'mscorlib'
            // And, GetFrameworkAssemblies should return an empty list when this happens
            var content = @"<?xml version='1.0' encoding='utf-8'?>
<FileList  Name='.NETFramework, v4.0.0.0'>
  <File AssemblyName='System.Net' Version='4.0.0.0' PublicKeyToken='aaaaaaaaaaaaaaaa' Culture='neutral' ProcessorArchitecture='MSIL' InGac='true' />
  <File AssemblyName=mscorlib Version='4.0.0.0' PublicKeyToken='aaaaaaaaaaaaaaaa' Culture='neutral' ProcessorArchitecture='MSIL' InGac='true' />
</FileList>";

            var mockFileSystem = new MockFileSystem(MockFileSystemRoot);
            mockFileSystem.AddFile(FrameworkAssemblyResolver.FrameworkListFileName, content);

            // Act & Assert
            Assert.Empty(FrameworkAssemblyResolver.GetFrameworkAssemblies(mockFileSystem));
        }

        [Fact]
        public void GetFrameworkAssembliesReturnsEmptyListWhenAFileEntryDoesNotHaveMandatoryAttributes()
        {
            // Arrange
            // In the content below, the attribute version is missing, and GetFrameworkAssemblies should return an empty list
            var content = @"<?xml version='1.0' encoding='utf-8'?>
<FileList  Name='.NETFramework, v4.0.0.0'>
  <File AssemblyName='System.Net' PublicKeyToken='aaaaaaaaaaaaaaaa' Culture='neutral' ProcessorArchitecture='MSIL' InGac='true' />
</FileList>";

            var mockFileSystem = new MockFileSystem(MockFileSystemRoot);
            mockFileSystem.AddFile(FrameworkAssemblyResolver.FrameworkListFileName, content);

            // Act & Assert
            Assert.Empty(FrameworkAssemblyResolver.GetFrameworkAssemblies(mockFileSystem));
        }

        private bool IsHigherAssemblyVersionInFrameworkHelper(string simpleAssemblyName, Version availableVersion, FrameworkName targetFrameworkName)
        {
            var mockFileSystem = GetMockFileSystem(simpleAssemblyName, targetFrameworkName.Version, targetFrameworkName);
            return IsHigherAssemblyVersionInFrameworkHelper(mockFileSystem, simpleAssemblyName, availableVersion, targetFrameworkName);
        }

        private bool IsHigherAssemblyVersionInFrameworkHelper(IFileSystem fileSystem, string simpleAssemblyName, Version availableVersion, FrameworkName targetFrameworkName)
        {
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();
            mockFileSystemProvider.Setup(f => f.GetFileSystem(MockFileSystemRoot)).Returns(fileSystem);

            return FrameworkAssemblyResolver.IsHigherAssemblyVersionInFramework(simpleAssemblyName,
                availableVersion, targetFrameworkName, mockFileSystemProvider.Object, GetPathToReferenceAssemblies, new ConcurrentDictionary<string,List<AssemblyName>>());
        }

        private IFileSystem GetMockFileSystem(string simpleAssemblyName, Version frameworkVersion, FrameworkName targetFrameworkName)
        {
            var content = @"<?xml version='1.0' encoding='utf-8'?>
<FileList  Name='" + targetFrameworkName.FullName + @"'>
  <File AssemblyName='" + simpleAssemblyName + @"' Version='" + frameworkVersion.ToString() +
@"' PublicKeyToken='aaaaaaaaaaaaaaaa' Culture='neutral' ProcessorArchitecture='MSIL' InGac='true' />
</FileList>";

            var fileSystem = new MockFileSystem(MockFileSystemRoot);
            fileSystem.AddFile(FrameworkAssemblyResolver.FrameworkListFileName, content);

            return fileSystem;
        }

        private IList<string> GetPathToReferenceAssemblies(FrameworkName targetFrameworkName)
        {
            return new List<string> { MockFileSystemRoot };
        }
    }
}
