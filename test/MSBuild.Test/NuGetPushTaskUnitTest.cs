using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Moq;
using NuGet.MSBuild;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test.MSBuild
{

    public class NuGetPushTaskUnitTest
    {
        private const string apiKey = "myApiKey";
        private const string server = "server";

        [Fact]
        public void WillLogAnErrorWhenThePackageDoesNotExist()
        {
            string actualMessage = null;
            var buildEngineStub = new Mock<IBuildEngine>();
            var fileSystemStub = new Mock<IFileSystem>();
            var fileSystemProvider = new Mock<IFileSystemProvider>();

            NuGetPush task = CreateTaskWithDefaultStubs(buildEngineStub: buildEngineStub, fileSystemStub: fileSystemProvider);

            fileSystemStub
                .Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(false);
            buildEngineStub
                .Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(e => actualMessage = e.Message);
            fileSystemProvider.Setup(c => c.CreateFileSystem(It.IsAny<string>())).Returns(fileSystemStub.Object);

            task.PackagePath = "aPathThatDoesNotExist";

            bool actualResult = task.Execute();

            Assert.Equal("The package at 'aPathThatDoesNotExist' does not exist.", actualMessage);
            Assert.False(actualResult);
        }

        [Fact]
        public void WillGetApiKeyFromConfigFileIfNotSpecified()
        {
            var packageServerStub = new Mock<IPackageServer>();
            var settingsStub = new Mock<ISettings>();

            NuGetPush task = CreateTaskWithDefaultStubs(packageServerStub: packageServerStub, settingsStub: settingsStub);

            task.ApiKey = "";
            task.Source = "the-site-to-upload-to";
            settingsStub
                .Setup(x => x.GetValues(It.IsAny<string>()))
                .Returns(new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>(task.Source, "shshshshs") });
            packageServerStub
                .Setup(x => x.PushPackage(It.IsAny<string>(), It.IsAny<Stream>()));

            bool actualResult = task.Execute();

            Assert.True(actualResult);
        }

        [Fact]
        public void WillPublishPackageByDefault()
        {
            var packageServerStub = new Mock<IPackageServer>();

            NuGetPush task = CreateTaskWithDefaultStubs(packageServerStub: packageServerStub);

            packageServerStub
                .Setup(x => x.PushPackage(It.IsAny<string>(), It.IsAny<Stream>()));

            bool actualResult = task.Execute();

            Assert.True(actualResult);
        }

        [Fact]
        public void WillPushButNotPublishIfCreateOnly()
        {
            var packageServerStub = new Mock<IPackageServer>();

            NuGetPush task = CreateTaskWithDefaultStubs(packageServerStub: packageServerStub);
            
            packageServerStub
                .Setup(x => x.PushPackage(It.IsAny<string>(), It.IsAny<Stream>()));

            bool actualResult = task.Execute();

            Assert.True(actualResult);
        }

        private static NuGetPush CreateTaskWithDefaultStubs(Mock<IBuildEngine> buildEngineStub = null,
                                                            Mock<IPackageServer> packageServerStub = null,
                                                            Mock<IFileSystemProvider> fileSystemStub = null,
                                                            Mock<IPackageFactory> packageFactoryStub = null,
                                                            Mock<ISettings> settingsStub = null)
        {
            const string packagePath = "thePackageId.1.0.nupkg";

            if (buildEngineStub == null)
            {
                buildEngineStub = new Mock<IBuildEngine>();
            }
            if (packageServerStub == null)
            {
                packageServerStub = new Mock<IPackageServer>();
            }
            if (fileSystemStub == null)
            {
                fileSystemStub = new Mock<IFileSystemProvider>();
            }
            if (packageFactoryStub == null)
            {
                packageFactoryStub = new Mock<IPackageFactory>();
            }
            if (settingsStub == null)
            {
                settingsStub = new Mock<ISettings>();
            }

            Mock<IPackageServerFactory> packageServerFactory = new Mock<IPackageServerFactory>();
            Mock<IPackage> packageStub = new Mock<IPackage>();
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile(packagePath);

            packageStub
                .Setup(x => x.Id)
                .Returns("package");
            packageStub
                .Setup(x => x.Version)
                .Returns(new SemanticVersion(1, 0, 0, 0));
            packageServerFactory
                .Setup(x => x.CreateFrom(It.IsAny<string>()))
                .Returns(packageServerStub.Object);
            fileSystemStub
                .Setup(x => x.CreateFileSystem(It.IsAny<string>()))
                .Returns(mockFileSystem);
            packageFactoryStub
                .Setup(x => x.CreatePackage(It.IsAny<Func<Stream>>()))
                .Returns(packageStub.Object);


            var task = new NuGetPush(packageServerFactory.Object, packageFactoryStub.Object, fileSystemStub.Object, settingsStub.Object);

            task.BuildEngine = buildEngineStub.Object;
            task.PackagePath = packagePath;
            task.ApiKey = apiKey;
            task.Source = server;

            return task;
        }
    }
}
