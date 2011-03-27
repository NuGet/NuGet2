using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Authoring;

namespace NuGet.Test.MSBuild {
    [TestClass]
    public class NuGetTaskUnitTest {
        [TestMethod]
        public void WillLogAnErrorWhenTheSpecFileIsEmpty() {
            string actualMessage = null;
            var fileSystemStub = new Mock<IExtendedFileSystem>();
            var buildEngineStub = new Mock<IBuildEngine>();
            NuGet.MSBuild.NuGet task = CreateTaskWithDefaultStubs(fileSystemStub: fileSystemStub, buildEngineStub: buildEngineStub);
            buildEngineStub
                .Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(e => actualMessage = e.Message);
            fileSystemStub
                .Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(false);
            task.SpecFile = string.Empty;

            bool actualResut = task.Execute();

            Assert.AreEqual("The spec file must not be empty.", actualMessage);
            Assert.IsFalse(actualResut);
        }

        [TestMethod]
        public void WillSetOutputPathWhenRun() {
            var packageStreamStub = new Mock<Stream>();
            var packageBuilderStub = new Mock<IPackageBuilder>();
            NuGet.MSBuild.NuGet task = CreateTaskWithDefaultStubs(packageBuilderStub: packageBuilderStub, packageStreamStub: packageStreamStub);

            bool actualResut = task.Execute();
            string packagePath = task.OutputPackage;

            packageBuilderStub.Verify(x => x.Save(packageStreamStub.Object));
            Assert.AreEqual(createdPackage, packagePath);
        }

        [TestMethod]
        public void WillErrorWhenTheSpecFileDoesNotExist() {
            string actualMessage = null;
            var fileSystemStub = new Mock<IExtendedFileSystem>();
            var buildEngineStub = new Mock<IBuildEngine>();
            NuGet.MSBuild.NuGet task = CreateTaskWithDefaultStubs(fileSystemStub: fileSystemStub, buildEngineStub: buildEngineStub);
            buildEngineStub
                .Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(e => actualMessage = e.Message);
            fileSystemStub
                .Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(false);
            task.SpecFile = "aPathThatDoesNotExist";

            bool actualResut = task.Execute();

            Assert.AreEqual("The spec file does not exist.", actualMessage);
            Assert.IsFalse(actualResut);
        }

        [TestMethod]
        public void WillCreatePackageUsingSpecFileAndWorkingDirectory() {
            var packageStreamStub = new Mock<Stream>();
            var packageBuilderStub = new Mock<IPackageBuilder>();
            NuGet.MSBuild.NuGet task = CreateTaskWithDefaultStubs(packageBuilderStub: packageBuilderStub, packageStreamStub: packageStreamStub);

            bool actualResut = task.Execute();

            packageBuilderStub.Verify(x => x.Save(packageStreamStub.Object));
            Assert.IsTrue(actualResut);
        }

        [TestMethod]
        public void WillRemoveNuspecFilesFromPackage() {
            var packageBuilderStub = new Mock<IPackageBuilder>();
            var packageFileStub = new Mock<IPackageFile>();
            packageFileStub.Setup(x => x.Path).Returns("/aFile.nuspec");
            NuGet.MSBuild.NuGet task = CreateTaskWithDefaultStubs(packageBuilderStub: packageBuilderStub);
            packageBuilderStub.Setup(x => x.Files).Returns(new Collection<IPackageFile>() { packageFileStub.Object });

            bool actualResut = task.Execute();

            Assert.AreEqual(0, packageBuilderStub.Object.Files.Count);
        }

        [TestMethod]
        public void WillRemoveNupkgFilesFromPackage() {
            var packageBuilderStub = new Mock<IPackageBuilder>();
            var packageFileStub = new Mock<IPackageFile>();
            packageFileStub.Setup(x => x.Path).Returns("/aFile.nupkg");
            NuGet.MSBuild.NuGet task = CreateTaskWithDefaultStubs(packageBuilderStub: packageBuilderStub);
            packageBuilderStub.Setup(x => x.Files).Returns(new Collection<IPackageFile>() { packageFileStub.Object });

            bool actualResut = task.Execute();

            Assert.AreEqual(0, packageBuilderStub.Object.Files.Count);
        }

        [TestMethod]
        public void WillNotRemoveALibraryFileFromPackage() {
            var packageBuilderStub = new Mock<IPackageBuilder>();
            var packageFileStub = new Mock<IPackageFile>();
            packageFileStub.Setup(x => x.Path).Returns("/lib/aFile.dll");
            NuGet.MSBuild.NuGet task = CreateTaskWithDefaultStubs(packageBuilderStub: packageBuilderStub);
            packageBuilderStub.Setup(x => x.Files).Returns(new Collection<IPackageFile>() { packageFileStub.Object });

            bool actualResut = task.Execute();

            Assert.AreEqual(1, packageBuilderStub.Object.Files.Count);
        }

        [TestMethod]
        public void WillLogMessagesBeforeAndAfterPackageCreation() {
            Queue<string> actualMessages = new Queue<string>();
            var buildEngineStub = new Mock<IBuildEngine>();
            NuGet.MSBuild.NuGet task = CreateTaskWithDefaultStubs(buildEngineStub: buildEngineStub);
            buildEngineStub
                .Setup(x => x.LogMessageEvent(It.IsAny<BuildMessageEventArgs>()))
                .Callback<BuildMessageEventArgs>(e => actualMessages.Enqueue(e.Message));

            task.Execute();

            Assert.AreEqual("Creating a package for  at .", actualMessages.Dequeue());
            Assert.AreEqual("Created a package for  at .", actualMessages.Dequeue());
        }

        [TestMethod]
        public void WillLogAnErrorWhenAnUnexpectedErrorHappens() {
            string actualMessage = null;
            var packageBuilderStub = new Mock<IPackageBuilder>();
            var buildEngineStub = new Mock<IBuildEngine>();
            NuGet.MSBuild.NuGet task = CreateTaskWithDefaultStubs(packageBuilderStub: packageBuilderStub, buildEngineStub: buildEngineStub);
            packageBuilderStub.Setup(x => x.Save(It.IsAny<Stream>())).Throws(new Exception());
            buildEngineStub
                .Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(e => actualMessage = e.Message);

            bool actualResut = task.Execute();

            Assert.IsTrue(actualMessage.Contains("An unexpected error occurred while creating the package:"));
            Assert.IsFalse(actualResut);
        }

        private const string createdPackage = "/thePackageId.1.0.nupkg";

        static NuGet.MSBuild.NuGet CreateTaskWithDefaultStubs(
            Mock<IExtendedFileSystem> fileSystemStub = null,
            Mock<IPackageBuilderFactory> packageBuilderFactoryStub = null,
            Mock<IPackageBuilder> packageBuilderStub = null,
            Mock<Stream> packageStreamStub = null,
            Mock<IBuildEngine> buildEngineStub = null) {

            if (fileSystemStub == null)
                fileSystemStub = new Mock<IExtendedFileSystem>();
            if (packageBuilderFactoryStub == null)
                packageBuilderFactoryStub = new Mock<IPackageBuilderFactory>();
            if (packageBuilderStub == null)
                packageBuilderStub = new Mock<IPackageBuilder>();
            if (packageStreamStub == null)
                packageStreamStub = new Mock<Stream>();
            if (buildEngineStub == null)
                buildEngineStub = new Mock<IBuildEngine>();

            packageBuilderStub
                .SetupGet(x => x.Id)
                .Returns("thePackageId");
            packageBuilderStub
                .SetupGet(x => x.Version)
                .Returns(new Version(1, 0));
            packageBuilderStub
                .SetupGet(x => x.Files)
                .Returns(new Collection<IPackageFile>());
            packageBuilderFactoryStub
                .Setup(x => x.CreateFrom("/thePackageId.nuspec"))
                .Returns(packageBuilderStub.Object);
            fileSystemStub
                .Setup(x => x.FileExists("thePackageId.nuspec"))
                .Returns(true);
            fileSystemStub
                .Setup(x => x.GetCurrentDirectory())
                .Returns("/");
            fileSystemStub
                .Setup(x => x.CreateFile(createdPackage))
                .Returns(packageStreamStub.Object);

            var task = new global::NuGet.MSBuild.NuGet(fileSystemStub.Object, packageBuilderFactoryStub.Object);

            task.BuildEngine = buildEngineStub.Object;
            task.SpecFile = "thePackageId.nuspec";

            return task;
        }
    }
}
