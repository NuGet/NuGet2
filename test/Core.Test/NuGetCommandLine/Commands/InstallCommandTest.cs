using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Test.Mocks;

namespace NuGet.Test.NuGetCommandLine.Commands {
    [TestClass]
    public class InstallCommandTest {
        [TestMethod]
        public void InstallCommandInstallsPackageIfArgumentIsNotPackageReferenceFile() {
            // Arrange
            var fileSystem = new MockFileSystem();
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem);
            installCommand.Arguments = new List<string> { "Foo" };

            // Act
            installCommand.ExecuteCommand();

            // Assert
            Assert.AreEqual(@"Foo.1.0\Foo.1.0.nupkg", fileSystem.Paths.Single().Key);
        }

        [TestMethod]
        public void InstallCommandResolvesSourceName() {
            // Arrange
            var fileSystem = new MockFileSystem();
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem);
            installCommand.Arguments = new List<string> { "Foo" };
            installCommand.Source.Add("Some source name");

            // Act
            installCommand.ExecuteCommand();

            // Assert
            Assert.AreEqual(@"Foo.1.0\Foo.1.0.nupkg", fileSystem.Paths.Single().Key);
        }

        [TestMethod]
        public void InstallCommandLogsWarningsForFailingRepositoriesIfNoSourcesAreSpecified() {
            // Arrange
            MessageLevel? level = null;
            string message = null;
            var repositoryA = new MockPackageRepository();
            repositoryA.AddPackage(PackageUtility.CreatePackage("Foo"));
            var repositoryB = new Mock<IPackageRepository>();
            repositoryB.Setup(c => c.GetPackages()).Returns(GetPackagesWithException().AsQueryable());
            var fileSystem = new MockFileSystem();
            var console = new Mock<IConsole>();
            console.Setup(c => c.Log(It.IsAny<MessageLevel>(), It.IsAny<string>(), It.IsAny<object[]>())).Callback((MessageLevel a, string b, object[] c) => {
                if (a == MessageLevel.Warning) {
                    level = a;
                    message = b;
                }
            });
            
            var sourceProvider = GetSourceProvider(new[] { new PackageSource("A"), new PackageSource("B") });
            var factory = new Mock<IPackageRepositoryFactory>();
            factory.Setup(c => c.CreateRepository("A")).Returns(repositoryA);
            factory.Setup(c => c.CreateRepository("B")).Returns(repositoryB.Object);
            var installCommand = new TestInstallCommand(factory.Object, sourceProvider, fileSystem) {
                Arguments = new List<string> { "Foo" },
                Console = console.Object
            };

            // Act
            installCommand.ExecuteCommand();

            // Assert
            Assert.AreEqual("Boom", message);
            Assert.AreEqual(MessageLevel.Warning, level.Value);
        }

        [TestMethod]
        public void InstallCommandInstallsPackageFromAllSourcesIfArgumentIsNotPackageReferenceFile() {
            // Arrange
            var fileSystem = new MockFileSystem();
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem);
            installCommand.Arguments = new List<string> { "Foo" };

            // Act
            installCommand.ExecuteCommand();

            installCommand.Arguments = new List<string> { "Bar" };
            installCommand.Version = "0.5";

            installCommand.ExecuteCommand();

            // Assert
            Assert.AreEqual(@"Foo.1.0\Foo.1.0.nupkg", fileSystem.Paths.First().Key);
            Assert.AreEqual(@"Bar.0.5\Bar.0.5.nupkg", fileSystem.Paths.Last().Key);
        }

        [TestMethod]
        public void InstallCommandInstallsAllPackagesFromConfigFileIfSpecifiedAsArgument() {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"dir\packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""Foo"" version=""1.0"" />
  <package id=""Baz"" version=""0.7"" />
</packages>");
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem);
            installCommand.Arguments = new List<string> { @"dir\packages.config" };

            // Act
            installCommand.ExecuteCommand();

            // Assert
            Assert.AreEqual(3, fileSystem.Paths.Count);
            Assert.AreEqual(@"dir\packages.config", fileSystem.Paths.ElementAt(0).Key);
            Assert.AreEqual(@"Foo.1.0\Foo.1.0.nupkg", fileSystem.Paths.ElementAt(1).Key);
            Assert.AreEqual(@"Baz.0.7\Baz.0.7.nupkg", fileSystem.Paths.ElementAt(2).Key);
        }

        [TestMethod]
        public void InstallCommandUsesMultipleSourcesIfSpecified() {
            // Arrange
            var fileSystem = new MockFileSystem();
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem);
            installCommand.Arguments = new List<string> { @"Baz" };
            installCommand.Source.Add("Some Source name");
            installCommand.Source.Add("Some other Source");

            // Act
            installCommand.ExecuteCommand();

            // Assert
            Assert.AreEqual(@"Baz.0.7\Baz.0.7.nupkg", fileSystem.Paths.Single().Key);
        }

        [TestMethod]
        public void InstallCommandUpdatesPackageIfAlreadyPresentAndUsingSideBySide() {
            // Arrange
            var fileSystem = new MockFileSystem();
            var packages = new List<IPackage>();
            var repository = new Mock<IPackageRepository>();
            repository.Setup(c => c.GetPackages()).Returns(packages.AsQueryable());
            repository.Setup(c => c.AddPackage(It.IsAny<IPackage>())).Callback<IPackage>(c => packages.Add(c)).Verifiable();
            repository.Setup(c => c.RemovePackage(It.IsAny<IPackage>())).Callback<IPackage>(c => packages.Remove(c)).Verifiable();

            var packageManager = new PackageManager(GetFactory().CreateRepository("Some source"), new DefaultPackagePathResolver(fileSystem), fileSystem, repository.Object);
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem, packageManager);

            installCommand.Version = "0.4";
            installCommand.ExcludeVersion = true;
            installCommand.Arguments = new List<string> { "Baz" };

            // Act - 1
            installCommand.ExecuteCommand();

            // Assert - 1
            Assert.AreEqual("Baz", packages.Single().Id);
            Assert.AreEqual(new Version("0.4"), packages.Single().Version);

            // Act - 2
            installCommand.Version = null;
            installCommand.Execute();

            // Assert - 2
            Assert.AreEqual("Baz", packages.Single().Id);
            Assert.AreEqual(new Version("0.7"), packages.Single().Version);
            repository.Verify();
        }

        [TestMethod]
        public void InstallCommandWorksIfExcludedVersionsAndPackageIsNotFoundInRemoteRepository() {
            // Arrange
            var fileSystem = new MockFileSystem();
            var packages = new List<IPackage> { PackageUtility.CreatePackage("A", "0.5") };
            var repository = new Mock<IPackageRepository>();
            repository.Setup(c => c.GetPackages()).Returns(packages.AsQueryable());
            repository.Setup(c => c.AddPackage(It.IsAny<IPackage>())).Throws(new Exception("Method should not be called"));
            repository.Setup(c => c.RemovePackage(It.IsAny<IPackage>())).Throws(new Exception("Method should not be called"));

            var packageManager = new PackageManager(GetFactory().CreateRepository("Some source"), new DefaultPackagePathResolver(fileSystem), fileSystem, repository.Object);
            var installCommand = new TestInstallCommand(GetFactory(), GetSourceProvider(), fileSystem, packageManager);

            installCommand.ExcludeVersion = true;
            installCommand.Arguments = new List<string> { "A" };

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => installCommand.ExecuteCommand(), "Unable to find package 'A'.");
            // Ensure packages were not removed.
            Assert.AreEqual(1, packages.Count);
        }

        private static IPackageRepositoryFactory GetFactory() {
            var repositoryA = new MockPackageRepository { PackageUtility.CreatePackage("Foo"), PackageUtility.CreatePackage("Baz", "0.4"), PackageUtility.CreatePackage("Baz", "0.7") };
            var repositoryB = new MockPackageRepository { PackageUtility.CreatePackage("Bar", "0.5") };

            var factory = new Mock<IPackageRepositoryFactory>();
            factory.Setup(c => c.CreateRepository(It.Is<string>(f => f.Equals("Some source")))).Returns(repositoryA);
            factory.Setup(c => c.CreateRepository(It.Is<string>(f => f.Equals("Some other source")))).Returns(repositoryB);

            return factory.Object;
        }

        private static IPackageSourceProvider GetSourceProvider(IEnumerable<PackageSource> sources = null) {
            var sourceProvider = new Mock<IPackageSourceProvider>();
            sources = sources ?? new[] { new PackageSource("Some source", "Some source name"), new PackageSource("Some other source") };
            sourceProvider.Setup(c => c.LoadPackageSources()).Returns(sources);

            return sourceProvider.Object;
        }

        private static IEnumerable<IPackage> GetPackagesWithException() {
            yield return PackageUtility.CreatePackage("Baz");
            throw new InvalidOperationException("Boom");
        }

        private class TestInstallCommand : InstallCommand {
            private readonly IFileSystem _fileSystem;
            private readonly PackageManager _packageManager;

            public TestInstallCommand(IPackageRepositoryFactory factory, IPackageSourceProvider sourceProvider, IFileSystem fileSystem, PackageManager packageManager = null)
                : base(factory, sourceProvider) {
                _fileSystem = fileSystem;
                _packageManager = packageManager;
            }

            protected override IFileSystem GetFileSystem() {
                return _fileSystem;
            }

            protected override PackageManager GetPackageManager(IFileSystem fileSystem, bool useMachineCache) {
                return _packageManager ?? base.GetPackageManager(fileSystem, useMachineCache);
            }
        }
    }
}
