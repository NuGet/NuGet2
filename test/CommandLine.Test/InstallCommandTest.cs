using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Moq;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Test.Mocks;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test.NuGetCommandLine.Commands
{
    public class InstallCommandTest : IDisposable
    {
        private static readonly string _environmentVariableValue = Environment.GetEnvironmentVariable("EnableNuGetPackageRestore");

        public InstallCommandTest()
        {
            Environment.SetEnvironmentVariable("EnableNuGetPackageRestore", "", EnvironmentVariableTarget.Process);
        }

        [Fact]
        public void InstallCommandInstallsPackageIfArgumentIsNotPackageReferenceFile()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem);
            installCommand.Arguments.Add("Foo");

            // Act
            installCommand.ExecuteCommand();

            // Assert
            Assert.Equal(@"Foo.1.0\Foo.1.0.nupkg", fileSystem.Paths.Single().Key);
        }

        [Fact]
        public void InstallCommandUsesCurrentDirectoryAsInstallPathIfNothingSpecified()
        {
            // Arrange
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider());

            // Act
            string installPath = installCommand.ResolveInstallPath();

            // Assert
            Assert.Equal(Directory.GetCurrentDirectory(), installPath);
        }

        [Fact]
        public void InstallCommandUsesOutputDirectoryAsInstallPathIfSpecified()
        {
            // Arrange
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider()) { OutputDirectory = @"Bar\Baz" };

            // Act
            string installPath = installCommand.ResolveInstallPath();

            // Assert
            Assert.Equal(@"Bar\Baz", installPath);
        }

        [Theory]
        [InlineData(@"x:\solution-dir")]
        [InlineData(@"x:\solution-dir\")]
        public void InstallCommandUsesPathFromSolutionRepositorySettings(string solutionDirectory)
        {
            // Arrange
            string expectedPath = @"x:\working-dir\packages\";
            var installCommand = new Mock<TestInstallCommand>(GetFactory(), GetSourceProvider(), null, null, null, null, true, Mock.Of<ISettings>()) { CallBase = true };
            installCommand.Setup(s => s.CreateFileSystem(@"x:\solution-dir\.nuget"))
                          .Returns<string>(_ => GetFileSystemWithDefaultConfig(expectedPath, repositoryRoot: @"x:\solution-dir\.nuget"))
                          .Verifiable();
            installCommand.Object.SolutionDirectory = solutionDirectory;

            // Act
            string installPath = installCommand.Object.ResolveInstallPath();

            // Assert
            Assert.Equal(expectedPath, installPath);
            installCommand.Verify();
        }

        [Fact]
        public void InstallCommandUsesPathFromConfigInSolutionRoot()
        {
            // Arrange
            string expectedPath = @"x:\working-dir\packages\";
            var installCommand = new Mock<TestInstallCommand>(GetFactory(), GetSourceProvider(), null, null, null, null, true, Mock.Of<ISettings>()) { CallBase = true };
            installCommand.Setup(s => s.CreateFileSystem(@"x:\solution-dir\.nuget"))
                .Returns<string>(_ => GetFileSystemWithDefaultConfig(expectedPath, settingsFilePath: @"x:\solution-dir\nuget.config", repositoryRoot: @"x:\solution-dir\.nuget"))
                          .Verifiable();
            installCommand.Object.SolutionDirectory = @"x:\solution-dir";

            // Act
            string installPath = installCommand.Object.ResolveInstallPath();

            // Assert
            Assert.Equal(expectedPath, installPath);
            installCommand.Verify();
        }

        [Fact]
        public void InstallCommandUsesRepositoryPathFromConfigIfSpecified()
        {
            // Arrange
            var fileSystem = GetFileSystemWithDefaultConfig();
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem,
                settings: Settings.LoadDefaultSettings(fileSystem, null, null));

            // Act
            string installPath = installCommand.ResolveInstallPath();

            // Assert
            Assert.Equal(@"C:\This\Is\My\Install\Path", installPath);
        }

        [Fact]
        public void InstallCommandUsesCredentialsFromSolutionDotNuGetConfigFileIfSpecified()
        {
            // Arrange
            var fileSystem = GetFileSystemWithConfigWithCredential();
            var installCommand = new Mock<TestInstallCommand>(
                GetFactory(), GetSourceProvider(), fileSystem, null, null, null, null, 
                Settings.LoadDefaultSettings(fileSystem, null, null))
                {
                    CallBase = true
                };
            installCommand.Object.Console = new MockConsole();
            installCommand.Object.SolutionDirectory = @"C:\My\Solution";
            installCommand.Setup(c => c.CreateFileSystem(@"C:\My\Solution")).Returns(fileSystem);

            // Act
            installCommand.Object.ResolveInstallPath();

            // Assert
            var enabledPackageSource = installCommand
                .Object
                .SourceProvider
                .GetEnabledPackageSources()
                .SingleOrDefault(src => src.Name.Equals("__ATESTREPO__"));
            Assert.NotNull(enabledPackageSource);
            Assert.Equal("user", enabledPackageSource.UserName);
            Assert.Equal("123abc", enabledPackageSource.Password);
            var creds = HttpClient.DefaultCredentialProvider.GetCredentials(
                uri: new Uri("http://www.myprivatefeed.example/"),
                proxy: null,
                credentialType: CredentialType.RequestCredentials, 
                retrying: false).GetCredential(null, null);
            Assert.NotNull(creds);
            Assert.Equal("user", creds.UserName);
            Assert.Equal("123abc", creds.Password);
        }

        [Fact]
        public void InstallCommandOutPathTakesPrecedenceOverRepositoryPath()
        {
            // Arrange
            var fileSystem = GetFileSystemWithDefaultConfig();
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem,
                                                        settings: Settings.LoadDefaultSettings(fileSystem, null, null))
                                     {
                                         OutputDirectory = @"Bar\Baz"
                                     };

            // Act
            string installPath = installCommand.ResolveInstallPath();

            // Assert
            Assert.Equal(@"Bar\Baz", installPath);
        }

        [Theory]
        [InlineData(@"under_dot_nuget\other_dir", @"C:\MockFileSystem\under_dot_nuget\other_dir")]
        [InlineData(@"..\..\packages", @"C:\MockFileSystem\..\..\packages")]
        public void InstallCommandCanUsePathsRelativeToConfigFile(string input, string expected)
        {
            // Arrange
            var fileSystem = GetFileSystemWithDefaultConfig(input);
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem,
                settings: Settings.LoadDefaultSettings(fileSystem, null, null));

            // Act
            string installPath = installCommand.ResolveInstallPath();

            // Assert
            Assert.Equal(expected, installPath);
        }

        [Fact]
        public void InstallCommandUsesInstallOperationIfArgumentIsNotPackageReferenceFile()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var mockRepo = new MockPackageRepository() { PackageUtility.CreatePackage("Foo") };
            var mockFactory = new Mock<IPackageRepositoryFactory>();
            mockFactory.Setup(r => r.CreateRepository(It.IsAny<string>())).Returns(mockRepo);
            var installCommand = new TestInstallCommand(mockFactory.Object, GetSourceProvider(), fileSystem);
            installCommand.Arguments.Add("Foo");

            // Act
            installCommand.ExecuteCommand();

            // Assert
            Assert.Equal(RepositoryOperationNames.Install, mockRepo.LastOperation);
        }

        [Fact]
        public void InstallCommandForPackageReferenceFileDoesNotThrowIfThereIsNoPackageToInstall()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"x:\test\packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
</packages>".AsStream());

            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem, allowPackageRestore: false);
            installCommand.Arguments.Add(@"x:\test\packages.config");

            // Act & Assert
            installCommand.ExecuteCommand();
        }

        public void InstallCommandForPackageReferenceFileThrowIfThereIsPackageToInstallAndConsentIsNotGranted()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"x:\test\packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""Foo"" version=""1.0"" />
  <package id=""Baz"" version=""0.7"" />
</packages>");

            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem, allowPackageRestore: false);
            installCommand.Arguments.Add(@"x:\test\packages.config");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => installCommand.ExecuteCommand());
            Assert.Equal(1, fileSystem.Paths.Count);
            Assert.Equal(@"x:\test\packages.config", fileSystem.Paths.ElementAt(0).Key);
        }

        [Fact]
        public void InstallCommandInstallsPackageSuccessfullyIfCacheRepositoryIsNotSet()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem);
            installCommand.Arguments.Add("Foo");

            // Act
            installCommand.ExecuteCommand();

            // Assert
            Assert.Equal(@"Foo.1.0\Foo.1.0.nupkg", fileSystem.Paths.Single().Key);
        }

        [Fact]
        public void InstallCommandResolvesSourceName()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem);
            installCommand.Arguments.Add("Foo");
            installCommand.Source.Add("Some source name");

            // Act
            installCommand.ExecuteCommand();

            // Assert
            Assert.Equal(@"Foo.1.0\Foo.1.0.nupkg", fileSystem.Paths.Single().Key);
        }

        [Fact]
        public void InstallCommandLogsWarningsForFailingRepositoriesIfNoSourcesAreSpecified()
        {
            // Arrange
            MessageLevel? level = null;
            string message = null;
            var repositoryA = new MockPackageRepository();
            repositoryA.AddPackage(PackageUtility.CreatePackage("Foo"));
            var repositoryB = new Mock<IPackageRepository>();
            repositoryB.Setup(c => c.GetPackages()).Returns(GetPackagesWithException().AsQueryable());
            var fileSystem = new MockFileSystem();
            var console = new Mock<IConsole>();
            console.Setup(c => c.Log(It.IsAny<MessageLevel>(), It.IsAny<string>(), It.IsAny<object[]>())).Callback((MessageLevel a, string b, object[] c) =>
            {
                if (a == MessageLevel.Warning)
                {
                    level = a;
                    message = b;
                }
            });

            var sourceProvider = GetSourceProvider(new[] { new PackageSource("A"), new PackageSource("B") });
            var factory = new Mock<IPackageRepositoryFactory>();
            factory.Setup(c => c.CreateRepository("A")).Returns(repositoryA);
            factory.Setup(c => c.CreateRepository("B")).Returns(repositoryB.Object);
            var installCommand = new TestInstallCommand(factory.Object, sourceProvider, fileSystem)
            {
                Console = console.Object
            };
            installCommand.Arguments.Add("Foo");

            // Act
            installCommand.ExecuteCommand();

            // Assert
            Assert.Equal("Boom", message);
            Assert.Equal(MessageLevel.Warning, level.Value);
        }

        [Fact]
        public void InstallCommandInstallsPackageFromAllSourcesIfArgumentIsNotPackageReferenceFile()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem);
            installCommand.Arguments.Add("Foo");

            // Act
            installCommand.ExecuteCommand();

            installCommand.Arguments.Clear();
            installCommand.Arguments.Add("Bar");
            installCommand.Version = "0.5";

            installCommand.ExecuteCommand();

            // Assert
            Assert.Equal(@"Foo.1.0\Foo.1.0.nupkg", fileSystem.Paths.First().Key);
            Assert.Equal(@"Bar.0.5\Bar.0.5.nupkg", fileSystem.Paths.Last().Key);
        }

        [Fact(Skip="Bug in Moq makes this test flaky")]
        public void InstallCommandInstallsAllPackagesFromConfigFileIfSpecifiedAsArgument()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"x:\test\packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""Foo"" version=""1.0"" />
  <package id=""Baz"" version=""0.7"" />
</packages>");
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem);
            installCommand.Arguments.Add(@"x:\test\packages.config");

            // Actt
            installCommand.ExecuteCommand();

            // Assert
            Assert.Equal(3, fileSystem.Paths.Count);
            Assert.Equal(@"x:\test\packages.config", fileSystem.Paths.ElementAt(0).Key);
            Assert.Contains(@"Foo.1.0\Foo.1.0.nupkg", fileSystem.Paths.Keys);
            Assert.Contains(@"Baz.0.7\Baz.0.7.nupkg", fileSystem.Paths.Keys);
        }

        [Fact]
        public void InstallCommandUsesRestoreOperationIfArgumentIsPackageReferenceFile()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"x:\test\packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""Foo"" version=""1.0"" />
</packages>");
            var mockRepo = new MockPackageRepository() { PackageUtility.CreatePackage("Foo") };
            var mockFactory = new Mock<IPackageRepositoryFactory>();
            mockFactory.Setup(r => r.CreateRepository(It.IsAny<string>())).Returns(mockRepo);
            var installCommand = new TestInstallCommand(mockFactory.Object, GetSourceProvider(), fileSystem);
            installCommand.Arguments.Add(@"x:\test\packages.config");

            // Act
            installCommand.ExecuteCommand();

            // Assert
            Assert.Equal(RepositoryOperationNames.Restore, mockRepo.LastOperation);
        }

        [Fact(Skip = "Bug in mock")]
        public void InstallCommandInstallsAllPackagesUsePackagesConfigByDefaultIfNoArgumentIsSpecified()
        {
            // Arrange
            var fileSystem = new MockFileSystem("x:\\test");
            fileSystem.AddFile(@"packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""Foo"" version=""1.0"" />
  <package id=""Baz"" version=""0.7"" />
</packages>");
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem);

            // Act
            installCommand.ExecuteCommand();

            // Assert
            Assert.Equal(3, fileSystem.Paths.Count);
            Assert.Equal(@"packages.config", fileSystem.Paths.ElementAt(0).Key);
            Assert.Contains(@"Foo.1.0\Foo.1.0.nupkg", fileSystem.Paths.Keys);
            Assert.Contains(@"Baz.0.7\Baz.0.7.nupkg", fileSystem.Paths.Keys);
        }

        [Fact]
        public void InstallCommandThrowsIfConfigFileDoesNotExist()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem);
            installCommand.Arguments.Add(@"x:\test\packages.config");

            // Act and Assert
            ExceptionAssert.Throws<FileNotFoundException>(() => installCommand.ExecuteCommand(), @"x:\test\packages.config not found.");
        }

        [Fact]
        public void InstallCommandUsesMultipleSourcesIfSpecified()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem);
            installCommand.Arguments.Add("Baz");
            installCommand.Source.Add("Some Source name");
            installCommand.Source.Add("Some other Source");

            // Act
            installCommand.ExecuteCommand();

            // Assert
            Assert.Equal(@"Baz.0.7\Baz.0.7.nupkg", fileSystem.Paths.Single().Key);
        }

        // Test that when installing a specific version, if NoCache is false, then 
        // local cache will be used to locate the package.
        [Fact]
        public void InstallCommandUsesLocalCacheIfNoCacheIsFalse()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var localCache = new Mock<IPackageRepository>(MockBehavior.Strict);
            localCache.Setup(c => c.GetPackages()).Returns(new[] { PackageUtility.CreatePackage("Gamma") }.AsQueryable()).Verifiable();
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem, machineCacheRepository: localCache.Object)
            {
                NoCache = false,
                Version = "1.0"
            };
            installCommand.Arguments.Add("Gamma");
            installCommand.Source.Add("Some Source name");
            installCommand.Source.Add("Some other Source");

            // Act
            installCommand.ExecuteCommand();

            // Assert
            Assert.Equal(@"Gamma.1.0\Gamma.1.0.nupkg", fileSystem.Paths.Single().Key);
            localCache.Verify();
        }

        // Test that if version is not specified, then local cache will not be used 
        // even if NoCache if false.
        [Fact]
        public void InstallCommandNotUseLocalCacheIfVersionNotSpecified()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var localCache = new Mock<IPackageRepository>(MockBehavior.Strict);
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem, machineCacheRepository: localCache.Object)
            {
                NoCache = false
            };
            installCommand.Arguments.Add("Baz");
            installCommand.Source.Add("Some Source name");
            installCommand.Source.Add("Some other Source");

            // Act
            installCommand.ExecuteCommand();

            // Assert
            Assert.Equal(@"Baz.0.7\Baz.0.7.nupkg", fileSystem.Paths.Single().Key);
            localCache.Verify(c => c.GetPackages(), Times.Never());
        }

        [Fact]
        public void InstallCommandDoesNotUseLocalCacheIfNoCacheIsTrue()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var localCache = new Mock<IPackageRepository>(MockBehavior.Strict);
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem, machineCacheRepository: localCache.Object)
            {
                NoCache = true
            };
            installCommand.Arguments.Add("Baz");
            installCommand.Source.Add("Some Source name");
            installCommand.Source.Add("Some other Source");

            // Act
            installCommand.ExecuteCommand();

            // Assert
            Assert.Equal(@"Baz.0.7\Baz.0.7.nupkg", fileSystem.Paths.Single().Key);
            localCache.Verify(c => c.GetPackages(), Times.Never());
        }

        [Fact]
        public void InstallCommandInstallsPrereleasePackageIfFlagIsSpecified()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem) { Prerelease = true };
            installCommand.Arguments.Add("Baz");
            installCommand.Source.Add("Some Source name");
            installCommand.Source.Add("Some other Source");

            // Act
            installCommand.ExecuteCommand();

            // Assert
            Assert.Equal(@"Baz.0.8.1-alpha\Baz.0.8.1-alpha.nupkg", fileSystem.Paths.Single().Key);
        }

        [Fact]
        public void InstallCommandUpdatesPackageIfAlreadyPresentAndNotUsingSideBySide()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var repository = new MockPackageRepository();

            var packageManager = new PackageManager(GetFactory().CreateRepository("Some source"), new DefaultPackagePathResolver(fileSystem), fileSystem, repository);
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem, packageManager)
                                    {
                                        Console = new MockConsole(),
                                        ExcludeVersion = true,
                                        Version = "0.4",
                                    };
            installCommand.Arguments.Add("Baz");

            // Act - 1
            installCommand.ExecuteCommand();

            // Assert - 1
            Assert.True(repository.Exists("Baz", new SemanticVersion("0.4")));

            // Act - 2
            installCommand.Version = null;
            installCommand.ExecuteCommand();

            // Assert - 2
            Assert.False(repository.Exists("Baz", new SemanticVersion("0.4")));
            Assert.True(repository.Exists("Baz", new SemanticVersion("0.7")));
        }

        [Fact]
        public void InstallCommandUpdatesPackageFromPackagesConfigIfDifferentVersionAlreadyPresentAndNotUsingSideBySide()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"baz\baz.0.4.nupkg");
            var baz40 = PackageUtility.CreatePackage("Baz", "0.4");
            var packages = new List<IPackage> { baz40 };
            var repository = new Mock<LocalPackageRepository>(new DefaultPackagePathResolver(fileSystem, useSideBySidePaths: false), fileSystem) { CallBase = true };
            repository.Setup(c => c.GetPackages()).Returns(packages.AsQueryable());
            repository.Setup(c => c.Exists("Baz", new SemanticVersion("0.4"))).Returns(true);
            repository.Setup(c => c.FindPackagesById("Baz")).Returns(packages);
            repository.Setup(c => c.AddPackage(It.IsAny<IPackage>())).Callback<IPackage>(c => packages.Add(c)).Verifiable();
            repository.Setup(c => c.RemovePackage(It.IsAny<IPackage>())).Callback<IPackage>(c => packages.Remove(c)).Verifiable();

            var packageManager = new PackageManager(GetFactory().CreateRepository("Some source"), new DefaultPackagePathResolver(fileSystem), fileSystem, repository.Object);
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem, packageManager)
                                 {
                                     Console = new MockConsole()
                                 };

            installCommand.ExcludeVersion = true;
            installCommand.Arguments.Add("Baz");

            // Act 
            installCommand.ExecuteCommand();

            // Assert 
            repository.Verify();
            Assert.Equal("Baz", packages.Single().Id);
            Assert.Equal(new SemanticVersion("0.7"), packages.Single().Version);
        }

        // Tests that when
        // - -ExcludeVersion is specified;
        // - The version of the package to install is not specified;
        // - The package in the source repository is not newer than the existing package;
        // Then nuget won't install the package.
        [Theory]
        [InlineData("0.7")]
        [InlineData("1.0")]
        public void InstallCommandNoOps_ExcludingVersionIsTrue_VersionIsEmpty_ExistingPackageIsNewerOrSame(
            string installedVersion)
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"baz\baz.nupkg");
            var baz = PackageUtility.CreatePackage("Baz", installedVersion);
            var packages = new List<IPackage> { baz };
            var repository = new Mock<LocalPackageRepository>(new DefaultPackagePathResolver(fileSystem, useSideBySidePaths: false), fileSystem) { CallBase = true };
            repository.Setup(c => c.GetPackages()).Returns(packages.AsQueryable());
            repository.Setup(c => c.Exists("Baz", new SemanticVersion(installedVersion))).Returns(true);
            repository.Setup(c => c.FindPackagesById("Baz")).Returns(packages);

            var packageManager = new PackageManager(GetFactory().CreateRepository("Some source"), new DefaultPackagePathResolver(fileSystem), fileSystem, repository.Object);
            var console = new MockConsole();
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem, packageManager)
            {
                Console = console,
                ExcludeVersion = true
            };

            installCommand.Arguments.Add("Baz");

            // Act 
            installCommand.ExecuteCommand();

            // Assert 
            repository.Verify();
            Assert.Equal("Package \"Baz\" is already installed." + Environment.NewLine, console.Output);
        }

        [Fact]
        public void InstallCommandNoOpsIfExcludingVersionAndALowerVersionOfThePackageIsAlreadyInstalled()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var packages = new[] { PackageUtility.CreatePackage("A", "0.5") };
            var repository = new Mock<LocalPackageRepository>(new DefaultPackagePathResolver(fileSystem, useSideBySidePaths: false), fileSystem) { CallBase = true };
            repository.Setup(c => c.FindPackagesById("A")).Returns(packages);
            repository.Setup(c => c.AddPackage(It.IsAny<IPackage>())).Throws(new Exception("Method should not be called"));
            repository.Setup(c => c.RemovePackage(It.IsAny<IPackage>())).Throws(new Exception("Method should not be called"));

            var packageManager = new PackageManager(GetFactory().CreateRepository("Some source"), new DefaultPackagePathResolver(fileSystem), fileSystem, repository.Object);
            var console = new MockConsole();
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem, packageManager)
            {
                Console = console,
                ExcludeVersion = true,
                Version = "0.4"
            };
            installCommand.Arguments.Add("A");

            // Act
            installCommand.ExecuteCommand();

            // Assert
            // Ensure packages were not removed.
            Assert.Equal(1, packages.Length);
            Assert.Equal("Package \"A\" is already installed." + Environment.NewLine, console.Output);
        }

        [Fact]
        public void InstallCommandFromConfigIgnoresDependencies()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"X:\test\packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""Foo"" version=""1.0.0"" />
  <package id=""Qux"" version=""2.3.56-beta"" />
</packages>");
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            var packageManager = new Mock<IPackageManager>(MockBehavior.Strict);
            var package1 = PackageUtility.CreatePackage("Foo", "1.0.0");
            var package2 = PackageUtility.CreatePackage("Qux", "2.3.56-beta");
            var repository = new MockPackageRepository { package1, package2 };
            packageManager.Setup(p => p.InstallPackage(package1, true, true, true)).Verifiable();
            packageManager.Setup(p => p.InstallPackage(package2, true, true, true)).Verifiable();
            packageManager.SetupGet(p => p.PathResolver).Returns(pathResolver);
            packageManager.SetupGet(p => p.LocalRepository).Returns(new LocalPackageRepository(pathResolver, fileSystem));
            packageManager.SetupGet(p => p.FileSystem).Returns(fileSystem);
            packageManager.SetupGet(p => p.SourceRepository).Returns(repository);
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(r => r.CreateRepository("My Source")).Returns(repository);
            var packageSourceProvider = Mock.Of<IPackageSourceProvider>();

            // Act
            var installCommand = new TestInstallCommand(repositoryFactory.Object, packageSourceProvider, fileSystem, packageManager.Object);
            installCommand.Arguments.Add(@"X:\test\packages.config");
            installCommand.ExecuteCommand();

            // Assert
            packageManager.Verify();
        }

        [Fact]
        public void InstallCommandFromConfigPerformsQuickCheckForFiles()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"X:\test\packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""Foo"" version=""1.8.0"" />
  <package id=""Qux"" version=""2.3.56-beta"" />
</packages>");
            fileSystem.AddFile("Foo.1.8.nupkg");
            var package = PackageUtility.CreatePackage("Qux", "2.3.56-beta");
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            var packageManager = new Mock<IPackageManager>(MockBehavior.Strict);
            var repository = new MockPackageRepository { package };
            packageManager.Setup(p => p.InstallPackage(package, true, true, true)).Verifiable();
            packageManager.SetupGet(p => p.PathResolver).Returns(pathResolver);
            packageManager.SetupGet(p => p.LocalRepository).Returns(new LocalPackageRepository(pathResolver, fileSystem));
            packageManager.SetupGet(p => p.FileSystem).Returns(fileSystem);
            packageManager.SetupGet(p => p.SourceRepository).Returns(repository);
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(r => r.CreateRepository("My Source")).Returns(repository);
            var packageSourceProvider = Mock.Of<IPackageSourceProvider>();

            // Act
            var installCommand = new TestInstallCommand(repositoryFactory.Object, packageSourceProvider, fileSystem, packageManager.Object)
            {
                Console = new MockConsole()
            };
            installCommand.Arguments.Add(@"X:\test\packages.config");
            installCommand.ExecuteCommand();

            // Assert
            packageManager.Verify();
        }

        [Fact]
        public void InstallCommandDoesNotPromptForConsentIfRequireConsentIsNotSet()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"X:\test\packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""Abc"" version=""1.0.0"" />
</packages>");
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            var packageManager = new Mock<IPackageManager>(MockBehavior.Strict);
            var package = PackageUtility.CreatePackage("Abc");
            var repository = new MockPackageRepository { package };
            packageManager.SetupGet(p => p.PathResolver).Returns(pathResolver);
            packageManager.SetupGet(p => p.LocalRepository).Returns(new LocalPackageRepository(pathResolver, fileSystem));
            packageManager.SetupGet(p => p.FileSystem).Returns(fileSystem);
            packageManager.SetupGet(p => p.SourceRepository).Returns(repository);
            packageManager.Setup(p => p.InstallPackage(package, true, true, true)).Verifiable();
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(r => r.CreateRepository("My Source")).Returns(repository);
            var packageSourceProvider = Mock.Of<IPackageSourceProvider>();
            var console = new MockConsole();

            var installCommand = new TestInstallCommand(repositoryFactory.Object, packageSourceProvider, fileSystem, packageManager.Object, allowPackageRestore: false);
            installCommand.Arguments.Add(@"X:\test\packages.config");
            installCommand.Console = console;

            // Act
            installCommand.ExecuteCommand();

            // Assert
            packageManager.Verify();
        }

        [Fact]
        public void InstallCommandPromptsForConsentIfRequireConsentIsSet()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"X:\test\packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""Abc"" version=""1.0.0"" />
</packages>");
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            var packageManager = new Mock<IPackageManager>(MockBehavior.Strict);
            var repository = new MockPackageRepository { PackageUtility.CreatePackage("Abc") };
            packageManager.SetupGet(p => p.PathResolver).Returns(pathResolver);
            packageManager.SetupGet(p => p.LocalRepository).Returns(new LocalPackageRepository(pathResolver, fileSystem));
            packageManager.SetupGet(p => p.FileSystem).Returns(fileSystem);
            packageManager.SetupGet(p => p.SourceRepository).Returns(repository);
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(r => r.CreateRepository("My Source")).Returns(repository);
            var packageSourceProvider = Mock.Of<IPackageSourceProvider>();
            var console = new MockConsole();

            var installCommand = new TestInstallCommand(repositoryFactory.Object, packageSourceProvider, fileSystem, packageManager.Object, allowPackageRestore: false);
            installCommand.Arguments.Add(@"X:\test\packages.config");
            installCommand.Console = console;
            installCommand.RequireConsent = true;

            // Act 
            var exception = Assert.Throws<AggregateException>(() => installCommand.ExecuteCommand());

            // Assert
#pragma warning disable 0219
           // mono compiler complains that innerException is assigned but not used.
            var innerException = Assert.IsAssignableFrom<InvalidOperationException>(exception.InnerException);           
#pragma warning restore 0219

            // The culture can't be forced to en-US as the error message is generated in another thread.
            // Hence, only check the error message if the language is english.
            var culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (culture == "en" || culture == "iv") // english or invariant
            {
                string message = string.Format(
                    CultureInfo.CurrentCulture,
                    NuGetResources.InstallCommandPackageRestoreConsentNotFound,
                    NuGet.Resources.NuGetResources.PackageRestoreConsentCheckBoxText.Replace("&", ""));
                Assert.Equal(message, exception.InnerException.Message);
            }
        }

        [Fact]
        public void InstallCommandInstallsSatellitePackagesAfterCorePackages()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"X:\test\packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
    <package id=""Foo.Fr"" version=""1.0.0"" />  
    <package id=""Foo"" version=""1.0.0"" />
</packages>");
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            var packageManager = new Mock<IPackageManager>(MockBehavior.Strict);
            var package1 = PackageUtility.CreatePackage("Foo", "1.0.0");
            var package2 = PackageUtility.CreatePackage("Foo.Fr", "1.0.0", language: "fr",
                dependencies: new[] { new PackageDependency("Foo", VersionUtility.ParseVersionSpec("[1.0.0]")) });
            var repository = new MockPackageRepository { package1, package2 };
            // We *shouldn't* be testing if a sequence of operations worked rather that the outcome that satellite package was installed correctly, 
            // but doing so requires work with  nice to have a unit test that tests it. 
            bool langPackInstalled = false;
            packageManager.Setup(p => p.InstallPackage(package1, true, true, true)).Callback(() =>
                {
                    if (langPackInstalled)
                    {
                        throw new Exception("Lang package installed first");
                    }
                }).Verifiable();
            packageManager.Setup(p => p.InstallPackage(package2, true, true)).Callback(() => langPackInstalled = true).Verifiable();
            packageManager.SetupGet(p => p.PathResolver).Returns(pathResolver);
            packageManager.SetupGet(p => p.LocalRepository).Returns(new LocalPackageRepository(pathResolver, fileSystem));
            packageManager.SetupGet(p => p.FileSystem).Returns(fileSystem);
            packageManager.SetupGet(p => p.SourceRepository).Returns(repository);
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(r => r.CreateRepository("My Source")).Returns(repository);
            var packageSourceProvider = Mock.Of<IPackageSourceProvider>();

            // Act
            var installCommand = new TestInstallCommand(repositoryFactory.Object, packageSourceProvider, fileSystem, packageManager.Object);
            installCommand.Arguments.Add(@"X:\test\packages.config");
            installCommand.ExecuteCommand();

            // Assert
            packageManager.Verify();
        }

        [Fact]
        public void InstallCommandUsesRemoteIfCacheHasOldDependencyVersion()
        {
            // Arrange
            var newPackageDependency = PackageUtility.CreatePackage("Foo", "1.1");
            var newPackage = PackageUtility.CreatePackage("Foo.UI", "1.1",
                dependencies: new[] { new PackageDependency("Foo", VersionUtility.ParseVersionSpec("[1.1]")) });
            var repository = new MockPackageRepository { newPackageDependency, newPackage };
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(r => r.CreateRepository("Some source")).Returns(repository);

            var fileSystem = new MockFileSystem();
            var localCache = new Mock<IPackageRepository>(MockBehavior.Strict);
            localCache.Setup(c => c.GetPackages()).Returns(new[] { PackageUtility.CreatePackage("Foo", "1.0") }.AsQueryable()).Verifiable();
            
            var localRepository = new MockPackageRepository();
            var installCommand = new TestInstallCommand(
                repositoryFactory.Object,
                GetSourceProvider(new[] { new PackageSource("Some source", "Some source name") }),
                fileSystem,
                machineCacheRepository: localCache.Object,
                localRepository: localRepository)
            {
                NoCache = false,
                Version = "1.1"
            };
            installCommand.Arguments.Add("Foo.UI");
            installCommand.Source.Add("Some Source name");

            // Act
            installCommand.ExecuteCommand();

            // Assert
            var installedPackages = localRepository.GetPackages().ToList();
            Assert.Equal(2, installedPackages.Count);
            Assert.Same(newPackageDependency, installedPackages[0]);
            Assert.Same(newPackage, installedPackages[1]);
            localCache.Verify();
        }

        private static IPackageRepositoryFactory GetFactory()
        {
            var repositoryA = new MockPackageRepository { PackageUtility.CreatePackage("Foo"), PackageUtility.CreatePackage("Baz", "0.4"), PackageUtility.CreatePackage("Baz", "0.7") };
            var repositoryB = new MockPackageRepository { PackageUtility.CreatePackage("Bar", "0.5"), PackageUtility.CreatePackage("Baz", "0.8.1-alpha") };

            var factory = new Mock<IPackageRepositoryFactory>();
            factory.Setup(c => c.CreateRepository(It.Is<string>(f => f.Equals("Some source")))).Returns(repositoryA);
            factory.Setup(c => c.CreateRepository(It.Is<string>(f => f.Equals("Some other source")))).Returns(repositoryB);

            return factory.Object;
        }

        private static IPackageSourceProvider GetSourceProvider(IEnumerable<PackageSource> sources = null)
        {
            var sourceProvider = new Mock<IPackageSourceProvider>();
            sources = sources ?? new[] { new PackageSource("Some source", "Some source name"), new PackageSource("Some other source") };
            sourceProvider.Setup(c => c.LoadPackageSources()).Returns(sources);

            return sourceProvider.Object;
        }

        private static IEnumerable<IPackage> GetPackagesWithException()
        {
            yield return PackageUtility.CreatePackage("Baz");
            throw new InvalidOperationException("Boom");
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("EnableNuGetPackageRestore", _environmentVariableValue, EnvironmentVariableTarget.Process);
        }

        public class TestInstallCommand : InstallCommand
        {
            private readonly IFileSystem _fileSystem;
            private readonly IPackageManager _packageManager;
            private readonly IPackageRepository _localRepository;

            public TestInstallCommand(IPackageRepositoryFactory factory,
                                      IPackageSourceProvider sourceProvider,
                                      IFileSystem fileSystem = null,
                                      IPackageManager packageManager = null,
                                      IPackageRepository machineCacheRepository = null,
                                      IPackageRepository localRepository = null,
                                      bool allowPackageRestore = true,
                                      ISettings settings = null)
                : base(machineCacheRepository ?? new MockPackageRepository())
            {
                RepositoryFactory = factory;
                SourceProvider = sourceProvider;
                Settings = settings ?? CreateSettings(allowPackageRestore);
                _fileSystem = fileSystem ?? new MockFileSystem();
                _packageManager = packageManager;
                _localRepository = localRepository;
            }

            private static ISettings CreateSettings(bool allowPackageRestore)
            {
                var settings = new Mock<ISettings>();
                settings.Setup(s => s.GetValue("packageRestore", "enabled", false))
                        .Returns(allowPackageRestore.ToString());
                return settings.Object;
            }

            protected internal override IFileSystem CreateFileSystem(string path)
            {
                return _fileSystem;
            }

            protected override IPackageManager CreatePackageManager(IFileSystem fileSystem, bool useSideBySidePaths, bool checkDowngrade)
            {
                if (_packageManager != null)
                {
                    return _packageManager;
                }

                if (_localRepository != null)
                {
                    return new PackageManager(CreateRepository(),
                        new DefaultPackagePathResolver(fileSystem, useSideBySidePaths), fileSystem, _localRepository)
                    {
                        CheckDowngrade = checkDowngrade
                    };
                }

                return base.CreatePackageManager(fileSystem, useSideBySidePaths, checkDowngrade);
            }

            protected override PackageReferenceFile GetPackageReferenceFile(string path)
            {
                return new PackageReferenceFile(_fileSystem, path);
            }
        }

        private static IFileSystem GetFileSystemWithDefaultConfig(string repositoryPath = @"C:\This\Is\My\Install\Path",
                string settingsFilePath = "nuget.config",
                string repositoryRoot = @"C:\MockFileSystem\")
        {
            var fileSystem = new MockFileSystem(repositoryRoot);
            fileSystem.AddFile(settingsFilePath, String.Format(
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <config>
        <add key=""repositorypath"" value=""{0}"" />
    </config>
</configuration>", repositoryPath));
            return fileSystem;
        }

        private static IFileSystem GetFileSystemWithConfigWithCredential()
        {
            var fileSystem = new MockFileSystem(@"C:\This\Is\My\Install\Path\.nuget");
            fileSystem.AddFile("nuget.config",
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <solution>
    <add key=""disableSourceControlIntegration"" value=""true"" />
  </solution>
  <packageSources>
    <add key=""__ATESTREPO__"" value=""http://www.myprivatefeed.example/"" />
  </packageSources>
  <packageSourceCredentials>
      <__ATESTREPO__>
          <add key=""Username"" value=""user"" />
          <add key=""Password"" value=""" + EncryptionUtility.EncryptString("123abc") + @""" />
      </__ATESTREPO__>
  </packageSourceCredentials>
</configuration>");
            return fileSystem;
        }

    }
}
