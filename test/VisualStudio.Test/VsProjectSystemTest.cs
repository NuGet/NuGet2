using System;
using EnvDTE;
using Moq;
using Xunit;
using NuGet.Test.Mocks;
using Xunit.Extensions;

namespace NuGet.VisualStudio.Test
{
    public class VsProjectSystemTest
    {
        [Fact]
        public void GetPropertyValueUnknownPropertyReturnsNull()
        {
            // Arrange
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();
            mockFileSystemProvider.Setup(fs => fs.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
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
            mockFileSystemProvider.Setup(fs => fs.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
            Project project = TestUtils.GetProject("Name",
                                                   propertyGetter: name => { throw new ArgumentException(); });
            VsProjectSystem projectSystem = new VsProjectSystem(project, mockFileSystemProvider.Object);

            // Assert
            var value = projectSystem.GetPropertyValue("notexist");

            // Assert
            Assert.Null(value);
        }

        [Theory]
        [InlineData("web.config")]
        [InlineData("web.DEBUG.CONFIG")]
        [InlineData("Web.release.Config")]
        [InlineData("Web.aaaa.config")]
        public void IsSupportedFileMethodRejectsAllVariationsOfWebConfigFile(string filePath)
        {
            // Arrange
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();
            mockFileSystemProvider.Setup(fs => fs.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
            Project project = TestUtils.GetProject("TestProject");
            VsProjectSystem projectSystem = new VsProjectSystem(project, mockFileSystemProvider.Object);

            // Act
            bool supported = projectSystem.IsSupportedFile(filePath);

            // Assert
            Assert.False(supported);
        }

        [Theory]
        [InlineData(".NetFramework, Version=1.0")]
        [InlineData(".NetCompact, Version=2.0, Profile=Client")]
        public void NonSilverlightProjectSupportsBindingRedirect(string targetFramework)
        {
            // Arrange
            var silverlightProject = TestUtils.GetProject(
                "Silverlight", 
                propertyGetter: name => GetTargetFrameworkProperty("TargetFrameworkMoniker", targetFramework));
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();
            mockFileSystemProvider.Setup(fs => fs.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
            var projectSystem = new VsProjectSystem(silverlightProject, mockFileSystemProvider.Object);

            // Act
            bool bindingRedirectSupported = projectSystem.IsBindingRedirectSupported;

            // Assert
            Assert.True(bindingRedirectSupported);
        }

        [Theory]
        [InlineData("Silverlight, Version=1.0")]
        [InlineData("Silverlight, Version=2.0, Profile=Phone")]
        public void SilverlightProjectDoesNotSupportsBindingRedirect(string targetFramework)
        {
            // Arrange
            var silverlightProject = TestUtils.GetProject(
                "Silverlight",
                propertyGetter: name => GetTargetFrameworkProperty("TargetFrameworkMoniker", targetFramework));
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();
            mockFileSystemProvider.Setup(fs => fs.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
            var projectSystem = new VsProjectSystem(silverlightProject, mockFileSystemProvider.Object);

            // Act
            bool bindingRedirectSupported = projectSystem.IsBindingRedirectSupported;

            // Assert
            Assert.False(bindingRedirectSupported);
        }

        private Property GetTargetFrameworkProperty(string name, string targetFramework)
        {
            if (name == "TargetFrameworkMoniker")
            {
                var property = new Mock<Property>();
                property.Setup(p => p.Name).Returns(name);
                property.Setup(p => p.Value).Returns(targetFramework);
                return property.Object;
            }

            return null;
        }
    }
}