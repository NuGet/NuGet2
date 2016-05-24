using EnvDTE;
using Moq;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    public class JsProjectSystemTest
    {
        [Fact]
        public void DoesNotSupportBindingRedirect()
        {
            // Arrange
            var project = new Mock<Project>();
            project.Setup(s => s.Properties.Item("FullPath").Value).Returns("x:\\");

            var fileSystem = new Mock<IFileSystem>();

            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(f => f.GetFileSystem(It.IsAny<string>())).Returns(fileSystem.Object);

            var jsproject = new JsProjectSystem(project.Object, fileSystemProvider.Object);

            // Assert
            Assert.False(jsproject.IsBindingRedirectSupported);
        }

        [Fact]
        public void BeginProcessorCallsTheSameMethodFromFileSystem()
        {
            // Arrange
            string[] files = new string[0];

            var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            
            var processor = fileSystem.As<IBatchProcessor<string>>();
            processor.Setup(f => f.BeginProcessing(files, PackageAction.Install)).Verifiable();

            var fileSystemProvider = new Mock<IFileSystemProvider>(MockBehavior.Strict);
            fileSystemProvider.Setup(f => f.GetFileSystem(It.IsAny<string>(), It.IsAny<bool>())).Returns(fileSystem.Object);

            var project = new Mock<Project>();
            project.Setup(s => s.Properties.Item("FullPath").Value).Returns("x:\\");

            var projectSystem = new JsProjectSystem(project.Object, fileSystemProvider.Object);

            // Act
            projectSystem.BeginProcessing(files, PackageAction.Install);

            // Assert
            fileSystem.Verify();
        }

        [Fact]
        public void EndProcessorCallsTheSameMethodFromFileSystem()
        {
            // Arrange
            string[] files = new string[0];

            var fileSystem = new Mock<IFileSystem>();

            var processor = fileSystem.As<IBatchProcessor<string>>();
            processor.Setup(f => f.EndProcessing()).Verifiable();

            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(f => f.GetFileSystem(It.IsAny<string>(), It.IsAny<bool>())).Returns(fileSystem.Object);

            var project = new Mock<Project>();
            project.Setup(s => s.Properties.Item("FullPath").Value).Returns("x:\\");

            var projectSystem = new JsProjectSystem(project.Object, fileSystemProvider.Object);

            // Act
            projectSystem.EndProcessing();

            // Assert
            fileSystem.Verify();
        }
    }
}
