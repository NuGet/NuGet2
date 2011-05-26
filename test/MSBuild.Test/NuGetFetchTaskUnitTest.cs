using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.MSBuild;

namespace NuGet.Test.MSBuild {
    [TestClass]
    public class NuGetFetchTaskUnitTest {

        [TestMethod]
        public void WillErrorIfPackagesDotConfigUnreadable() {
            var packageReferenceProvider = new Mock<IPackageReferenceProvider>();
            packageReferenceProvider
                .Setup(x => x.getPackageReferences(It.IsAny<string>()))
                .Throws(new InvalidOperationException());

            string actualMessage = null;
            var buildEngineStub = new Mock<IBuildEngine>();
            buildEngineStub
                .Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(e => actualMessage = e.Message);

            NuGet.MSBuild.NuGetFetch task = CreateTaskWithDefaultStubs(buildEngineStub:buildEngineStub, packageReferenceProviderStub:packageReferenceProvider);

            var result = task.Execute();

            Assert.AreEqual(false, result);
            Assert.AreEqual("Error parsing packages.config file at '/packages.config'", actualMessage);
        }

        [TestMethod]
        public void WillGetSettingsFromSpecifiedConfigFileIfFeedsAreNull()
        {
            var installed = 0;

            var packageManagerStub = new Mock<IPackageManager>();
            packageManagerStub
                .Setup(x => x.InstallPackage(It.IsAny<string>(), It.IsAny<Version>(), true))
                .Callback(() => installed++);

            var aggregateRepoStub = new Mock<IAggregateRepositoryFactory>();
            aggregateRepoStub
                .Setup(x => x.createSpecificSettingsRepository(It.IsAny<string>()))
                .Returns(new Mock<IPackageRepository>().Object);

            NuGet.MSBuild.NuGetFetch task = CreateTaskWithDefaultStubs(packageManagerStub: packageManagerStub, aggregateRepositoryFactory: aggregateRepoStub);
            task.FeedUrls = null;
            task.NugetConfigPath = "pathToConfigFile";

            bool actualResult = task.Execute();

            Assert.AreEqual(3, installed);
            Assert.IsTrue(actualResult);
        }

        [TestMethod]
        public void WillGetSettingsFromSpecifiedConfigFileIfFeedsAreNotSpecified() {
            var installed = 0;

            var packageManagerStub = new Mock<IPackageManager>();
            packageManagerStub
                .Setup(x => x.InstallPackage(It.IsAny<string>(), It.IsAny<Version>(), true))
                .Callback(() => installed++);

            var aggregateRepoStub = new Mock<IAggregateRepositoryFactory>();
            aggregateRepoStub
                .Setup(x => x.createSpecificSettingsRepository(It.IsAny<string>()))
                .Returns(new Mock<IPackageRepository>().Object);

            NuGet.MSBuild.NuGetFetch task = CreateTaskWithDefaultStubs(packageManagerStub: packageManagerStub, aggregateRepositoryFactory:aggregateRepoStub);
            task.FeedUrls = new string[]{};
            task.NugetConfigPath = "pathToConfigFile";

            bool actualResult = task.Execute();

            Assert.AreEqual(3, installed);
            Assert.IsTrue(actualResult);
        }

        [TestMethod]
        public void WillGetSettingsFromDefaultConfigFileIfFeedsAreNotSpecified() {
            var installed = 0;

            var packageManagerStub = new Mock<IPackageManager>();
            packageManagerStub
                .Setup(x => x.InstallPackage(It.IsAny<string>(), It.IsAny<Version>(), true))
                .Callback(() => installed++);

            var aggregateRepoStub = new Mock<IAggregateRepositoryFactory>();
            aggregateRepoStub
                .Setup(x => x.createDefaultSettingsRepository())
                .Returns(new Mock<IPackageRepository>().Object);

            NuGet.MSBuild.NuGetFetch task = CreateTaskWithDefaultStubs(packageManagerStub: packageManagerStub, aggregateRepositoryFactory: aggregateRepoStub);
            task.FeedUrls = new string[] { };

            bool actualResult = task.Execute();

            Assert.AreEqual(3, installed);
            Assert.IsTrue(actualResult);
        }

        [TestMethod]
        public void WillErrorIfPackagesDotConfigNotFound() {
            var packageReferenceProviderStub = new Mock<IPackageReferenceProvider>();
            packageReferenceProviderStub
                .Setup(x => x.getPackageReferences(It.IsAny<string>()))
                .Throws(new FileNotFoundException());
            string actualMessage = null;
            var buildEngineStub = new Mock<IBuildEngine>();
            buildEngineStub
                .Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(e => actualMessage = e.Message);

            NuGet.MSBuild.NuGetFetch task = CreateTaskWithDefaultStubs(buildEngineStub: buildEngineStub, packageReferenceProviderStub: packageReferenceProviderStub);
            task.FeedUrls = new string[] { };

            var result = task.Execute();

            Assert.AreEqual(false, result);
            Assert.AreEqual("Package configuration file '/packages.config' does not exist", actualMessage);
        }

        [TestMethod]
        public void WillErrorIfFeedsInvalid() {
            var aggregateRepositoryStub = new Mock<IAggregateRepositoryFactory>();
            aggregateRepositoryStub
                .Setup(x =>x.createSpecificFeedsRepository(It.IsAny<bool>(), It.IsAny<IEnumerable<string>>()))
                .Throws(new InvalidOperationException("Problem with feed"));

            string actualMessage = null;
            var buildEngineStub = new Mock<IBuildEngine>();
            buildEngineStub
                .Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(e => actualMessage = e.Message);

            NuGet.MSBuild.NuGetFetch task = CreateTaskWithDefaultStubs(aggregateRepositoryFactory:aggregateRepositoryStub, buildEngineStub:buildEngineStub);
            var result = task.Execute();

            Assert.AreEqual(false, result);
            Assert.AreEqual("Problem with feed", actualMessage);
        }

        [TestMethod]
        public void WillInstallPackagesFromPackagesDotConfig() {
            var installed = 0;
            var packageManagerStub = new Mock<IPackageManager>();

            NuGet.MSBuild.NuGetFetch task = CreateTaskWithDefaultStubs(packageManagerStub:packageManagerStub);

            packageManagerStub
                .Setup(x => x.InstallPackage(It.IsAny<string>(), It.IsAny<Version>(), true))
                .Callback( () => installed++ );

            bool actualResult = task.Execute();

            Assert.AreEqual(3,installed);
            Assert.IsTrue(actualResult);
        }

        [TestMethod]
        public void WilNotReInstallExistingPackages() {
            var installed = 0;
            var packageManagerStub = new Mock<IPackageManager>();
            var fileSystemStub = new Mock<IFileSystem>();

            NuGet.MSBuild.NuGetFetch task = CreateTaskWithDefaultStubs(packageManagerStub: packageManagerStub, fileSystemStub: fileSystemStub);

            packageManagerStub
                .Setup(x => x.InstallPackage(It.IsAny<string>(), It.IsAny<Version>(), true))
                .Callback(() => installed++);
            fileSystemStub.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);

            bool actualResult = task.Execute();

            Assert.AreEqual(0, installed);
            Assert.IsTrue(actualResult);
        }

        [TestMethod]
        public void WillDoNothingIfNoPackagesNeedToBeInstalled() {
            string actualMessage = string.Empty;
            var buildEngineStub = new Mock<IBuildEngine>();
            var installed = 0;
            var packageManagerStub = new Mock<IPackageManager>();
            var fileSystemStub = new Mock<IFileSystem>();

            NuGet.MSBuild.NuGetFetch task = CreateTaskWithDefaultStubs(buildEngineStub: buildEngineStub, packageManagerStub: packageManagerStub, fileSystemStub: fileSystemStub);

            packageManagerStub
                .Setup(x => x.InstallPackage(It.IsAny<string>(), It.IsAny<Version>(), true))
                .Callback(() => installed++);
            fileSystemStub.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);

            buildEngineStub
                .Setup(x => x.LogMessageEvent(It.IsAny<BuildMessageEventArgs>()))
                .Callback<BuildMessageEventArgs>(e => actualMessage = e.Message);

            bool actualResult = task.Execute();

            Assert.AreEqual("No packages found requiring installation", actualMessage);
            Assert.IsTrue(actualResult);
        }

        private const string createdPackage = "/thePackageId.1.0.nupkg";

        static NuGet.MSBuild.NuGetFetch CreateTaskWithDefaultStubs(
            Mock<IPackageManager> packageManagerStub = null,
            Mock<IPackageRepository> packageRepositoryStub = null,
            Mock<IFileSystem> fileSystemStub = null,
            Mock<IBuildEngine> buildEngineStub = null,
            Mock<IPackageReferenceProvider> packageReferenceProviderStub = null,
            Mock<IAggregateRepositoryFactory> aggregateRepositoryFactory = null) {

            if (packageManagerStub == null)
            {
                packageManagerStub = new Mock<IPackageManager>();
            }
            packageManagerStub.Setup(x => x.PathResolver.GetPackageDirectory(It.IsAny<string>(), It.IsAny<Version>())).Returns("/packagedir");
            packageManagerStub.Setup(x => x.PathResolver.GetPackageFileName(It.IsAny<string>(), It.IsAny<Version>())).Returns("packagename");
            
            if (packageRepositoryStub == null)
                packageRepositoryStub = new Mock<IPackageRepository>();

            if (fileSystemStub == null)
            {
                fileSystemStub = new Mock<IFileSystem>();
                fileSystemStub.Setup(x => x.FileExists(It.Is<string>(y => y == "/packages.config"))).Returns(true);
            }

            if (buildEngineStub == null)
                buildEngineStub = new Mock<IBuildEngine>();

            Mock<IPackageManagerFactory> packageManagerFactoryStub = new Mock<IPackageManagerFactory>();
            packageManagerFactoryStub
                .Setup(x => x.CreateFrom(It.IsAny<IPackageRepository>(), It.IsAny<string>()))
                .Returns(packageManagerStub.Object);

            if (packageReferenceProviderStub == null)
            {
                packageReferenceProviderStub = new Mock<IPackageReferenceProvider>();
                packageReferenceProviderStub
                    .Setup(x => x.getPackageReferences(It.IsAny<string>()))
                    .Returns(new[]
                                 {
                                     new PackageReference("package1", new Version("1.0.0"), new VersionSpec()),
                                     new PackageReference("package2", new Version("1.0.2"), new VersionSpec()),
                                     new PackageReference("package3", new Version("1.0.5"), new VersionSpec())
                                 });
            }
            else if (aggregateRepositoryFactory != null) {
                throw new ArgumentException("Cannot specify IAggregateRepositoryFactory and IPackageRepository at the same time");
            }

            if (aggregateRepositoryFactory == null)
            {
                aggregateRepositoryFactory = new Mock<IAggregateRepositoryFactory>();
                aggregateRepositoryFactory
                    .Setup(x => x.createSpecificFeedsRepository(It.IsAny<bool>(), It.IsAny<IEnumerable<string>>()))
                    .Returns(packageRepositoryStub.Object);
            }

            var task = new global::NuGet.MSBuild.NuGetFetch(packageManagerFactoryStub.Object, fileSystemStub.Object, packageReferenceProviderStub.Object, aggregateRepositoryFactory.Object);

            task.BuildEngine = buildEngineStub.Object;
            task.FeedUrls = new[] {"http://www.boogerloo.com/hello"};
            task.PackageConfigFile = "/packages.config";
            task.PackageDir = "/packages";
            return task;
        }
    }
}
