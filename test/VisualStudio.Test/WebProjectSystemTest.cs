using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Moq;
using NuGet.Test.Mocks;
using Xunit;
using Xunit.Extensions;

namespace NuGet.VisualStudio.Test
{
    public class WebProjectSystemTest
    {
        [Theory]
        [InlineData("app.config")]
        [InlineData("app.DEBUG.CONFIG")]
        [InlineData("App.release.Config")]
        [InlineData("APP.aaaa.config")]
        public void IsFileSupportedRejectsAllVariationsOfAppConfig(string filePath)
        {
            // Arrange
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();
            mockFileSystemProvider.Setup(fs => fs.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
            Project project = TestUtils.GetProject("TestProject");
            var projectSystem = new WebProjectSystem(project, mockFileSystemProvider.Object);

            // Act
            bool supported = projectSystem.IsSupportedFile(filePath);

            // Assert
            Assert.False(supported);
        }
    }
}
