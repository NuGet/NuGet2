using System.IO;
using System.Linq;
using Moq;
using NuGet.Test.Mocks;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test
{
    public class PreprocessorTest
    {
        [Fact]
        public void TransformFileReplacesTokensWithValueAndReturnsModifiedStream()
        {
            // Arrange
            var processor = new Preprocessor();
            var mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };
            mockProjectSystem.Setup(m => m.GetPropertyValue("token")).Returns("token value");
            var mockFile = new Mock<IPackageFile>();
            mockFile.Setup(m => m.Path).Returns("foo.bar.pp");
            mockFile.Setup(m => m.GetStream()).Returns(() => GetStream("test $token$"));

            // Act
            processor.TransformFile(mockFile.Object, "foo.bar", mockProjectSystem.Object);

            // Assert
            Assert.True(mockProjectSystem.Object.FileExists("foo.bar"));
            Assert.Equal("test token value", mockProjectSystem.Object.OpenFile("foo.bar").ReadToEnd());
        }

        [Fact]
        public void TransformFileDoesNothingIfFileExists()
        {
            // Arrange
            var processor = new Preprocessor();
            var mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };
            mockProjectSystem.Setup(m => m.GetPropertyValue("token")).Returns("token value");
            mockProjectSystem.Object.AddFile("foo.bar", GetStream("hello"));
            var mockFile = new Mock<IPackageFile>();
            mockFile.Setup(m => m.Path).Returns("foo.bar.pp");
            mockFile.Setup(m => m.GetStream()).Returns(() => GetStream("test $token$"));

            // Act
            processor.TransformFile(mockFile.Object, "foo.bar", mockProjectSystem.Object);

            // Assert            
            Assert.Equal("hello", mockProjectSystem.Object.OpenFile("foo.bar").ReadToEnd());
        }

        [Theory]
        [InlineData(FileConflictResolution.Overwrite)]
        [InlineData(FileConflictResolution.OverwriteAll)]
        public void TransformFileOverrideIfFileExistsAndLoggerReturnsOverride(FileConflictResolution resolution)
        {
            // Arrange
            var logger = new Mock<ILogger>();
            logger.Setup(l => l.ResolveFileConflict(It.IsAny<string>())).Returns(resolution);

            var processor = new Preprocessor();
            var mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };
            mockProjectSystem.Setup(m => m.GetPropertyValue("token")).Returns("token value");
            mockProjectSystem.Object.AddFile("foo.bar", GetStream("hello"));
            mockProjectSystem.Setup(m => m.Logger).Returns(logger.Object);
            var mockFile = new Mock<IPackageFile>();
            mockFile.Setup(m => m.Path).Returns("foo.bar.pp");
            mockFile.Setup(m => m.GetStream()).Returns(() => GetStream("test $token$"));

            // Act
            processor.TransformFile(mockFile.Object, "foo.bar", mockProjectSystem.Object);

            // Assert            
            Assert.Equal("test token value", mockProjectSystem.Object.OpenFile("foo.bar").ReadToEnd());
        }

        [Theory]
        [InlineData(FileConflictResolution.Ignore)]
        [InlineData(FileConflictResolution.IgnoreAll)]
        public void TransformFileOverrideIfFileExistsAndLoggerReturnsIgnore(FileConflictResolution resolution)
        {
            // Arrange
            var logger = new Mock<ILogger>();
            logger.Setup(l => l.ResolveFileConflict(It.IsAny<string>())).Returns(resolution);

            var processor = new Preprocessor();
            var mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };
            mockProjectSystem.Setup(m => m.GetPropertyValue("token")).Returns("token value");
            mockProjectSystem.Object.AddFile("foo.bar", GetStream("hello"));
            mockProjectSystem.Setup(m => m.Logger).Returns(logger.Object);
            var mockFile = new Mock<IPackageFile>();
            mockFile.Setup(m => m.Path).Returns("foo.bar.pp");
            mockFile.Setup(m => m.GetStream()).Returns(() => GetStream("test $token$"));

            // Act
            processor.TransformFile(mockFile.Object, "foo.bar", mockProjectSystem.Object);

            // Assert            
            Assert.Equal("hello", mockProjectSystem.Object.OpenFile("foo.bar").ReadToEnd());
        }

        [Fact]
        public void RevertFileRemovesFileIfContentIsTheSame()
        {
            // Arrange
            var processor = new Preprocessor();
            var mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };
            mockProjectSystem.Setup(m => m.GetPropertyValue("token")).Returns("token value");
            mockProjectSystem.Object.AddFile("foo.bar", GetStream("test token value"));
            var mockFile = new Mock<IPackageFile>();
            mockFile.Setup(m => m.Path).Returns("foo.bar.pp");
            mockFile.Setup(m => m.GetStream()).Returns(() => GetStream("test $token$"));

            // Act
            processor.RevertFile(mockFile.Object, "foo.bar", Enumerable.Empty<IPackageFile>(), mockProjectSystem.Object);

            // Assert            
            Assert.True(mockProjectSystem.Object.Deleted.Contains(Path.Combine(mockProjectSystem.Object.Root, "foo.bar")));
        }

        private Stream GetStream(string content)
        {
            return content.AsStream();
        }
    }
}
