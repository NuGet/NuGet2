using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.VisualStudio.Test {
    [TestClass]
    public class PathValidatorTest {

        [TestMethod]
        public void IsValidFilePathReturnsTrueForValidLocalPath() {
            // Act
            var isValid = PathValidator.IsValidLocalPath(@"C:\Path\To\Source");

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void IsValidUncPathReturnsTrueForValidUncPath() {
            // Act
            var isValid = PathValidator.IsValidUncPath(@"\\server\share\Path\To\Source");

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void IsValidFilePathReturnsFalseForInvalidLocalPathFormat() {
            // Act
            var isValid = PathValidator.IsValidLocalPath(@"ABC:\Path\To\Source");

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void IsValidFilePathReturnsFalseForRelativePath() {
            // Act
            var isValid = PathValidator.IsValidLocalPath(@"Path\To\Source");

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void IsValidFilePathReturnsFalseForAbsolutePathWithoutDriveLetter() {
            // Act
            var isValid = PathValidator.IsValidLocalPath(@"\Path\To\Source");

            // Assert
            Assert.IsFalse(isValid);
        }
        [TestMethod]
        public void IsValidUncPathReturnsFalseForInvalidUncPathMissingShare() {
            // Act
            var isValid = PathValidator.IsValidUncPath(@"\\server\");

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void IsValidFilePathReturnsFalseForInvalidLocalPathBadCharacters() {
            // Act
            var isValid = PathValidator.IsValidLocalPath(@"C:\Path\*\:\""\Source");

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void IsValidUncPathReturnsFalseForInvalidUncPathBadCharacters() {
            // Act
            var isValid = PathValidator.IsValidUncPath(@"\\server\share\*\:\""\Source");

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void IsValidUrlReturnsTrueForValidUrl() {
            // Act
            var isValid = PathValidator.IsValidUrl(@"http://go.microsoft.com/fwlink/?LinkID=199193");

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void IsValidUrlReturnsTrueForValidHttpsUrl() {
            // Act
            var isValid = PathValidator.IsValidUrl(@"https://go.microsoft.com/fwlink/?LinkID=199193");

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void IsValidUrlReturnsTrueForValidUrlUsingIpAddress() {
            // Act
            var isValid = PathValidator.IsValidUrl(@"http://127.0.0.1/mysource");

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void IsValidUrlReturnsTrueForValidUrlUsingPorts() {
            // Act
            var isValid = PathValidator.IsValidUrl(@"http://127.0.0.1:8080/mysource");

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void IsValidUrlReturnsFalseForInvalidUrl() {
            // Act
            var isValid = PathValidator.IsValidUrl(@"http::/\/go.microsoft.com/fw&link/?LinkID=199193");

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void IsValidUrlReturnsFalseForInvalidUrlAsLocalPath() {
            // Act
            var isValid = PathValidator.IsValidUrl(@"C:\Path\To\Source");

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void IsValidUrlReturnsFalseForInvalidUrlAsUncPath() {
            // Act
            var isValid = PathValidator.IsValidUrl(@"\\server\share");

            // Assert
            Assert.IsFalse(isValid);
        }

    }
}
