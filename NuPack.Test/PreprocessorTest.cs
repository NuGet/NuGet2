using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test.Mocks;

namespace NuGet.Test {
    [TestClass]
    public class PreprocessorTest {        
        [TestMethod]
        public void TransformFileReplacesTokensWithValueAndReturnsModifiedStream() {
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
            Assert.IsTrue(mockProjectSystem.Object.FileExists("foo.bar"));
            Assert.AreEqual("test token value", mockProjectSystem.Object.OpenFile("foo.bar").ReadToEnd());
        }

        [TestMethod]
        public void TransformFileDoesNothingIfFileExists() {
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
            Assert.AreEqual("hello", mockProjectSystem.Object.OpenFile("foo.bar").ReadToEnd());
        }

        [TestMethod]
        public void TransformFileThrowsIfTokenValueIsNull() {
            // Arrange
            var processor = new Preprocessor();
            var mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };
            var mockFile = new Mock<IPackageFile>();
            mockFile.Setup(m => m.Path).Returns("foo.bar.pp");           
            mockFile.Setup(m => m.GetStream()).Returns(() => GetStream("test $token$"));

            // Act
            ExceptionAssert.Throws<InvalidOperationException>(() => processor.TransformFile(mockFile.Object, "foo.bar", mockProjectSystem.Object), "The replacement token 'token' has no value.");
        }

        [TestMethod]
        public void RevertFileRemovesFileIfContentIsTheSame() {
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
            Assert.IsTrue(mockProjectSystem.Object.Deleted.Contains("foo.bar"));
        }

        private Stream GetStream(string content) {
            return content.AsStream();
        }
    }
}
