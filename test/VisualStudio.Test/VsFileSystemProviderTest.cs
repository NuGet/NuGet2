using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test;

namespace NuGet.VisualStudio.Test {
    [TestClass]
    public class VsFileSystemProviderTest {
        [TestMethod]
        public void VsFileSystemProviderThrowsIfDteIsNull() {
            // Act and Assert
            ExceptionAssert.ThrowsArgNull(() => new VsFileSystemProvider(dte: null, componentModel: null, settings: null), "dte");
        }

        [TestMethod]
        public void VsFileSystemProviderThrowsIfComponentModelIsNull() {
            // Act and Assert
            ExceptionAssert.ThrowsArgNull(() => new VsFileSystemProvider(dte: new Mock<DTE>().Object, componentModel: null, settings: null), "componentModel");
        }

        [TestMethod]
        public void VsFileSystemProviderThrowsIfSettingsIsNull() {
            // Act and Assert
            ExceptionAssert.ThrowsArgNull(() => new VsFileSystemProvider(dte: new Mock<DTE>().Object, componentModel: new Mock<IComponentModel>().Object, settings: null), "settings");
        }

        [TestMethod]
        public void VsFileSystemProviderReturnsPhysialFileSystemIfSourceControlSupportIsDisabled() {
            // Arrange
            var dte = new Mock<DTE>();
            var componentModel = new Mock<IComponentModel>();
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValue("solution", "disableSourceControlIntegration")).Returns("true").Verifiable();

            // Act
            var vsFileSystemProvider = new VsFileSystemProvider(dte.Object, componentModel.Object, settings.Object);
            var fileSystem = vsFileSystemProvider.GetFileSystem(@"x:\test-path");

            // Assert
            Assert.IsInstanceOfType(fileSystem, typeof(PhysicalFileSystem));
            Assert.AreEqual(@"x:\test-path", fileSystem.Root);
            settings.Verify();
        }
    }
}
