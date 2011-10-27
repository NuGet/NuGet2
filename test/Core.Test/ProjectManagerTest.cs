using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using Moq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{

    public class ProjectManagerTest
    {
        [Fact]
        public void AddingPackageReferenceNullOrEmptyPackageIdThrows()
        {
            // Arrange
            ProjectManager projectManager = CreateProjectManager();

            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => projectManager.AddPackageReference((string)null), "packageId");
            ExceptionAssert.ThrowsArgNullOrEmpty(() => projectManager.AddPackageReference(String.Empty), "packageId");
        }

        [Fact]
        public void AddingUnknownPackageReferenceThrows()
        {
            // Arrange
            ProjectManager projectManager = CreateProjectManager();

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.AddPackageReference("unknown"), "Unable to find package 'unknown'.");
        }

        [Fact]
        public void AddingPackageReferenceThrowsExceptionPackageReferenceIsAdded()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new Mock<MockProjectSystem>() { CallBase = true };
            projectSystem.Setup(m => m.AddFile("file", It.IsAny<Stream>())).Throws<UnauthorizedAccessException>();
            projectSystem.Setup(m => m.Root).Returns("FakeRoot");
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem.Object), projectSystem.Object, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", new[] { "file" });
            sourceRepository.AddPackage(packageA);

            // Act
            ExceptionAssert.Throws<UnauthorizedAccessException>(() => projectManager.AddPackageReference("A"));

            // Assert
            Assert.True(projectManager.LocalRepository.Exists(packageA));
        }

        [Fact]
        public void AddingPackageReferenceAddsPreprocessedFileToTargetPathWithRemovedExtension()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", new[] { @"foo\bar\file.pp" });
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.False(projectSystem.FileExists(@"foo\bar\file.pp"));
            Assert.True(projectSystem.FileExists(@"foo\bar\file"));
        }

        [Fact]
        public void AddPackageReferenceWhenNewVersionOfPackageAlreadyReferencedThrows()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                    new PackageDependency("B")
                                                                }, content: new[] { "foo" });
            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                    new PackageDependency("B")
                                                                }, content: new[] { "foo" });
            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0", content: new[] { "foo" });
            projectManager.LocalRepository.AddPackage(packageA20);
            projectManager.LocalRepository.AddPackage(packageB10);

            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.AddPackageReference("A", SemanticVersion.Parse("1.0")), @"Already referencing a newer version of 'A'.");
        }

        [Fact]
        public void RemovingUnknownPackageReferenceThrows()
        {
            // Arrange
            var projectManager = CreateProjectManager();

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.RemovePackageReference("foo"), "Unable to find package 'foo'.");
        }

        [Fact]
        public void RemovingPackageReferenceWithOtherProjectWithReferencesThatWereNotCopiedToProject()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            var packageA = PackageUtility.CreatePackage("A", "1.0", content: new[] { "a.file" });
            var packageB = PackageUtility.CreatePackage("B", "1.0",
                                                        content: null,
                                                        assemblyReferences: new[] { PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName("SP", new Version("40.0"))) },
                                                        tools: null,
                                                        dependencies: null,
                                                        downloadCount: 0,
                                                        description: null,
                                                        summary: null);
            projectManager.LocalRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageA);
            projectManager.LocalRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageB);

            // Act
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.False(projectManager.LocalRepository.Exists(packageA));
        }

        [Fact]
        public void RemovingUnknownPackageReferenceNullOrEmptyPackageIdThrows()
        {
            // Arrange
            var projectManager = CreateProjectManager();

            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => projectManager.RemovePackageReference((string)null), "packageId");
            ExceptionAssert.ThrowsArgNullOrEmpty(() => projectManager.RemovePackageReference(String.Empty), "packageId");
        }

        [Fact]
        public void RemovingPackageReferenceWithNoDependents()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            var package = PackageUtility.CreatePackage("foo", "1.2.33", content: new[] { "file1" });
            projectManager.LocalRepository.AddPackage(package);
            sourceRepository.AddPackage(package);

            // Act
            projectManager.RemovePackageReference("foo");

            // Assert
            Assert.False(projectManager.LocalRepository.Exists(package));
        }

        [Fact]
        public void AddPackageReferenceAddsContentAndReferencesProjectSystem()
        {
            // Arrange
            var projectSystem = new MockProjectSystem();
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        new[] { "contentFile" },
                                                        new[] { "reference.dll" },
                                                        new[] { "tool" });

            mockRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.Equal(1, projectSystem.Paths.Count);
            Assert.Equal(1, projectSystem.References.Count);
            Assert.True(projectSystem.References.ContainsKey(@"reference.dll"));
            Assert.True(projectSystem.FileExists(@"contentFile"));
            Assert.True(localRepository.Exists("A"));
        }

        [Fact]
        public void AddPackageReferenceAddingPackageWithDuplicateReferenceOverwritesReference()
        {
            // Arrange
            var projectSystem = new MockProjectSystem();
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        assemblyReferences: new[] { "reference.dll" });
            var packageB = PackageUtility.CreatePackage("B", "1.0",
                                                        assemblyReferences: new[] { "reference.dll" });

            mockRepository.AddPackage(packageA);
            mockRepository.AddPackage(packageB);

            // Act
            projectManager.AddPackageReference("A");
            projectManager.AddPackageReference("B");

            // Assert
            Assert.Equal(0, projectSystem.Paths.Count);
            Assert.Equal(1, projectSystem.References.Count);
            Assert.True(projectSystem.References.ContainsKey(@"reference.dll"));
            Assert.True(projectSystem.References.ContainsValue(@"B.1.0\reference.dll"));
            Assert.True(localRepository.Exists("A"));
            Assert.True(localRepository.Exists("B"));
        }

        [Fact]
        public void AddPackageReferenceRaisesOnBeforeInstallAndOnAfterInstall()
        {
            // Arrange
            var projectSystem = new MockProjectSystem();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        new[] { "contentFile" },
                                                        new[] { "reference.dll" },
                                                        new[] { "tool" });
            projectManager.PackageReferenceAdding += (sender, e) =>
            {
                // Assert
                Assert.Equal(e.InstallPath, @"C:\MockFileSystem\A.1.0");
                Assert.Same(e.Package, packageA);
            };

            projectManager.PackageReferenceAdded += (sender, e) =>
            {
                // Assert
                Assert.Equal(e.InstallPath, @"C:\MockFileSystem\A.1.0");
                Assert.Same(e.Package, packageA);
            };

            mockRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");
        }

        [Fact]
        public void RemovePackageReferenceRaisesOnBeforeUninstallAndOnAfterUninstall()
        {
            // Arrange
            var mockProjectSystem = new MockProjectSystem();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(new MockProjectSystem()), mockProjectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                             new[] { @"sub\file1", @"sub\file2" });
            projectManager.PackageReferenceRemoving += (sender, e) =>
            {
                // Assert
                Assert.Equal(e.InstallPath, @"C:\MockFileSystem\A.1.0");
                Assert.Same(e.Package, packageA);
            };

            projectManager.PackageReferenceRemoved += (sender, e) =>
            {
                // Assert
                Assert.Equal(e.InstallPath, @"C:\MockFileSystem\A.1.0");
                Assert.Same(e.Package, packageA);
            };

            mockRepository.AddPackage(packageA);
            projectManager.AddPackageReference("A");

            // Act
            projectManager.RemovePackageReference("A");
        }

        [Fact]
        public void RemovePackageReferenceExcludesFileIfAnotherPackageUsesThem()
        {
            // Arrange
            var mockProjectSystem = new MockProjectSystem();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(new MockProjectSystem()), mockProjectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                             new[] { "fileA", "commonFile" });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0",
                                                            new[] { "fileB", "commonFile" });

            mockRepository.AddPackage(packageA);
            mockRepository.AddPackage(packageB);

            projectManager.AddPackageReference("A");
            projectManager.AddPackageReference("B");

            // Act
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.True(mockProjectSystem.Deleted.Contains(@"fileA"));
            Assert.True(mockProjectSystem.FileExists(@"commonFile"));
        }

        [Fact]
        public void AddPackageWithUnsupportedFilesSkipsUnsupportedFiles()
        {
            // Arrange            
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new Mock<MockProjectSystem>() { CallBase = true };
            projectSystem.Setup(m => m.IsSupportedFile("unsupported")).Returns(false);
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem.Object), projectSystem.Object, localRepository);
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", new[] { "a", "b", "unsupported" });
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.Equal(2, projectSystem.Object.Paths.Count);
            Assert.True(projectSystem.Object.FileExists("a"));
            Assert.True(projectSystem.Object.FileExists("b"));
            Assert.True(localRepository.Exists("A"));
            Assert.False(projectSystem.Object.FileExists("unsupported"));
        }

        [Fact]
        public void AddPackageWithUnsupportedTransformFileSkipsUnsupportedFile()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var localRepository = new MockPackageRepository();
            var projectSystem = new Mock<MockProjectSystem>() { CallBase = true };
            projectSystem.Setup(m => m.IsSupportedFile("unsupported")).Returns(false);
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem.Object), projectSystem.Object, localRepository);
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", new[] { "a", "b", "unsupported.pp" });
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.Equal(2, projectSystem.Object.Paths.Count);
            Assert.True(projectSystem.Object.FileExists("a"));
            Assert.True(projectSystem.Object.FileExists("b"));
            Assert.True(localRepository.Exists("A"));
            Assert.False(projectSystem.Object.FileExists("unsupported"));
        }

        [Fact]
        public void AddPackageWithTransformFile()
        {
            // Arrange
            var mockProjectSystem = new MockProjectSystem();
            var mockRepository = new MockPackageRepository();
            mockProjectSystem.AddFile("web.config",
@"<configuration>
    <system.web>
        <compilation debug=""true"" targetFramework=""4.0"" />
    </system.web>
</configuration>
".AsStream());
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(new MockProjectSystem()), mockProjectSystem, new MockPackageRepository());
            var package = new Mock<IPackage>();
            package.Setup(m => m.Id).Returns("A");
            package.Setup(m => m.Version).Returns(new SemanticVersion("1.0"));
            var file = new Mock<IPackageFile>();
            file.Setup(m => m.Path).Returns(@"content\web.config.transform");
            file.Setup(m => m.GetStream()).Returns(() =>
@"<configuration>
    <configSections>
        <add a=""n"" />
    </configSections>
</configuration>
".AsStream());
            package.Setup(m => m.GetFiles()).Returns(new[] { file.Object });
            mockRepository.AddPackage(package.Object);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <configSections>
    <add a=""n"" />
  </configSections>
  <system.web>
    <compilation debug=""true"" targetFramework=""4.0"" />
  </system.web>
</configuration>", mockProjectSystem.OpenFile("web.config").ReadToEnd());
        }

        [Fact]
        public void RemovePackageWithTransformFile()
        {
            // Arrange
            var mockProjectSystem = new MockProjectSystem();
            var mockRepository = new MockPackageRepository();
            mockProjectSystem.AddFile("web.config",
@"<configuration>
    <system.web>
        <compilation debug=""true"" targetFramework=""4.0"" baz=""test"" />
    </system.web>
</configuration>
".AsStream());
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(new MockProjectSystem()), mockProjectSystem, new MockPackageRepository());
            var package = new Mock<IPackage>();
            package.Setup(m => m.Id).Returns("A");
            package.Setup(m => m.Version).Returns(new SemanticVersion("1.0"));
            var file = new Mock<IPackageFile>();
            file.Setup(m => m.Path).Returns(@"content\web.config.transform");
            file.Setup(m => m.GetStream()).Returns(() =>
@"<configuration>
    <system.web>
        <compilation debug=""true"" targetFramework=""4.0"" />
    </system.web>
</configuration>
".AsStream());
            package.Setup(m => m.GetFiles()).Returns(new[] { file.Object });
            mockRepository.AddPackage(package.Object);
            projectManager.LocalRepository.AddPackage(package.Object);

            // Act
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <system.web>
    <compilation baz=""test"" />
  </system.web>
</configuration>", mockProjectSystem.OpenFile("web.config").ReadToEnd());
        }

        [Fact]
        public void RemovePackageWithTransformFileThatThrowsContinuesRemovingPackage()
        {
            // Arrange
            var mockProjectSystem = new MockProjectSystem();
            var mockRepository = new MockPackageRepository();
            var localRepository = new MockPackageRepository();
            mockProjectSystem.AddFile("web.config", () => { throw new UnauthorizedAccessException(); });
            mockProjectSystem.AddFile("foo.txt");
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(new MockProjectSystem()), mockProjectSystem, localRepository);
            var package = new Mock<IPackage>();
            package.Setup(m => m.Id).Returns("A");
            package.Setup(m => m.Version).Returns(new SemanticVersion("1.0"));
            var file = new Mock<IPackageFile>();
            var contentFile = new Mock<IPackageFile>();
            contentFile.Setup(m => m.Path).Returns(@"content\foo.txt");
            contentFile.Setup(m => m.GetStream()).Returns(new MemoryStream());
            file.Setup(m => m.Path).Returns(@"content\web.config.transform");
            file.Setup(m => m.GetStream()).Returns(() =>
@"<configuration>
    <system.web>
        <compilation debug=""true"" targetFramework=""4.0"" />
    </system.web>
</configuration>
".AsStream());
            package.Setup(m => m.GetFiles()).Returns(new[] { file.Object, contentFile.Object });
            mockRepository.AddPackage(package.Object);
            projectManager.LocalRepository.AddPackage(package.Object);

            // Act
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.False(mockProjectSystem.FileExists("foo.txt"));
            Assert.False(localRepository.Exists(package.Object));
        }

        [Fact]
        public void RemovePackageWithUnsupportedTransformFileDoesNothing()
        {
            // Arrange
            var mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };
            mockProjectSystem.Setup(m => m.IsSupportedFile("web.config")).Returns(false);
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(new MockProjectSystem()), mockProjectSystem.Object, new MockPackageRepository());
            var package = new Mock<IPackage>();
            package.Setup(m => m.Id).Returns("A");
            package.Setup(m => m.Version).Returns(new SemanticVersion("1.0"));
            var file = new Mock<IPackageFile>();
            file.Setup(m => m.Path).Returns(@"content\web.config.transform");
            file.Setup(m => m.GetStream()).Returns(() =>
@"<configuration>
    <system.web>
        <compilation debug=""true"" targetFramework=""4.0"" />
    </system.web>
</configuration>
".AsStream());
            package.Setup(m => m.GetFiles()).Returns(new[] { file.Object });
            mockRepository.AddPackage(package.Object);
            projectManager.LocalRepository.AddPackage(package.Object);

            // Act
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.False(mockProjectSystem.Object.FileExists("web.config"));
        }

        [Fact]
        public void RemovePackageRemovesDirectoriesAddedByPackageFilesIfEmpty()
        {
            // Arrange
            var mockProjectSystem = new MockProjectSystem();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(new MockProjectSystem()), mockProjectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                             new[] { @"sub\file1", @"sub\file2" });

            mockRepository.AddPackage(packageA);
            projectManager.AddPackageReference("A");

            // Act
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.True(mockProjectSystem.Deleted.Contains(@"sub\file1"));
            Assert.True(mockProjectSystem.Deleted.Contains(@"sub\file2"));
            Assert.True(mockProjectSystem.Deleted.Contains("sub"));
        }

        [Fact]
        public void AddPackageReferenceWhenOlderVersionOfPackageInstalledDoesAnUpgrade()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                    PackageDependency.CreateDependency("B", "[1.0]")
                                                                },
                                                                content: new[] { "foo" });

            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                    PackageDependency.CreateDependency("B", "[2.0]")
                                                                },
                                                                content: new[] { "bar" });
            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0", content: new[] { "foo" });
            IPackage packageB20 = PackageUtility.CreatePackage("B", "2.0", content: new[] { "foo" });

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);

            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageB20);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.False(projectManager.LocalRepository.Exists(packageA10));
            Assert.False(projectManager.LocalRepository.Exists(packageB10));
            Assert.True(projectManager.LocalRepository.Exists(packageA20));
            Assert.True(projectManager.LocalRepository.Exists(packageB20));
        }

        [Fact]
        public void UpdatePackageNullOrEmptyPackageIdThrows()
        {
            // Arrange
            ProjectManager packageManager = CreateProjectManager();

            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => packageManager.UpdatePackageReference(null), "packageId");
            ExceptionAssert.ThrowsArgNullOrEmpty(() => packageManager.UpdatePackageReference(String.Empty), "packageId");
        }

        [Fact]
        public void UpdatePackageReferenceWithMixedDependenciesUpdatesPackageAndDependenciesIfUnused()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            // A 1.0 -> [B 1.0, C 1.0]
            IPackage packageA10 = PackageUtility.CreatePackage("A",
                                                               "1.0",
                                                               dependencies: new List<PackageDependency> { 
                                                                    PackageDependency.CreateDependency("B","[1.0]"),
                                                                    PackageDependency.CreateDependency("C","[1.0]")
                                                                }, content: new[] { "A.file" });

            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0", content: new[] { "B.fie" });
            IPackage packageC10 = PackageUtility.CreatePackage("C", "1.0", content: new[] { "C.file" });

            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);
            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            projectManager.LocalRepository.AddPackage(packageC10);

            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                               dependencies: new List<PackageDependency> { 
                                                                                    PackageDependency.CreateDependency("B", "[1.0]"),
                                                                                    PackageDependency.CreateDependency("C", "[2.0]"),
                                                                                    PackageDependency.CreateDependency("D", "[1.0]")
                                                               }, content: new[] { "A.20.file" });

            IPackage packageC20 = PackageUtility.CreatePackage("C", "2.0", content: new[] { "C.20" });
            IPackage packageD10 = PackageUtility.CreatePackage("D", "1.0", content: new[] { "D.20" });

            // A 2.0 -> [B 1.0, C 2.0, D 1.0]
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageC20);
            sourceRepository.AddPackage(packageD10);

            // Act
            projectManager.UpdatePackageReference("A");

            // Assert
            Assert.True(projectManager.LocalRepository.Exists(packageA20));
            Assert.True(projectManager.LocalRepository.Exists(packageB10));
            Assert.True(projectManager.LocalRepository.Exists(packageC20));
            Assert.True(projectManager.LocalRepository.Exists(packageD10));
            Assert.False(projectManager.LocalRepository.Exists(packageA10));
            Assert.False(projectManager.LocalRepository.Exists(packageC10));
        }

        [Fact]
        public void UpdatePackageReferenceIfPackageNotReferencedThrows()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.UpdatePackageReference("A"), @"C:\MockFileSystem\ does not reference 'A'.");
        }

        [Fact]
        public void UpdatePackageReferenceToOlderVersionThrows()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            // A 1.0 -> [B 1.0]
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0");
            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0");
            IPackage packageA30 = PackageUtility.CreatePackage("A", "3.0");

            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageA30);

            projectManager.LocalRepository.AddPackage(packageA20);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.UpdatePackageReference("A", version: SemanticVersion.Parse("1.0")), @"Already referencing a newer version of 'A'.");
        }

        [Fact]
        public void UpdatePackageReferenceWithUnresolvedDependencyThrows()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            // A 1.0 -> [B 1.0]
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                               dependencies: new List<PackageDependency> { 
                                                                   PackageDependency.CreateDependency("B", "[1.0]"),
                                                               });

            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0");

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);

            // A 2.0 -> [B 2.0]
            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                    PackageDependency.CreateDependency("B", "[2.0]")
                                                            });

            sourceRepository.AddPackage(packageA20);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.UpdatePackageReference("A"), "Unable to resolve dependency 'B (= 2.0)'.");
        }

        [Fact]
        public void UpdatePackageReferenceWithUpdateDependenciesSetToFalseIgnoresDependencies()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            // A 1.0 -> [B 1.0]
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                               dependencies: new List<PackageDependency> { 
                                                                   PackageDependency.CreateDependency("B", "[1.0]"),
                                                               }, content: new[] { "A.cs" });


            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0", content: new[] { "B.fs.spp" });

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);

            // A 2.0 -> [B 2.0]
            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                    PackageDependency.CreateDependency("B", "[2.0]"),
                                                                }, content: new[] { "D.a" });

            IPackage packageB20 = PackageUtility.CreatePackage("B", "2.0", content: new[] { "B.s" });

            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB20);

            // Act
            projectManager.UpdatePackageReference("A", version: null, updateDependencies: false, allowPrereleaseVersions: false);

            // Assert
            Assert.True(projectManager.LocalRepository.Exists(packageA20));
            Assert.False(projectManager.LocalRepository.Exists(packageA10));
            Assert.True(projectManager.LocalRepository.Exists(packageB10));
            Assert.False(projectManager.LocalRepository.Exists(packageB20));
        }

        [Fact]
        public void UpdatePackageHasNoEffectIfConstraintsDefinedDontAllowForUpdates()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            var constraintProvider = new Mock<IPackageConstraintProvider>();
            constraintProvider.Setup(m => m.GetConstraint("A")).Returns(VersionUtility.ParseVersionSpec("[1.0, 2.0)"));
            constraintProvider.Setup(m => m.Source).Returns("foo");
            projectManager.ConstraintProvider = constraintProvider.Object;
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0");
            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0");

            projectManager.LocalRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);

            // Act
            projectManager.UpdatePackageReference("A");

            // Assert
            Assert.True(projectManager.LocalRepository.Exists(packageA10));
            Assert.False(projectManager.LocalRepository.Exists(packageA20));
        }

        [Fact]
        public void UpdateDependencyDependentsHaveSatisfyableDependencies()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            // A 1.0 -> [C >= 1.0]
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> {                                                                         
                                                                        PackageDependency.CreateDependency("C", "1.0")
                                                                    }, content: new[] { "A" });

            // B 1.0 -> [C <= 2.0]
            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0",
                                                                dependencies: new List<PackageDependency> {                                                                         
                                                                        PackageDependency.CreateDependency("C", "2.0")
                                                                    }, content: new[] { "B" });

            IPackage packageC10 = PackageUtility.CreatePackage("C", "1.0", content: new[] { "C" });

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            projectManager.LocalRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);

            IPackage packageC20 = PackageUtility.CreatePackage("C", "2.0", content: new[] { "C2" });

            // A 2.0 -> [B 1.0, C 2.0, D 1.0]
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageC20);

            // Act
            projectManager.UpdatePackageReference("C");

            // Assert
            Assert.True(projectManager.LocalRepository.Exists(packageA10));
            Assert.True(projectManager.LocalRepository.Exists(packageB10));
            Assert.True(projectManager.LocalRepository.Exists(packageC20));
            Assert.False(projectManager.LocalRepository.Exists(packageC10));
        }

        [Fact]
        public void UpdatePackageReferenceDoesNothingIfVersionIsNotSpecifiedAndNewVersionIsLessThanOldPrereleaseVersion()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            var packageA1 = PackageUtility.CreatePackage("A", "1.0", content: new string[] { "good" });
            var packageA2 = PackageUtility.CreatePackage("A", "2.0-alpha", content: new string[] { "excellent" });

            // project has A 2.0alpha installed
            projectManager.LocalRepository.AddPackage(packageA2);

            sourceRepository.AddPackage(packageA1);

            // Act
            projectManager.UpdatePackageReference("A", version: null, updateDependencies: false, allowPrereleaseVersions: false);

            // Assert
            Assert.True(projectManager.LocalRepository.Exists("A", new SemanticVersion("2.0-alpha")));
            Assert.False(projectManager.LocalRepository.Exists("A", new SemanticVersion("1.0")));
        }

        [Fact]
        public void UpdatePackageReferenceUpdateToNewerVersionIfPrereleaseFlagIsSet()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            var packageA1 = PackageUtility.CreatePackage("A", "1.0", content: new string[] {"good"});
            var packageA2 = PackageUtility.CreatePackage("A", "2.0-alpha", content: new string[] {"excellent"});

            // project has A 1.0 installed
            projectManager.LocalRepository.AddPackage(packageA1);

            sourceRepository.AddPackage(packageA2);

            // Act
            projectManager.UpdatePackageReference("A", version: null, updateDependencies: false, allowPrereleaseVersions: true);

            // Assert
            Assert.True(projectManager.LocalRepository.Exists("A", new SemanticVersion("2.0-alpha")));
        }

        [Fact]
        public void UpdatePackageReferenceWithSatisfyableDependencies()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            // A 1.0 -> [B 1.0, C 1.0]
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0]"),
                                                                        PackageDependency.CreateDependency("C", "[1.0]")
                                                                    }, content: new[] { "file" });

            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0", new[] { "Bfile" });
            IPackage packageC10 = PackageUtility.CreatePackage("C", "1.0", new[] { "Cfile" });

            // G 1.0 -> [C (>= 1.0)]
            IPackage packageG10 = PackageUtility.CreatePackage("G", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("C", "1.0")
                                                                    }, content: new[] { "Gfile" });

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            projectManager.LocalRepository.AddPackage(packageC10);
            projectManager.LocalRepository.AddPackage(packageG10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageG10);

            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0]"),
                                                                        PackageDependency.CreateDependency("C", "[2.0]"),
                                                                        PackageDependency.CreateDependency("D", "[1.0]")
                                                                    }, content: new[] { "A20file" });

            IPackage packageC20 = PackageUtility.CreatePackage("C", "2.0", new[] { "C20file" });
            IPackage packageD10 = PackageUtility.CreatePackage("D", "1.0", new[] { "D20file" });

            // A 2.0 -> [B 1.0, C 2.0, D 1.0]
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageC20);
            sourceRepository.AddPackage(packageD10);

            // Act
            projectManager.UpdatePackageReference("A");

            // Assert
            Assert.True(projectManager.LocalRepository.Exists(packageA20));
            Assert.True(projectManager.LocalRepository.Exists(packageB10));
            Assert.True(projectManager.LocalRepository.Exists(packageC20));
            Assert.True(projectManager.LocalRepository.Exists(packageD10));
            Assert.True(projectManager.LocalRepository.Exists(packageG10));

            Assert.False(projectManager.LocalRepository.Exists(packageC10));
            Assert.False(projectManager.LocalRepository.Exists(packageA10));
        }

        [Fact]
        public void UpdatePackageReferenceWithDependenciesInUseThrowsConflictError()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            // A 1.0 -> [B 1.0, C 1.0]
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0]"),
                                                                        PackageDependency.CreateDependency("C", "[1.0]")
                                                                    }, content: new[] { "afile" });

            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0", content: new[] { "Bfile" });
            IPackage packageC10 = PackageUtility.CreatePackage("C", "1.0", content: new[] { "Cfile" });

            // G 1.0 -> [C 1.0]
            IPackage packageG10 = PackageUtility.CreatePackage("G", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("C", "[1.0]")
                                                                    }, content: new[] { "gfile" });

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            projectManager.LocalRepository.AddPackage(packageC10);
            projectManager.LocalRepository.AddPackage(packageG10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageG10);

            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0]"),
                                                                        PackageDependency.CreateDependency("C", "[2.0]"),
                                                                        PackageDependency.CreateDependency("D", "[1.0]")
                                                                    }, content: new[] { "a20file" });

            IPackage packageC20 = PackageUtility.CreatePackage("C", "2.0", content: new[] { "cfile" });
            IPackage packageD10 = PackageUtility.CreatePackage("D", "1.0", content: new[] { "dfile" });

            // A 2.0 -> [B 1.0, C 2.0, D 1.0]
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageC20);
            sourceRepository.AddPackage(packageD10);

            // Act 
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.UpdatePackageReference("A"), "Updating 'C 1.0' to 'C 2.0' failed. Unable to find a version of 'G' that is compatible with 'C 2.0'.");
        }

        [Fact]
        public void UpdatePackageReferenceFromRepositorySuccesfullyUpdatesDependentsIfDependentsAreResolvable()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0]")
                                                                    }, content: new[] { "afile" });

            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0, 3.0]")
                                                                    }, content: new[] { "a2file" });

            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0", content: new[] { "bfile" });
            IPackage packageB20 = PackageUtility.CreatePackage("B", "2.0", content: new[] { "b2file" });
            IPackage packageB30 = PackageUtility.CreatePackage("B", "3.0", content: new[] { "b3file" });
            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageB20);
            sourceRepository.AddPackage(packageB30);

            // Act
            projectManager.UpdatePackageReference("B");

            // Assert
            Assert.False(projectManager.LocalRepository.Exists(packageA10));
            Assert.False(projectManager.LocalRepository.Exists(packageB10));
            Assert.True(projectManager.LocalRepository.Exists(packageA20));
            Assert.True(projectManager.LocalRepository.Exists(packageB30));
        }

        [Fact]
        public void UpdatePackageReferenceFromRepositoryFailsIfPackageHasUnresolvableDependents()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            // A -> B 1.0
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0]")
                                                                    }, content: new[] { "afile" });
            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0", content: new[] { "bfile" });
            IPackage packageB20 = PackageUtility.CreatePackage("B", "2.0", content: new[] { "cfile" });
            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageB20);

            // Act & Assert            
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.UpdatePackageReference("B"), "Updating 'B 1.0' to 'B 2.0' failed. Unable to find a version of 'A' that is compatible with 'B 2.0'.");
        }

        [Fact]
        public void UpdatePackageReferenceFromRepositoryFailsIfPackageHasAnyUnresolvableDependents()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            // A 1.0 -> B 1.0
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0]")
                                                                    }, content: new[] { "afile" });

            // A 2.0 -> B [2.0]
            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[2.0]")
                                                                    }, content: new[] { "afile" });

            // B 1.0
            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0", content: new[] { "bfile" });
            // B 2.0
            IPackage packageB20 = PackageUtility.CreatePackage("B", "2.0", content: new[] { "cfile" });
            // C 1.0 -> B [1.0]
            IPackage packageC10 = PackageUtility.CreatePackage("C", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0]")
                                                                    }, content: new[] { "bfile" });

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            projectManager.LocalRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageB20);
            sourceRepository.AddPackage(packageC10);

            // Act & Assert            
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.UpdatePackageReference("B"), "Updating 'B 1.0' to 'B 2.0' failed. Unable to find a version of 'C' that is compatible with 'B 2.0'.");
        }

        [Fact]
        public void UpdatePackageReferenceFromRepositoryOverlappingDependencies()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            // A 1.0 -> B 1.0
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0]")
                                                                    }, content: new[] { "afile" });

            // A 2.0 -> B [2.0]
            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[2.0]")
                                                                    }, content: new[] { "afile" });

            // B 1.0
            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0", content: new[] { "b1file" });

            // B 2.0 -> C 2.0
            IPackage packageB20 = PackageUtility.CreatePackage("B", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("C", "2.0")
                                                                    }, content: new[] { "afile" });

            // C 2.0
            IPackage packageC20 = PackageUtility.CreatePackage("C", "2.0", content: new[] { "c2file" });

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageB20);
            sourceRepository.AddPackage(packageC20);

            // Act
            projectManager.UpdatePackageReference("B");

            // Assert
            Assert.False(projectManager.LocalRepository.Exists(packageA10));
            Assert.False(projectManager.LocalRepository.Exists(packageB10));
            Assert.True(projectManager.LocalRepository.Exists(packageA20));
            Assert.True(projectManager.LocalRepository.Exists(packageB20));
            Assert.True(projectManager.LocalRepository.Exists(packageC20));
        }


        [Fact]
        public void UpdatePackageReferenceFromRepositoryChainedIncompatibleDependents()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            // A 1.0 -> B [1.0]
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0]")
                                                                    }, content: new[] { "afile" });
            // B 1.0 -> C [1.0]
            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("C", "[1.0]")
                                                                    }, content: new[] { "bfile" });
            // C 1.0
            IPackage packageC10 = PackageUtility.CreatePackage("C", "1.0", content: new[] { "c1file" });

            // A 2.0 -> B [1.0, 2.0)
            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0, 2.0)")
                                                                    }, content: new[] { "afile" });

            // B 2.0 -> C [2.0]
            IPackage packageB20 = PackageUtility.CreatePackage("B", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("C", "[2.0]")
                                                                    }, content: new[] { "cfile" });

            // C 2.0
            IPackage packageC20 = PackageUtility.CreatePackage("C", "2.0", content: new[] { "c2file" });

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            projectManager.LocalRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageB20);
            sourceRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageC20);

            // Act & Assert            
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.UpdatePackageReference("C"), "Updating 'C 1.0' to 'C 2.0' failed. Unable to find a version of 'B' that is compatible with 'C 2.0'.");
        }

        [Fact]
        public void UpdatePackageReferenceNoVersionSpecifiedShouldUpdateToLatest()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage package10 = PackageUtility.CreatePackage("NetFramework", "1.0", content: new[] { "1.0f" });
            projectManager.LocalRepository.AddPackage(package10);
            sourceRepository.AddPackage(package10);

            IPackage package11 = PackageUtility.CreatePackage("NetFramework", "1.1", content: new[] { "1.1f" });
            sourceRepository.AddPackage(package11);

            IPackage package20 = PackageUtility.CreatePackage("NetFramework", "2.0", content: new[] { "2.0f" });
            sourceRepository.AddPackage(package20);

            IPackage package35 = PackageUtility.CreatePackage("NetFramework", "3.5", content: new[] { "3.5f" });
            sourceRepository.AddPackage(package35);

            // Act
            projectManager.UpdatePackageReference("NetFramework");

            // Assert
            Assert.False(projectManager.LocalRepository.Exists(package10));
            Assert.True(projectManager.LocalRepository.Exists(package35));
        }

        [Fact]
        public void UpdatePackageReferenceVersionSpeciedShouldUpdateToSpecifiedVersion()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            var package10 = PackageUtility.CreatePackage("NetFramework", "1.0", new[] { "file.dll" });
            projectManager.LocalRepository.AddPackage(package10);
            sourceRepository.AddPackage(package10);

            var package11 = PackageUtility.CreatePackage("NetFramework", "1.1", new[] { "file.dll" });
            sourceRepository.AddPackage(package11);

            var package20 = PackageUtility.CreatePackage("NetFramework", "2.0", new[] { "file.dll" });
            sourceRepository.AddPackage(package20);

            // Act
            projectManager.UpdatePackageReference("NetFramework", new SemanticVersion("1.1"));

            // Assert
            Assert.False(projectManager.LocalRepository.Exists(package10));
            Assert.True(projectManager.LocalRepository.Exists(package11));
        }

        [Fact]
        public void RemovingPackageReferenceRemovesPackageButNotDependencies()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                new PackageDependency("B")
                                                            }, content: new[] { "A" });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0", content: new[] { "B" });

            projectManager.LocalRepository.AddPackage(packageA);
            projectManager.LocalRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);

            // Act
            projectManager.RemovePackageReference("A");

            // Assert            
            Assert.False(projectManager.LocalRepository.Exists(packageA));
            Assert.True(projectManager.LocalRepository.Exists(packageB));
        }

        [Fact]
        public void RemovePackageReferenceOnlyRemovedAssembliesFromTheTargetFramework()
        {
            // Arrange
            var net20 = new FrameworkName(".NETFramework", new Version("2.0"));
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(net20);
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            IPackageAssemblyReference net20Reference = PackageUtility.CreateAssemblyReference("foo.dll", net20);
            IPackageAssemblyReference net40Reference = PackageUtility.CreateAssemblyReference("bar.dll", new FrameworkName(".NETFramework", new Version("4.0")));

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                content: null,
                assemblyReferences: new[] { net20Reference, net40Reference },
                tools: null,
                dependencies: null,
                downloadCount: 0,
                description: null,
                summary: null);

            projectManager.LocalRepository.AddPackage(packageA);

            sourceRepository.AddPackage(packageA);
            projectManager.AddPackageReference("A");

            // Act
            projectManager.RemovePackageReference("A");


            // Assert
            Assert.False(projectManager.LocalRepository.Exists(packageA));
            Assert.Equal(1, projectSystem.Deleted.Count);
            Assert.True(projectSystem.Deleted.Contains("foo.dll"));
        }

        [Fact]
        public void ReAddingAPackageReferenceAfterRemovingADependencyShouldReReferenceAllDependencies()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                dependencies: new List<PackageDependency> {
                    new PackageDependency("B")
                },
                content: new[] { "foo" });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                new PackageDependency("C")
                                                            },
                                                            content: new[] { "bar" });

            var packageC = PackageUtility.CreatePackage("C", "1.0", content: new[] { "baz" });

            projectManager.LocalRepository.AddPackage(packageA);
            projectManager.LocalRepository.AddPackage(packageB);

            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageC);

            // Act
            projectManager.AddPackageReference("A");

            // Assert            
            Assert.True(projectManager.LocalRepository.Exists(packageA));
            Assert.True(projectManager.LocalRepository.Exists(packageB));
            Assert.True(projectManager.LocalRepository.Exists(packageC));
        }

        [Fact]
        public void AddPackageReferenceWithAnyNonCompatibleReferenceThrowsAndPackageIsNotReferenced()
        {
            // Arrange
            var mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(mockProjectSystem.Object), mockProjectSystem.Object, localRepository);
            mockProjectSystem.Setup(m => m.TargetFramework).Returns(new FrameworkName(".NETFramework", new Version("2.0")));
            var mockPackage = new Mock<IPackage>();
            mockPackage.Setup(m => m.Id).Returns("A");
            mockPackage.Setup(m => m.Version).Returns(new SemanticVersion("1.0"));
            var assemblyReference = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("5.0")));
            mockPackage.Setup(m => m.AssemblyReferences).Returns(new[] { assemblyReference });
            sourceRepository.AddPackage(mockPackage.Object);

            // Act & Assert            
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.AddPackageReference("A"), "Could not install package 'A 1.0'. You are trying to install this package into a project that targets '.NETFramework,Version=v2.0', but the package does not contain any assembly references that are compatible with that framework. For more information, contact the package author.");
            Assert.False(localRepository.Exists(mockPackage.Object));
        }

        [Fact]
        public void AddPackageReferenceWithAnyNonCompatibleFrameworkReferenceDoesNotThrow()
        {
            // Arrange
            var mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(mockProjectSystem.Object), mockProjectSystem.Object, localRepository);
            mockProjectSystem.Setup(m => m.TargetFramework).Returns(VersionUtility.ParseFrameworkName("net20"));
            var mockPackage = new Mock<IPackage>();
            mockPackage.Setup(m => m.Id).Returns("A");
            mockPackage.Setup(m => m.Version).Returns(new SemanticVersion("1.0"));
            var frameworkReference = new FrameworkAssemblyReference("System.Web", new[] { VersionUtility.ParseFrameworkName("net50") });
            mockPackage.Setup(m => m.FrameworkAssemblies).Returns(new[] { frameworkReference });
            sourceRepository.AddPackage(mockPackage.Object);

            // Act & Assert            
            projectManager.AddPackageReference("A");
            Assert.True(localRepository.Exists(mockPackage.Object));
        }

        private ProjectManager CreateProjectManager()
        {
            var projectSystem = new MockProjectSystem();
            return new ProjectManager(new MockPackageRepository(), new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
        }
    }
}
