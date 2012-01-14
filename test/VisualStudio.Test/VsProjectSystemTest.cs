using System;
using EnvDTE;
using Xunit;
using Moq;
using NuGet.Test.Mocks;

namespace NuGet.VisualStudio.Test
{

    public class VsProjectSystemTest
    {
        [Fact]
        public void GetPropertyValueUnknownPropertyReturnsNull()
        {
            // Arrange
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();
            mockFileSystemProvider.Setup(c => c.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
            VsProjectSystem projectSystem = new VsProjectSystem(TestUtils.GetProject("Name"), mockFileSystemProvider.Object);

            // Assert
            var value = projectSystem.GetPropertyValue("notexist");

            // Assert
            Assert.Null(value);
        }

        [Fact]
        public void GetPropertyValueThrowsArgumentExceptionReturnsNull()
        {
            // Vs throws an argument exception when trying to index into an invalid property

            // Arrange
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();
            mockFileSystemProvider.Setup(c => c.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
            Project project = TestUtils.GetProject("Name",
                                                   propertyGetter: name => { throw new ArgumentException(); });
            VsProjectSystem projectSystem = new VsProjectSystem(project, mockFileSystemProvider.Object);

            // Assert
            var value = projectSystem.GetPropertyValue("notexist");

            // Assert
            Assert.Null(value);
        }
    }
}
