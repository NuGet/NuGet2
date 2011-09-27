using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using Moq;
using NuGet.MSBuild;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test.MSBuild {
	
    public class NuGetFetchTaskUnitTest {
        private const string createdPackage = "thePackageId.1.0.nupkg";

		[Fact]
        public void WillErrorIfPackagesDotConfigUnreadable() {
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", "foo bar".AsStream());
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(c => c.CreateFileSystem(It.IsAny<string>())).Returns(fileSystem);

            string actualMessage = null;
            var buildEngineStub = new Mock<IBuildEngine>();
            buildEngineStub
                .Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(e => actualMessage = e.Message);

            NuGetFetch task = CreateTaskWithDefaultStubs(buildEngineStub: buildEngineStub, fileSystemProvider: fileSystemProvider);

            var result = task.Execute();

            Assert.False(result);
            Assert.Equal("Error parsing packages.config file at 'packages.config'", actualMessage);
        }

		[Fact]
        public void WillGetSettingsFromSpecifiedConfigFileIfFeedsAreNull() {
            var installed = 0;

            var packageManagerStub = new Mock<IPackageManager>();
            packageManagerStub
                .Setup(x => x.InstallPackage(It.IsAny<string>(), It.IsAny<SemanticVersion>(), true, true))
                .Callback(() => installed++);

            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory
                .Setup(x => x.CreateRepository(It.Is<string>(c => c == "FooSourceValue")))
                .Returns(new MockPackageRepository());

            var fileSystem = GetFileSystem();
            var configFileSystem = new MockFileSystem();
            configFileSystem.AddFile(@"pathToConfigFile", @"<?xml version=""1.0""?><configuration><packageSources>
                                                    <add key=""FooSource"" value=""FooSourceValue"" />
                                                    </packageSources></configuration>");
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(c => c.CreateFileSystem(It.Is<string>(p => p != "config-path"))).Returns(fileSystem);
            fileSystemProvider.Setup(c => c.CreateFileSystem(It.Is<string>(p => p == "config-path"))).Returns(configFileSystem);

            NuGetFetch task = CreateTaskWithDefaultStubs(packageManagerStub: packageManagerStub, repositoryFactory: repositoryFactory, fileSystemProvider: fileSystemProvider);
            task.Sources = null;
            bool actualResult = task.Execute();

            Assert.True(actualResult);
            Assert.Equal(3, installed);
        }

        [Fact]
        public void WillGetSettingsFromSpecifiedConfigFileIfFeedsAreNotSpecified() {
            var installed = 0;

            var packageManagerStub = new Mock<IPackageManager>();
            packageManagerStub
                .Setup(x => x.InstallPackage(It.IsAny<string>(), It.IsAny<SemanticVersion>(), true, true))
                .Callback(() => installed++);

            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory
                .Setup(x => x.CreateRepository(It.IsAny<string>()))
                .Returns(new MockPackageRepository());

            NuGetFetch task = CreateTaskWithDefaultStubs(packageManagerStub: packageManagerStub, repositoryFactory: repositoryFactory);
            task.Sources = new string[] { };

            bool actualResult = task.Execute();

            Assert.Equal(3, installed);
            Assert.True(actualResult);
        }

        [Fact]
        public void WillGetSettingsFromDefaultConfigFileIfFeedsAreNotSpecified() {
            var installed = 0;

            var packageManagerStub = new Mock<IPackageManager>();
            packageManagerStub
                .Setup(x => x.InstallPackage(It.IsAny<string>(), It.IsAny<SemanticVersion>(), true, true))
                .Callback(() => installed++);

            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory
                .Setup(x => x.CreateRepository(It.IsAny<string>()))
                .Returns(new MockPackageRepository());

            NuGetFetch task = CreateTaskWithDefaultStubs(packageManagerStub: packageManagerStub, repositoryFactory: repositoryFactory);
            task.Sources = new string[] { };

            bool actualResult = task.Execute();

            Assert.Equal(3, installed);
            Assert.True(actualResult);
        }

        [Fact]
        public void WillErrorIfPackagesDotConfigNotFound() {
            string actualMessage = null;
            var buildEngineStub = new Mock<IBuildEngine>();
            buildEngineStub.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                           .Callback<BuildErrorEventArgs>(e => actualMessage = e.Message);
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(c => c.CreateFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
            NuGetFetch task = CreateTaskWithDefaultStubs(buildEngineStub: buildEngineStub, fileSystemProvider: fileSystemProvider);
            task.Sources = new string[] { };

            var result = task.Execute();

            Assert.Equal(false, result);
            Assert.Equal("Package configuration file 'packages.config' does not exist", actualMessage);
        }

        [Fact]
        public void WillErrorIfFeedsInvalid() {
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory
                .Setup(x => x.CreateRepository(It.IsAny<string>()))
                .Throws(new InvalidOperationException("Problem with feed"));

            string actualMessage = null;
            var buildEngineStub = new Mock<IBuildEngine>();
            buildEngineStub
                .Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(e => actualMessage = e.Message);

            NuGetFetch task = CreateTaskWithDefaultStubs(repositoryFactory: repositoryFactory, buildEngineStub: buildEngineStub);
            var result = task.Execute();

            Assert.Equal(false, result);
            Assert.Equal("Problem with feed", actualMessage);
        }

        [Fact]
        public void WillErrorGracefullyIfPackageNotFound() {
            const string notFoundMessage = "Package not found";
            var packageManagerStub = new Mock<IPackageManager>();
            packageManagerStub
                .Setup(x => x.InstallPackage(It.IsAny<string>(), It.IsAny<SemanticVersion>(), true, true))
                .Throws(new InvalidOperationException(notFoundMessage)).Verifiable();

            string actualMessage = null;
            var buildEngineStub = new Mock<IBuildEngine>();
            buildEngineStub
                .Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(e => actualMessage = e.Message);

            NuGetFetch task = CreateTaskWithDefaultStubs(packageManagerStub: packageManagerStub, buildEngineStub: buildEngineStub);

            var result = task.Execute();

            Assert.Equal(false, result);
            Assert.Equal(actualMessage, notFoundMessage);
            packageManagerStub.Verify();
        }

        [Fact]
        public void WillInstallPackagesFromPackagesDotConfig() {
            var installed = 0;
            var packageManagerStub = new Mock<IPackageManager>();

            NuGetFetch task = CreateTaskWithDefaultStubs(packageManagerStub: packageManagerStub);

            packageManagerStub
                .Setup(x => x.InstallPackage(It.IsAny<string>(), It.IsAny<SemanticVersion>(), true, true))
                .Callback(() => installed++);

            bool actualResult = task.Execute();

            Assert.Equal(3, installed);
            Assert.True(actualResult);
        }

        [Fact]
        public void WilNotReInstallExistingPackages() {
            var installed = 0;
            var packageManagerStub = new Mock<IPackageManager>();

            var fileSystem = GetFileSystem();
            fileSystem.AddFile(@"packagedir\packagename");
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(c => c.CreateFileSystem(It.IsAny<string>())).Returns(fileSystem);
            NuGetFetch task = CreateTaskWithDefaultStubs(packageManagerStub: packageManagerStub, fileSystemProvider: fileSystemProvider);

            packageManagerStub
                .Setup(x => x.InstallPackage(It.IsAny<string>(), It.IsAny<SemanticVersion>(), true, true))
                .Callback(() => installed++);

            bool actualResult = task.Execute();

            Assert.Equal(0, installed);
            Assert.True(actualResult);
        }

        [Fact]
        public void WillDoNothingIfNoPackagesNeedToBeInstalled() {
            string actualMessage = string.Empty;
            var buildEngineStub = new Mock<IBuildEngine>();
            var installed = 0;
            var packageManagerStub = new Mock<IPackageManager>();
            var fileSystem = GetFileSystem();
            fileSystem.AddFile(@"packagedir\packagename");
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(c => c.CreateFileSystem(It.IsAny<string>())).Returns(fileSystem);
            NuGetFetch task = CreateTaskWithDefaultStubs(buildEngineStub: buildEngineStub, packageManagerStub: packageManagerStub, fileSystemProvider: fileSystemProvider);

            packageManagerStub
                .Setup(x => x.InstallPackage(It.IsAny<string>(), It.IsAny<SemanticVersion>(), true, true))
                .Callback(() => installed++);

            buildEngineStub
                .Setup(x => x.LogMessageEvent(It.IsAny<BuildMessageEventArgs>()))
                .Callback<BuildMessageEventArgs>(e => actualMessage = e.Message);

            bool actualResult = task.Execute();

            Assert.Equal("No packages found requiring installation", actualMessage);
            Assert.True(actualResult);
        }

        private static NuGetFetch CreateTaskWithDefaultStubs(Mock<IPackageManager> packageManagerStub = null,
                                                             Mock<IPackageRepository> packageRepositoryStub = null,
                                                             Mock<IBuildEngine> buildEngineStub = null,
                                                             Mock<IFileSystemProvider> fileSystemProvider = null,
                                                             Mock<IPackageRepositoryFactory> repositoryFactory = null,
                                                             Mock<IPackageSourceProvider> packageSourceProvider = null) {
            if (packageManagerStub == null) {
                packageManagerStub = new Mock<IPackageManager>();
            }
            packageManagerStub.Setup(x => x.PathResolver.GetPackageDirectory(It.IsAny<string>(), It.IsAny<SemanticVersion>())).Returns("packagedir");
            packageManagerStub.Setup(x => x.PathResolver.GetPackageFileName(It.IsAny<string>(), It.IsAny<SemanticVersion>())).Returns("packagename");

            if (packageRepositoryStub == null) {
                packageRepositoryStub = new Mock<IPackageRepository>();
                packageRepositoryStub.Setup(c => c.GetPackages())
                                     .Returns(new[] { PackageUtility.CreatePackage("package1", "1.0.0"), PackageUtility.CreatePackage("package2", "1.0.2"), PackageUtility.CreatePackage("package3", "1.0.5") }.AsQueryable());
            }

            if (buildEngineStub == null) {
                buildEngineStub = new Mock<IBuildEngine>();
            }

            Mock<IPackageManagerFactory> packageManagerFactoryStub = new Mock<IPackageManagerFactory>();
            packageManagerFactoryStub
                .Setup(x => x.CreateFrom(It.IsAny<IPackageRepository>(), It.IsAny<string>()))
                .Returns(packageManagerStub.Object);

            if (fileSystemProvider == null) {
                fileSystemProvider = new Mock<IFileSystemProvider>();
                fileSystemProvider.Setup(c => c.CreateFileSystem(It.IsAny<string>())).Returns(GetFileSystem());
            }

            if (repositoryFactory == null) {
                repositoryFactory = new Mock<IPackageRepositoryFactory>();
                repositoryFactory
                    .Setup(x => x.CreateRepository(It.IsAny<string>()))
                    .Returns(packageRepositoryStub.Object);
            }

            if (packageSourceProvider == null) {
                packageSourceProvider = new Mock<IPackageSourceProvider>();
                packageSourceProvider.Setup(c => c.LoadPackageSources()).Returns(new[] { new PackageSource("Foo") });
            }

            var task = new NuGetFetch(packageManagerFactoryStub.Object, fileSystemProvider.Object, repositoryFactory.Object, packageSourceProvider.Object);

            task.BuildEngine = buildEngineStub.Object;
            task.Sources = new[] { "http://www.boogerloo.com/hello" };
            task.PackageDir = "packages";
            return task;
        }

        private static MockFileSystem GetFileSystem(IEnumerable<PackageReference> configReferences = null) {
            configReferences = configReferences ?? new[] {
                    new PackageReference("package1", new SemanticVersion("1.0.0"), new VersionSpec()),
                    new PackageReference("package2", new SemanticVersion("1.0.2"), new VersionSpec()),
                    new PackageReference("package3", new SemanticVersion("1.0.5"), new VersionSpec())
            };

            var content = @"<?xml version=""1.0"" encoding=""utf-8""?><packages>"
                        + String.Join("", configReferences.Select(c => String.Format(@"<package id=""{0}"" version=""{1}"" />", c.Id, c.Version)))
                        + "</packages>";

            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile("packages.config", content.AsStream());
            return mockFileSystem;
        }
    }
}
