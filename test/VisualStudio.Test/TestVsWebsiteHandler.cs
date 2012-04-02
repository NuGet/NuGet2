using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Moq;
using NuGet.Test.Mocks;
using EnvDTE;

namespace NuGet.VisualStudio.Test
{
    public class TestVsWebsiteHandler
    {
        [Fact]
        public void AddRefreshFilesForAssembliesReferencesIncludeFilesUnderLibRoot()
        {
            // Arrange
            var projectSystem = new MockProjectSystem();
            var projectSystemFactory = new Mock<IProjectSystemFactory>();
            projectSystemFactory.Setup(p => p.CreateProjectSystem(It.IsAny<Project>(), It.IsAny<IFileSystemProvider>()))
                                .Returns(projectSystem);

            var websiteHandler = new VsWebsiteHandler(null, projectSystemFactory.Object);

            var packagesFileSystem = new MockFileSystem("x:\\some\\path");
            packagesFileSystem.CreateDirectory("A.1.0");
            packagesFileSystem.CreateDirectory("A.1.0\\lib");
            packagesFileSystem.AddFile("A.1.0\\lib\\one.dll");
            packagesFileSystem.AddFile("A.1.0\\lib\\two.winmd");

            packagesFileSystem.CreateDirectory("B.3.0.0-beta");
            packagesFileSystem.CreateDirectory("B.3.0.0-beta\\lib");
            packagesFileSystem.AddFile("B.3.0.0-beta\\lib\\three.dll");
            packagesFileSystem.AddFile("B.3.0.0-beta\\lib\\four.exe");

            var properties = new MockDteProperties();
            properties.AddProperty("FullPath", "x:\\project");
            properties.AddProperty("TargetFrameworkMoniker", ".NETFramework,Version=v4.0");

            var website = new Mock<Project>();
            website.Setup(p => p.Kind).Returns(VsConstants.WebSiteProjectTypeGuid);
            website.Setup(p => p.Properties).Returns(properties);

            // Act
            websiteHandler.AddRefreshFilesForReferences(
                website.Object, packagesFileSystem, new[] { new PackageName("A", new SemanticVersion("1.0")), 
                                                            new PackageName("B", new SemanticVersion("3.0.0-beta"))});

            // Assert
            Assert.True(projectSystem.DirectoryExists("bin"));
            var refreshFiles = projectSystem.GetFiles("bin", "*.refresh").OrderBy(s => s).ToList();
            Assert.Equal(4, refreshFiles.Count);
            Assert.Equal("bin\\four.exe.refresh", refreshFiles[0]);
            Assert.Equal("bin\\one.dll.refresh", refreshFiles[1]);
            Assert.Equal("bin\\three.dll.refresh", refreshFiles[2]);
            Assert.Equal("bin\\two.winmd.refresh", refreshFiles[3]);
        }

        [Fact]
        public void AddRefreshFilesForAssembliesReferencesUseCorrectFrameworkFolders()
        {
            // Arrange
            var projectSystem = new MockProjectSystem();
            var projectSystemFactory = new Mock<IProjectSystemFactory>();
            projectSystemFactory.Setup(p => p.CreateProjectSystem(It.IsAny<Project>(), It.IsAny<IFileSystemProvider>()))
                                .Returns(projectSystem);

            var websiteHandler = new VsWebsiteHandler(null, projectSystemFactory.Object);

            var packagesFileSystem = new MockFileSystem("x:\\some\\path");
            packagesFileSystem.CreateDirectory("A.1.0");
            packagesFileSystem.CreateDirectory("A.1.0\\lib");
            packagesFileSystem.AddFile("A.1.0\\lib\\.NETFramework40\\one.dll");
            packagesFileSystem.AddFile("A.1.0\\lib\\sl40\\aaa.dll");
            packagesFileSystem.AddFile("A.1.0\\lib\\two.winmd");

            packagesFileSystem.CreateDirectory("B.3.0.0-beta");
            packagesFileSystem.CreateDirectory("B.3.0.0-beta\\lib");
            packagesFileSystem.AddFile("B.3.0.0-beta\\lib\\net40\\three.dll");
            packagesFileSystem.AddFile("B.3.0.0-beta\\lib\\four.exe");

            var properties = new MockDteProperties();
            properties.AddProperty("FullPath", "x:\\project");
            properties.AddProperty("TargetFrameworkMoniker", ".NETFramework,Version=v4.0");

            var website = new Mock<Project>();
            website.Setup(p => p.Kind).Returns(VsConstants.WebSiteProjectTypeGuid);
            website.Setup(p => p.Properties).Returns(properties);

            // Act
            websiteHandler.AddRefreshFilesForReferences(
                website.Object, packagesFileSystem, new[] { new PackageName("A", new SemanticVersion("1.0")), 
                                                            new PackageName("B", new SemanticVersion("3.0.0-beta"))});

            // Assert
            Assert.True(projectSystem.DirectoryExists("bin"));
            var refreshFiles = projectSystem.GetFiles("bin", "*.refresh").OrderBy(s => s).ToList();
            Assert.Equal(2, refreshFiles.Count);
            Assert.Equal("bin\\one.dll.refresh", refreshFiles[0]);
            Assert.Equal("bin\\three.dll.refresh", refreshFiles[1]);
        }

        [Fact]
        public void AddRefreshFilesForAssembliesReferencesIgnoreMissingPackages()
        {
            // Arrange
            var projectSystem = new MockProjectSystem();
            var projectSystemFactory = new Mock<IProjectSystemFactory>();
            projectSystemFactory.Setup(p => p.CreateProjectSystem(It.IsAny<Project>(), It.IsAny<IFileSystemProvider>()))
                                .Returns(projectSystem);

            var websiteHandler = new VsWebsiteHandler(null, projectSystemFactory.Object);

            var packagesFileSystem = new MockFileSystem("x:\\some\\path");
            packagesFileSystem.CreateDirectory("A.1.0");
            packagesFileSystem.CreateDirectory("A.1.0\\lib");
            packagesFileSystem.AddFile("A.1.0\\lib\\.NETFramework40\\one.dll");
            packagesFileSystem.AddFile("A.1.0\\lib\\sl40\\aaa.dll");
            packagesFileSystem.AddFile("A.1.0\\lib\\two.winmd");

            var properties = new MockDteProperties();
            properties.AddProperty("FullPath", "x:\\project");
            properties.AddProperty("TargetFrameworkMoniker", ".NETFramework,Version=v4.0");

            var website = new Mock<Project>();
            website.Setup(p => p.Kind).Returns(VsConstants.WebSiteProjectTypeGuid);
            website.Setup(p => p.Properties).Returns(properties);

            // Act
            websiteHandler.AddRefreshFilesForReferences(
                website.Object, packagesFileSystem, new[] { new PackageName("A", new SemanticVersion("1.0")), 
                                                            new PackageName("B", new SemanticVersion("3.0.0-beta"))});

            // Assert
            Assert.True(projectSystem.DirectoryExists("bin"));
            var refreshFiles = projectSystem.GetFiles("bin", "*.refresh").OrderBy(s => s).ToList();
            Assert.Equal(1, refreshFiles.Count);
            Assert.Equal("bin\\one.dll.refresh", refreshFiles[0]);
        }

        [Fact]
        public void AddRefreshFilesForAssembliesReferencesUseCorrectPackageVersion()
        {
            // Arrange
            var projectSystem = new MockProjectSystem();
            var projectSystemFactory = new Mock<IProjectSystemFactory>();
            projectSystemFactory.Setup(p => p.CreateProjectSystem(It.IsAny<Project>(), It.IsAny<IFileSystemProvider>()))
                                .Returns(projectSystem);

            var websiteHandler = new VsWebsiteHandler(null, projectSystemFactory.Object);

            var packagesFileSystem = new MockFileSystem("x:\\some\\path");
            packagesFileSystem.CreateDirectory("A.1.0");
            packagesFileSystem.CreateDirectory("A.1.0\\lib");
            packagesFileSystem.CreateDirectory("A.1.0\\lib\\.NETFramework40");
            packagesFileSystem.AddFile("A.1.0\\lib\\.NETFramework40\\one.dll");
            packagesFileSystem.AddFile("A.1.0\\lib\\sl40\\aaa.dll");
            packagesFileSystem.AddFile("A.1.0\\lib\\two.winmd");

            packagesFileSystem.CreateDirectory("A.2.0");
            packagesFileSystem.CreateDirectory("A.2.0\\lib");
            packagesFileSystem.CreateDirectory("A.2.0\\lib\\.NETFramework40");
            packagesFileSystem.AddFile("A.2.0\\lib\\.NETFramework40\\onev2.dll");

            var properties = new MockDteProperties();
            properties.AddProperty("FullPath", "x:\\project");
            properties.AddProperty("TargetFrameworkMoniker", ".NETFramework,Version=v4.0");

            var website = new Mock<Project>();
            website.Setup(p => p.Kind).Returns(VsConstants.WebSiteProjectTypeGuid);
            website.Setup(p => p.Properties).Returns(properties);

            // Act
            websiteHandler.AddRefreshFilesForReferences(
                website.Object, packagesFileSystem, new[] { new PackageName("A", new SemanticVersion("2.0")), 
                                                            new PackageName("B", new SemanticVersion("3.0.0-beta"))});

            // Assert
            Assert.True(projectSystem.DirectoryExists("bin"));
            var refreshFiles = projectSystem.GetFiles("bin", "*.refresh").OrderBy(s => s).ToList();
            Assert.Equal(1, refreshFiles.Count);
            Assert.Equal("bin\\onev2.dll.refresh", refreshFiles[0]);
        }

        [Fact]
        public void CopyNativeBinariesForFilesUnderTheSameFolderName()
        {
            // Arrange
            var projectSystem = new MockProjectSystem();
            var projectSystemFactory = new Mock<IProjectSystemFactory>();
            projectSystemFactory.Setup(p => p.CreateProjectSystem(It.IsAny<Project>(), It.IsAny<IFileSystemProvider>()))
                                .Returns(projectSystem);

            var websiteHandler = new VsWebsiteHandler(null, projectSystemFactory.Object);

            var packagesFileSystem = new MockFileSystem("x:\\some\\path");
            packagesFileSystem.CreateDirectory("A.1.0");
            packagesFileSystem.CreateDirectory("A.1.0\\nativebinaries");
            packagesFileSystem.AddFile("A.1.0\\NativeBinaries\\one.dll");
            packagesFileSystem.AddFile("A.1.0\\NativeBinaries\\two.winmd");

            packagesFileSystem.CreateDirectory("B.3.0.0-beta");
            packagesFileSystem.CreateDirectory("B.3.0.0-beta\\nativebinaries");
            packagesFileSystem.AddFile("B.3.0.0-beta\\nativebinaries\\three.txt");
            packagesFileSystem.AddFile("B.3.0.0-beta\\nativebinaries\\four.cd");
            packagesFileSystem.CreateDirectory("B.3.0.0-beta\\lib");
            packagesFileSystem.AddFile("B.3.0.0-beta\\lib\\forbidden.dll");

            var website = new Mock<Project>();
            website.Setup(p => p.Kind).Returns(VsConstants.WebSiteProjectTypeGuid);

            // Act
            websiteHandler.CopyNativeBinaries(
                website.Object, packagesFileSystem, new[] { new PackageName("A", new SemanticVersion("1.0")), 
                                                            new PackageName("B", new SemanticVersion("3.0.0-beta"))});

            // Assert
            Assert.True(projectSystem.DirectoryExists("bin"));
            var nativeBinaries = projectSystem.GetFiles("bin", "*.*").OrderBy(s => s).ToList();
            Assert.Equal(4, nativeBinaries.Count);
            Assert.Equal("bin\\four.cd", nativeBinaries[0]);
            Assert.Equal("bin\\one.dll", nativeBinaries[1]);
            Assert.Equal("bin\\three.txt", nativeBinaries[2]);
            Assert.Equal("bin\\two.winmd", nativeBinaries[3]);
        }
    }
}