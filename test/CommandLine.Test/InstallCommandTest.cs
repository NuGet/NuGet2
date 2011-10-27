using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test.NuGetCommandLine.Commands
{
    public class InstallCommandTest
    {
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
        public void InstallCommandInstallsPackageSuccessfullyIfCacheRepositoryIsNotSet()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var packageManager = new Mock<IPackageManager>(MockBehavior.Strict);
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

        [Fact]
        public void InstallCommandInstallsAllPackagesFromConfigFileIfSpecifiedAsArgument()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"dir\packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""Foo"" version=""1.0"" />
  <package id=""Baz"" version=""0.7"" />
</packages>");
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem);
            installCommand.Arguments.Add(@"dir\packages.config");

            // Act
            installCommand.ExecuteCommand();

            // Assert
            Assert.Equal(3, fileSystem.Paths.Count);
            Assert.Equal(@"dir\packages.config", fileSystem.Paths.ElementAt(0).Key);
            Assert.Equal(@"Foo.1.0\Foo.1.0.nupkg", fileSystem.Paths.ElementAt(1).Key);
            Assert.Equal(@"Baz.0.7\Baz.0.7.nupkg", fileSystem.Paths.ElementAt(2).Key);
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
            var packages = new List<IPackage>();
            var repository = new Mock<IPackageRepository>();
            repository.Setup(c => c.GetPackages()).Returns(packages.AsQueryable());
            repository.Setup(c => c.AddPackage(It.IsAny<IPackage>())).Callback<IPackage>(c => packages.Add(c)).Verifiable();
            repository.Setup(c => c.RemovePackage(It.IsAny<IPackage>())).Callback<IPackage>(c => packages.Remove(c)).Verifiable();

            var packageManager = new PackageManager(GetFactory().CreateRepository("Some source"), new DefaultPackagePathResolver(fileSystem), fileSystem, repository.Object, new MockPackageRepository());
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem, packageManager);

            installCommand.Version = "0.4";
            installCommand.ExcludeVersion = true;
            installCommand.Arguments.Add("Baz");

            // Act - 1
            installCommand.ExecuteCommand();

            // Assert - 1
            Assert.Equal("Baz", packages.Single().Id);
            Assert.Equal(new SemanticVersion("0.4"), packages.Single().Version);

            // Act - 2
            installCommand.Version = null;
            installCommand.Execute();

            // Assert - 2
            Assert.Equal("Baz", packages.Single().Id);
            Assert.Equal(new SemanticVersion("0.7"), packages.Single().Version);
            repository.Verify();
        }

        [Fact]
        public void InstallCommandUpdatesPackagesFromPackagesConfigIfAlreadyPresentAndNotUsingSideBySide()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var packages = new List<IPackage> { PackageUtility.CreatePackage("Baz", "0.4") };
            var repository = new Mock<IPackageRepository>();
            repository.Setup(c => c.GetPackages()).Returns(packages.AsQueryable());
            repository.Setup(c => c.AddPackage(It.IsAny<IPackage>())).Callback<IPackage>(c => packages.Add(c)).Verifiable();
            repository.Setup(c => c.RemovePackage(It.IsAny<IPackage>())).Callback<IPackage>(c => packages.Remove(c)).Verifiable();

            var packageManager = new PackageManager(GetFactory().CreateRepository("Some source"), new DefaultPackagePathResolver(fileSystem), fileSystem, repository.Object,
                new MockPackageRepository());
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem, packageManager);

            installCommand.ExcludeVersion = true;
            installCommand.Arguments.Add("Baz");

            // Act 
            installCommand.ExecuteCommand();

            // Assert 
            Assert.Equal("Baz", packages.Single().Id);
            Assert.Equal(new SemanticVersion("0.7"), packages.Single().Version);
        }

        [Fact]
        public void InstallCommandWorksIfExcludedVersionsAndPackageIsNotFoundInRemoteRepository()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var packages = new List<IPackage> { PackageUtility.CreatePackage("A", "0.5") };
            var repository = new Mock<IPackageRepository>();
            repository.Setup(c => c.GetPackages()).Returns(packages.AsQueryable());
            repository.Setup(c => c.AddPackage(It.IsAny<IPackage>())).Throws(new Exception("Method should not be called"));
            repository.Setup(c => c.RemovePackage(It.IsAny<IPackage>())).Throws(new Exception("Method should not be called"));

            var packageManager = new PackageManager(GetFactory().CreateRepository("Some source"), new DefaultPackagePathResolver(fileSystem), fileSystem, repository.Object, new MockPackageRepository());
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem, packageManager);

            installCommand.ExcludeVersion = true;
            installCommand.Arguments.Add("A");

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => installCommand.ExecuteCommand(), "Unable to find package 'A'.");
            // Ensure packages were not removed.
            Assert.Equal(1, packages.Count);
        }

        [Fact]
        public void InstallCommandFromConfigIgnoresDependencies()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""Foo"" version=""1.0.0"" />
  <package id=""Qux"" version=""2.3.56-beta"" />
</packages>");

            var packageManager = new Mock<IPackageManager>(MockBehavior.Strict);
            var package = PackageUtility.CreatePackage("Foo", "1.0.0");
            packageManager.Setup(p => p.InstallPackage("Foo", new SemanticVersion("1.0.0"), true, true)).Verifiable();
            packageManager.Setup(p => p.InstallPackage("Qux", new SemanticVersion("2.3.56-beta"), true, true)).Verifiable();
            packageManager.SetupGet(p => p.PathResolver).Returns(new DefaultPackagePathResolver(fileSystem));
            var repository = new MockPackageRepository();
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(r => r.CreateRepository("My Source")).Returns(repository);
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);

            // Act
            var installCommand = new TestInstallCommand(repositoryFactory.Object, packageSourceProvider.Object, fileSystem, packageManager.Object);
            installCommand.Arguments.Add("packages.config");
            installCommand.Execute();

            // Assert
            packageManager.Verify();
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

        private class TestInstallCommand : InstallCommand
        {
            private readonly IFileSystem _fileSystem;
            private readonly IPackageManager _packageManager;

            public TestInstallCommand(IPackageRepositoryFactory factory,
                                      IPackageSourceProvider sourceProvider,
                                      IFileSystem fileSystem,
                                      IPackageManager packageManager = null,
                                      IPackageRepository machineCacheRepository = null)
                : base(factory, sourceProvider)
            {
                _fileSystem = fileSystem;
                _packageManager = packageManager;
                CacheRepository = machineCacheRepository ?? new MockPackageRepository();
            }

            protected override IFileSystem CreateFileSystem()
            {
                return _fileSystem;
            }

            protected override IPackageManager CreatePackageManager(IFileSystem fileSystem)
            {
                return _packageManager ?? base.CreatePackageManager(fileSystem);
            }

            protected override PackageReferenceFile GetPackageReferenceFile(string path)
            {
                return new PackageReferenceFile(_fileSystem, path);
            }
        }
    }
}
