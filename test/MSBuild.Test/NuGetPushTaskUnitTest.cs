using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Common;
using NuGet.MSBuild;

namespace NuGet.Test.MSBuild {
    [TestClass]
    public class NuGetPushTaskUnitTest {
        [TestMethod]
        public void WillLogAnErrorWhenThePackageDoesNotExist() {
            string actualMessage = null;
            var buildEngineStub = new Mock<IBuildEngine>();
            var fileSystemStub = new Mock<IFileSystem>();

            NuGet.MSBuild.NuGetPush task = CreateTaskWithDefaultStubs(buildEngineStub:buildEngineStub, fileSystemStub:fileSystemStub);

            fileSystemStub
                .Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(false);
            buildEngineStub
                .Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(e => actualMessage = e.Message);

            task.PackagePath = "aPathThatDoesNotExist";

            bool actualResult = task.Execute();

            Assert.AreEqual("The package at 'aPathThatDoesNotExist' does not exist.", actualMessage);
            Assert.IsFalse(actualResult);
        }

        [TestMethod]
        public void WillGetApiKeyFromConfigFileIfNotSpecified() {
            var galleryServerStub = new Mock<IGalleryServer>();
            var settingsStub = new Mock<ISettings>();

            NuGet.MSBuild.NuGetPush task = CreateTaskWithDefaultStubs(galleryServerStub: galleryServerStub, settingsStub:settingsStub);

            task.ApiKey = "";
            task.Source = "the-site-to-upload-to";
            settingsStub
                .Setup(x => x.GetValues(It.IsAny<string>()))
                .Returns(new List<KeyValuePair<string, string>>{ new KeyValuePair<string, string>(task.Source, "shshshshs") });
            galleryServerStub
                .Setup(x => x.CreatePackage(It.IsAny<string>(), It.IsAny<Stream>()));
            galleryServerStub
                .Setup(x => x.PublishPackage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            bool actualResult = task.Execute();

            Assert.IsTrue(actualResult);
        }

        [TestMethod]
        public void WillPublishPackageByDefault() {
            var galleryServerStub = new Mock<IGalleryServer>();

            NuGet.MSBuild.NuGetPush task = CreateTaskWithDefaultStubs(galleryServerStub: galleryServerStub);

            galleryServerStub
                .Setup(x => x.CreatePackage(It.IsAny<string>(), It.IsAny<Stream>()));
            galleryServerStub
                .Setup(x => x.PublishPackage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            bool actualResult = task.Execute();

            Assert.IsTrue(actualResult);
        }

        [TestMethod]
        public void WillPushButNotPublishIfCreateOnly() {
            var galleryServerStub = new Mock<IGalleryServer>();

            NuGet.MSBuild.NuGetPush task = CreateTaskWithDefaultStubs(galleryServerStub: galleryServerStub);
            task.CreateOnly = true;

            galleryServerStub
                .Setup(x => x.CreatePackage(It.IsAny<string>(), It.IsAny<Stream>()));
            galleryServerStub
                .Setup(x => x.PublishPackage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception());

            bool actualResult = task.Execute();

            Assert.IsTrue(actualResult);
        }

        private const string apiKey = "myApiKey";
        private const string server = "server";

        static NuGet.MSBuild.NuGetPush CreateTaskWithDefaultStubs(
            Mock<IBuildEngine> buildEngineStub = null,
            Mock<IGalleryServer> galleryServerStub = null,
            Mock<IFileSystem> fileSystemStub = null,
            Mock<IPackageFactory> packageFactoryStub = null,
            Mock<ISettings> settingsStub=null)
        {

            if (buildEngineStub == null)
                buildEngineStub = new Mock<IBuildEngine>();
            if (galleryServerStub == null)
                galleryServerStub = new Mock<IGalleryServer>();
            if (fileSystemStub == null)
                fileSystemStub = new Mock<IFileSystem>();
            if (packageFactoryStub == null)
                packageFactoryStub = new Mock<IPackageFactory>();
            if (settingsStub==null)
                settingsStub = new Mock<ISettings>();

            Mock<IGalleryServerFactory> galleryServerFactoryStub = new Mock<IGalleryServerFactory>();
            Mock<IPackage> packageStub = new Mock<IPackage>();

            var strm = new Mock<Stream>();

            packageStub
                .Setup(x => x.Id)
                .Returns("package");
            packageStub
                .Setup(x => x.Version)
                .Returns(new Version(1,0,0,0));
            galleryServerFactoryStub
                .Setup(x => x.createFrom(It.IsAny<string>()))
                .Returns(galleryServerStub.Object);
            packageStub
                .Setup(x => x.GetStream())
                .Returns(strm.Object);
            fileSystemStub
                .Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(true);
            packageFactoryStub
                .Setup(x => x.CreatePackage(It.IsAny<string>()))
                .Returns(packageStub.Object);

            var task = new global::NuGet.MSBuild.NuGetPush(galleryServerFactoryStub.Object, packageFactoryStub.Object, fileSystemStub.Object, settingsStub.Object);

            task.BuildEngine = buildEngineStub.Object;
            task.PackagePath = "/thePackageId.1.0.nupkg";
            task.ApiKey = apiKey;
            task.Source = server;

            return task;
        }
    }
}
