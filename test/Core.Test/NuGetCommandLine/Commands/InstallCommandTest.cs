using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Commands;
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

        private static IPackageRepositoryFactory GetFactory() {
            var repositoryA = new MockPackageRepository { PackageUtility.CreatePackage("Foo"), PackageUtility.CreatePackage("Baz", "0.4"), PackageUtility.CreatePackage("Baz", "0.7") };
            var repositoryB = new MockPackageRepository { PackageUtility.CreatePackage("Bar", "0.5") };

            var factory = new Mock<IPackageRepositoryFactory>();
            factory.Setup(c => c.CreateRepository(It.Is<string>(f => f.Equals("Some source")))).Returns(repositoryA);
            factory.Setup(c => c.CreateRepository(It.Is<string>(f => f.Equals("Some other source")))).Returns(repositoryB);

            return factory.Object;
        }

        private static IPackageSourceProvider GetSourceProvider() {
            var sourceProvider = new Mock<IPackageSourceProvider>();
            sourceProvider.Setup(c => c.LoadPackageSources()).Returns(new[] { 
                new PackageSource("Some source", "Some source name"), new PackageSource("Some other source") });

            return sourceProvider.Object;
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

            protected override PackageManager GetPackageManager(IFileSystem fileSystem) {
                return _packageManager ?? base.GetPackageManager(fileSystem);
            }
        }
    }
}
