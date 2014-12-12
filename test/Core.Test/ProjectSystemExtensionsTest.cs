using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using Moq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{
    public class ProjectSystemExtensionsTest
    {
        [Fact]
        public void GetCompatibleReferencesPrefersMatchingProfile()
        {
            // Arrange                                                                                                                       
            var assemblyReference30client = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("3.0"), "client"));
            var assemblyReference40client = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("4.0"), "client"));
            var assemblyReference30 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("3.0")));
            var assemblyReference40 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("4.0")));
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReference30client, assemblyReference40client, assemblyReference30, assemblyReference40 };

            // Act
            var targetAssemblyReferences = GetCompatibleItems(new FrameworkName(".NETFramework", new Version("4.0")), assemblyReferences).ToList();

            // Assert
            Assert.Equal(1, targetAssemblyReferences.Count);
            Assert.Same(assemblyReference40, targetAssemblyReferences[0]);
        }

        [Fact]
        public void GetCompatibleReferencesPrefersMatchingProfileIfSpecified()
        {
            // Arrange                                                                                                                       
            var assemblyReferenceSL40phone = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName("Silverlight", new Version("4.0"), "WindowsPhone"));
            var assemblyReferenceSL40 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName("Silverlight", new Version("4.0")));
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReferenceSL40phone, assemblyReferenceSL40 };

            // Act
            var targetAssemblyReferences = GetCompatibleItems(new FrameworkName("Silverlight", new Version("4.0"), "WindowsPhone"), assemblyReferences)
                                                         .ToList();

            // Assert
            Assert.Equal(1, targetAssemblyReferences.Count);
            Assert.Same(assemblyReferenceSL40phone, targetAssemblyReferences[0]);
        }

        [Fact]
        public void GetCompatibleReferencesPicksHigestVersionLessThanTargetVersion()
        {
            // Arrange                                                                                                                       
            var assemblyReference10 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("1.0")));
            var assemblyReference20 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("2.0")));
            var assemblyReference30 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("3.0")));
            var assemblyReference40 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("4.0")));
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReference10, assemblyReference20, assemblyReference30, assemblyReference40 };

            // Act
            var targetAssemblyReferences = GetCompatibleItems(new FrameworkName(".NETFramework", new Version("3.5")), assemblyReferences)
                                                         .ToList();

            // Assert
            Assert.Equal(1, targetAssemblyReferences.Count);
            Assert.Same(assemblyReference30, targetAssemblyReferences[0]);
        }

        [Fact]
        public void GetCompatibleReferencesReferenceWithUnspecifiedFrameworkName()
        {
            // Arrange
            var assemblyReference10 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("1.0")));
            var assemblyReference20 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("2.0")));
            var assemblyReference30 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("3.0")));
            var assemblyReference40 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("4.0")));
            var assemblyReferenceNoVersion = PackageUtility.CreateAssemblyReference("foo.dll", null);
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReference10, assemblyReference20, assemblyReference30, assemblyReference40, assemblyReferenceNoVersion };

            // Act
            var targetAssemblyReferences = GetCompatibleItems(new FrameworkName(".NETFramework", new Version("3.5")), assemblyReferences).ToList();

            // Assert
            Assert.Equal(1, targetAssemblyReferences.Count);
            Assert.Same(assemblyReference30, targetAssemblyReferences[0]);
        }

        [Fact]
        public void GetCompatibleReferencesReferenceWithUnspecifiedFrameworkNameWinsIfNoMatchingSpecificFrameworkNames()
        {
            // Arrange
            var assemblyReference20 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("2.0")));
            var assemblyReference30 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("3.0")));
            var assemblyReference40 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("4.0")));
            var assemblyReferenceNoVersion = PackageUtility.CreateAssemblyReference("foo.dll", null);
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReference20, assemblyReference30, assemblyReference40, assemblyReferenceNoVersion };

            // Act
            var targetAssemblyReferences = GetCompatibleItems(new FrameworkName(".NETFramework", new Version("1.1")), assemblyReferences).ToList();

            // Assert
            Assert.Equal(1, targetAssemblyReferences.Count);
            Assert.Same(assemblyReferenceNoVersion, targetAssemblyReferences[0]);
        }


        [Fact]
        public void GetCompatibleReferencesReferenceWithUnspecifiedFrameworkNameWinsIfNoMatchingSpecificFrameworkNamesWithDifferentProfiles()
        {
            // Arrange
            var assemblyReference20 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("2.0")));
            var assemblyReference30 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName("Silverlight", new Version("3.0")));
            var assemblyReference40 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETCore", new Version("4.5")));
            var assemblyReferenceNoVersion = PackageUtility.CreateAssemblyReference("foo.dll", null);
            var assemblyReferenceNoVersion2 = PackageUtility.CreateAssemblyReference("bar.dll", null);
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReferenceNoVersion2, assemblyReference20, assemblyReference30, assemblyReference40, assemblyReferenceNoVersion };

            // Act
            var targetAssemblyReferences = GetCompatibleItems(new FrameworkName("WindowsPhone", new Version("8.0")), assemblyReferences).ToList();

            // Assert
            Assert.Equal(2, targetAssemblyReferences.Count);
            Assert.Same(assemblyReferenceNoVersion, targetAssemblyReferences[1]);
            Assert.Same(assemblyReferenceNoVersion2, targetAssemblyReferences[0]);
        }

        [Fact]
        public void GetCompatibleReferencesReferenceMostSpecificVersionWins()
        {
            // Arrange
            var assemblyReference10 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("1.0")));
            var assemblyReference20 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("2.0")));
            var assemblyReference30 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("3.0")));
            var assemblyReference40 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("4.0")));
            var assemblyReferenceNoVersion = PackageUtility.CreateAssemblyReference("foo.dll", null);
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReference10, assemblyReference20, assemblyReference30, assemblyReference40, assemblyReferenceNoVersion };

            // Act
            var targetAssemblyReferences = GetCompatibleItems(new FrameworkName(".NETFramework", new Version("4.0")), assemblyReferences).ToList();

            // Assert
            Assert.Equal(1, targetAssemblyReferences.Count);
            Assert.Same(assemblyReference40, targetAssemblyReferences[0]);
        }

        [Fact]
        public void GetCompatibleReferencesHighestSpecifiedAssemblyLessThanProjectTargetFrameworkWins()
        {
            // Arrange
            var assemblyReference10 = PackageUtility.CreateAssemblyReference("foo1.dll", new FrameworkName(".NETFramework", new Version("1.0")));
            var assemblyReference20 = PackageUtility.CreateAssemblyReference("foo1.dll", new FrameworkName(".NETFramework", new Version("2.0")));
            var assemblyReference30 = PackageUtility.CreateAssemblyReference("foo2.dll", new FrameworkName(".NETFramework", new Version("3.0")));
            var assemblyReference40 = PackageUtility.CreateAssemblyReference("foo2.dll", new FrameworkName(".NETFramework", new Version("4.0")));
            var assemblyReferenceNoVersion = PackageUtility.CreateAssemblyReference("foo3.dll", null);
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReference10, assemblyReference20, assemblyReference30, assemblyReference40, assemblyReferenceNoVersion };

            // Act
            var compatibleAssemblyReferences = GetCompatibleItems(new FrameworkName(".NETFramework", new Version("3.5")), assemblyReferences).ToList();

            // Assert
            Assert.Equal(1, compatibleAssemblyReferences.Count);
            Assert.Equal(assemblyReference30, compatibleAssemblyReferences[0]);
        }

        [Fact]
        public void GetCompatibleReferencesReturnsNullIfNoBestMatchFound()
        {
            // Arrange
            var assemblyReference = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("5.0")));
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReference };

            // Act
            var compatibleAssemblyReferences = GetCompatibleItems(new FrameworkName(".NETFramework", new Version("3.5")), assemblyReferences);

            // Assert
            Assert.Null(compatibleAssemblyReferences);
        }

        [Fact]
        public void GetCompatibleReferencesMostSpecificFrameworkIfProfileNameSpecified()
        {
            // Arrange
            var assemblyReference30client = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("3.0"), "Client"));
            var assemblyReference40 = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("4.0")));
            var assemblyReferenceNoVersion = PackageUtility.CreateAssemblyReference("foo.dll", null);
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReference30client, assemblyReference40, assemblyReferenceNoVersion };

            // Act
            var compatibleAssemblyReferences = GetCompatibleItems(new FrameworkName(".NETFramework", new Version("4.0"), "Client"), assemblyReferences).ToList();

            // Assert
            Assert.Equal(1, compatibleAssemblyReferences.Count);
            Assert.Equal(assemblyReference40, compatibleAssemblyReferences[0]);
        }

        [Fact]
        public void GetCompatibleReferencesNoProjectFrameworkSpecified()
        {
            // Arrange
            var assemblyReference = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("5.0")));
            var assemblyReferenceNullFrameworkName = PackageUtility.CreateAssemblyReference("foo2.dll", null);
            var assemblyReferences = new IPackageAssemblyReference[] { assemblyReference, assemblyReferenceNullFrameworkName };

            // Act
            var compatibleAssemblyReferences = GetCompatibleItems(null, assemblyReferences).ToList();

            // Assert
            Assert.Equal(1, compatibleAssemblyReferences.Count);
            Assert.Equal(assemblyReferenceNullFrameworkName, compatibleAssemblyReferences[0]);
        }

        [Fact]
        public void AddFilesCallResolveFileConflictIfThereIsFileConflict()
        {
            var logger = new Mock<ILogger>();

            // Arrange
            var project = new MockProjectSystem(VersionUtility.ParseFrameworkName("net40"), @"x:\root");
            project.AddFile("a.txt", "this is a");
            project.AddFile("c.txt", "this is c");
            project.Logger = logger.Object;

            var files = PackageUtility.CreateFiles(new[] { "a.txt", "b.txt" }, "content");

            // Act
            project.AddFiles(files, new Dictionary<FileTransformExtensions, IPackageFileTransformer>());

            // Assert
            logger.Verify(l => l.ResolveFileConflict(@"File 'a.txt' already exists in project 'x:\root'. Do you want to overwrite it?"), Times.Once());
        }

        [Fact]
        public void AddFilesDoNotCallResolveFileConflictIfThereIsNoFileConflict()
        {
            var logger = new Mock<ILogger>();

            // Arrange
            var project = new MockProjectSystem();
            project.AddFile("a.txt", "this is a");
            project.AddFile("c.txt", "this is c");
            project.Logger = logger.Object;

            var files = PackageUtility.CreateFiles(new[] { "b.txt", "d.txt" }, "content");

            // Act
            project.AddFiles(files, new Dictionary<FileTransformExtensions, IPackageFileTransformer>());

            // Assert
            logger.Verify(l => l.ResolveFileConflict(It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public void AddFilesDoNotCallResolveFileConflictIfTheConflictFileDoesNotBelongToProject()
        {
            var logger = new Mock<ILogger>();

            // Arrange
            var project = new MockProjectSystem();
            project.AddFile("a.txt", "this is a");
            project.AddFile("b.txt", "this is b");
            project.ExcludeFileFromProject("b.txt");
            project.Logger = logger.Object;

            var files = PackageUtility.CreateFiles(new[] { "b.txt", "d.txt" }, "content");

            // Act
            project.AddFiles(files, new Dictionary<FileTransformExtensions, IPackageFileTransformer>());

            // Assert
            Assert.True(project.FileExists("d.txt"));
            Assert.False(project.FileExistsInProject("b.txt"));
            logger.Verify(l => l.ResolveFileConflict(It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public void AddFilesAskingForResolutionForEveryConflict()
        {
            var resolutions = new FileConflictResolution[] 
            { 
                FileConflictResolution.Ignore,
                FileConflictResolution.Overwrite,
                FileConflictResolution.Ignore,
                FileConflictResolution.IgnoreAll,
                FileConflictResolution.OverwriteAll,
                FileConflictResolution.Overwrite,
            };

            var index = 0;
            var logger = new Mock<ILogger>();
            logger.Setup(l => l.ResolveFileConflict(It.IsAny<string>()))
                  .Returns(() => resolutions[index++]);

            // Arrange
            var project = new MockProjectSystem();
            project.AddFile("a.txt", "this is a");
            project.AddFile("b.txt", "this is b");
            project.AddFile("c.txt", "this is c");
            project.AddFile("d.txt", "this is d");
            project.AddFile("e.txt", "this is e");
            project.AddFile("f.txt", "this is f");
            project.Logger = logger.Object;

            var files = PackageUtility.CreateFiles(new [] { "a.txt", "b.txt", "c.txt", "d.txt", "e.txt", "f.txt" }, "content");

            // Act
            project.AddFiles(files, new Dictionary<FileTransformExtensions, IPackageFileTransformer>());

            // Assert
            Assert.True(project.FileExists("a.txt"));
            Assert.True(project.FileExists("b.txt"));
            Assert.True(project.FileExists("c.txt"));
            Assert.True(project.FileExists("d.txt"));
            Assert.True(project.FileExists("e.txt"));
            Assert.True(project.FileExists("f.txt"));

            logger.Verify(l => l.ResolveFileConflict(It.IsAny<string>()), Times.Exactly(6));

            Assert.Equal("this is a", project.ReadAllText("a.txt"));
            Assert.Equal("content\\b.txt", project.ReadAllText("b.txt"));
            Assert.Equal("this is c", project.ReadAllText("c.txt"));
            Assert.Equal("this is d", project.ReadAllText("d.txt"));
            Assert.Equal("content\\e.txt", project.ReadAllText("e.txt"));
            Assert.Equal("content\\f.txt", project.ReadAllText("f.txt"));
        }

        private IEnumerable<T> GetCompatibleItems<T>(FrameworkName frameworkName, IEnumerable<T> items) where T : IFrameworkTargetable
        {
            IEnumerable<T> compatibleItems;
            VersionUtility.TryGetCompatibleItems(frameworkName, items, out compatibleItems);
            return compatibleItems;
        }
    }
}