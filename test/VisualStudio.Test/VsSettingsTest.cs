using System;
using Moq;
using NuGet.Test;
using NuGet.Test.Mocks;
using NuGet.VisualStudio.Test.Mocks;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    public class VsSettingsTest
    {
        [Fact]
        public void VsSettingsThrowsIfSolutionManagerIsNull()
        {
            // Act and Assert
            ExceptionAssert.ThrowsArgNull(() => new VsSettings(solutionManager: null), "solutionManager");
        }

        [Fact]
        public void VsSettingsThrowsIfDefaultSettingsIsNull()
        {
            // Act and Assert
            ExceptionAssert.ThrowsArgNull(() => new VsSettings(solutionManager: new Mock<ISolutionManager>().Object, defaultSettings: null, fileSystemProvider: null), "defaultSettings");
        }

        [Fact]
        public void VsSettingsThrowsIfFileSystemProviderIsNull()
        {
            // Act and Assert
            ExceptionAssert.ThrowsArgNull(
                () => new VsSettings(solutionManager: new Mock<ISolutionManager>().Object, defaultSettings: NullSettings.Instance, fileSystemProvider: null), "fileSystemProvider");
        }

        [Fact]
        public void VsSettingUsesNullSettingsIfSolutionIsUnavailable()
        {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>(MockBehavior.Strict);
            solutionManager.Setup(s => s.IsSolutionOpen).Returns(false).Verifiable();
            var defaultSettings = new Mock<ISettings>(MockBehavior.Strict);
            var fileSystemProvider = new Mock<IFileSystemProvider>(MockBehavior.Strict);
            fileSystemProvider.Setup(f => f.GetFileSystem(It.IsAny<string>())).Throws(new Exception("This method should not be called"));

            // Act
            var vsSettings = new VsSettings(solutionManager.Object, defaultSettings.Object, fileSystemProvider.Object);
            var value = vsSettings.GetValue("Solution", "Foo");

            // Assert
            Assert.Equal("", value);
            solutionManager.VerifyAll();
        }

        [Fact]
        public void VsSettingUsesNullSettingsIfSolutionDirectoryDoesNotExist()
        {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>(MockBehavior.Strict);
            solutionManager.Setup(s => s.IsSolutionOpen).Returns(true).Verifiable();
            solutionManager.Setup(s => s.SolutionDirectory).Returns((string)null).Verifiable();
            var defaultSettings = new Mock<ISettings>(MockBehavior.Strict);
            var fileSystemProvider = new Mock<IFileSystemProvider>(MockBehavior.Strict);
            fileSystemProvider.Setup(f => f.GetFileSystem(It.IsAny<string>())).Throws(new Exception("This method should not be called"));

            // Act
            var vsSettings = new VsSettings(solutionManager.Object, defaultSettings.Object, fileSystemProvider.Object);
            var value = vsSettings.GetValue("Solution", "Foo");

            // Assert
            Assert.Equal("", value);
            solutionManager.VerifyAll();
        }

        [Fact]
        public void VsSettingUsesNullSettingsIfConfigFileDoesNotExistInRootOfSolution()
        {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>(MockBehavior.Strict);
            solutionManager.Setup(s => s.IsSolutionOpen).Returns(true).Verifiable();
            solutionManager.Setup(s => s.SolutionDirectory).Returns(@"x:\").Verifiable();
            var defaultSettings = new Mock<ISettings>(MockBehavior.Strict);
            var fileSystemProvider = new Mock<IFileSystemProvider>(MockBehavior.Strict);
            fileSystemProvider.Setup(f => f.GetFileSystem(@"x:\")).Returns(new MockFileSystem());

            // Act
            var vsSettings = new VsSettings(solutionManager.Object, defaultSettings.Object, fileSystemProvider.Object);
            var value = vsSettings.GetValue("Solution", "Foo");

            // Assert
            Assert.Equal("", value);
            solutionManager.VerifyAll();
        }

        [Fact]
        public void VsSettingUsesSettingsFileFromSolutionRootIfExists()
        {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>(MockBehavior.Strict);
            solutionManager.Setup(s => s.IsSolutionOpen).Returns(true).Verifiable();
            solutionManager.Setup(s => s.SolutionDirectory).Returns(@"x:\solution").Verifiable();
            var defaultSettings = new Mock<ISettings>(MockBehavior.Strict);

            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("nuget.config", @"<?xml version=""1.0"" ?><configuration><solution><add key=""foo"" value=""bar"" /></solution></configuration>");
            var fileSystemProvider = new Mock<IFileSystemProvider>(MockBehavior.Strict);
            fileSystemProvider.Setup(f => f.GetFileSystem(@"x:\solution")).Returns(fileSystem).Verifiable();

            // Act
            var vsSettings = new VsSettings(solutionManager.Object, defaultSettings.Object, fileSystemProvider.Object);
            var value = vsSettings.GetValue("solution", "foo");

            // Assert
            Assert.Equal("bar", value);
            solutionManager.VerifyAll();
            fileSystemProvider.VerifyAll();
        }

        [Fact]
        public void VsSettingUsesValuesFromDefaultSettingsForNonSolutionProperties()
        {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>(MockBehavior.Strict);
            solutionManager.Setup(s => s.IsSolutionOpen).Returns(true).Verifiable();
            solutionManager.Setup(s => s.SolutionDirectory).Returns(@"x:\solution").Verifiable();
            var defaultSettings = new Mock<ISettings>(MockBehavior.Strict);
            defaultSettings.Setup(d => d.GetValue("PackageSources", "foo")).Returns("qux").Verifiable();

            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("nuget.config", @"<?xml version=""1.0"" ?><configuration><solution><add key=""foo"" value=""bar"" /></solution></configuration>");
            var fileSystemProvider = new Mock<IFileSystemProvider>(MockBehavior.Strict);
            fileSystemProvider.Setup(f => f.GetFileSystem(@"x:\solution")).Returns(fileSystem).Verifiable();

            // Act
            var vsSettings = new VsSettings(solutionManager.Object, defaultSettings.Object, fileSystemProvider.Object);
            var value = vsSettings.GetValue("PackageSources", "foo");

            // Assert
            Assert.Equal("qux", value);
            defaultSettings.Verify();
        }

        [Fact]
        public void VsSettingSwitchesSettingsIfSolutionChanges()
        {
            // Arrange
            var solutionManager = new Mock<MockSolutionManager>();
            solutionManager.Setup(s => s.IsSolutionOpen).Returns(true).Verifiable();
            solutionManager.Setup(s => s.SolutionDirectory).Returns(@"x:\solution").Verifiable();
            var defaultSettings = new Mock<ISettings>(MockBehavior.Strict);

            var fileSystemA = new MockFileSystem();
            fileSystemA.AddFile("nuget.config", @"<?xml version=""1.0"" ?><configuration><solution><add key=""foo"" value=""barA"" /></solution></configuration>");

            var fileSystemB = new MockFileSystem();
            fileSystemB.AddFile("nuget.config", @"<?xml version=""1.0"" ?><configuration><solution><add key=""foo"" value=""barB"" /></solution></configuration>");

            var fileSystemProvider = new Mock<IFileSystemProvider>(MockBehavior.Strict);
            fileSystemProvider.Setup(f => f.GetFileSystem(@"x:\solution")).Returns(fileSystemA).Verifiable();
            fileSystemProvider.Setup(f => f.GetFileSystem(@"y:\solution")).Returns(fileSystemB).Verifiable();

            // Act
            var vsSettings = new VsSettings(solutionManager.Object, defaultSettings.Object, fileSystemProvider.Object);
            var valueA = vsSettings.GetValue("solution", "foo");
            solutionManager.Object.CloseSolution();
            solutionManager.Setup(s => s.SolutionDirectory).Returns(@"y:\solution").Verifiable();
            solutionManager.Object.CloseSolution();

            var valueB = vsSettings.GetValue("solution", "foo");

            // Assert
            Assert.Equal("barA", valueA);
            Assert.Equal("barB", valueB);
        }
    }
}
