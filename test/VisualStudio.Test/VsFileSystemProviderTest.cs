using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Moq;
using NuGet.Test;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    public class VsFileSystemProviderTest
    {
        [Fact]
        public void VsFileSystemProviderThrowsIfDteIsNull()
        {
            // Act and Assert
            ExceptionAssert.ThrowsArgNull(() => new VsFileSystemProvider(dte: null, componentModel: null, settings: null), "dte");
        }

        [Fact]
        public void VsFileSystemProviderThrowsIfComponentModelIsNull()
        {
            // Act and Assert
            ExceptionAssert.ThrowsArgNull(() => new VsFileSystemProvider(dte: new Mock<DTE>().Object, componentModel: null, settings: null), "componentModel");
        }

        [Fact]
        public void VsFileSystemProviderThrowsIfSettingsIsNull()
        {
            // Act and Assert
            ExceptionAssert.ThrowsArgNull(() => new VsFileSystemProvider(dte: new Mock<DTE>().Object, componentModel: new Mock<IComponentModel>().Object, settings: null), "settings");
        }

        [Fact]
        public void VsFileSystemProviderReturnsPhysialFileSystemIfSourceControlSupportIsDisabled()
        {
            // Arrange
            var dte = new Mock<DTE>();
            var componentModel = new Mock<IComponentModel>();
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValue("solution", "disableSourceControlIntegration")).Returns("true").Verifiable();

            // Act
            var vsFileSystemProvider = new VsFileSystemProvider(dte.Object, componentModel.Object, settings.Object);
            var fileSystem = vsFileSystemProvider.GetFileSystem(@"x:\test-path");

            // Assert
            Assert.IsType<PhysicalFileSystem>(fileSystem);
            Assert.Equal(@"x:\test-path", fileSystem.Root);
            settings.Verify();
        }
    }
}
