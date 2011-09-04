using Xunit;

namespace NuGet.VisualStudio.Test {
    
    public class PathValidatorTest {
        [Fact]
        public void IsValidFilePathReturnsTrueForValidLocalPath() {
            // Act
            var isValid = PathValidator.IsValidLocalPath(@"C:\Path\To\Source");

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsValidUncPathReturnsTrueForValidUncPath() {
            // Act
            var isValid = PathValidator.IsValidUncPath(@"\\server\share\Path\To\Source");

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsValidFilePathReturnsFalseForInvalidLocalPathFormat() {
            // Act
            var isValid = PathValidator.IsValidLocalPath(@"ABC:\Path\To\Source");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidFilePathReturnsFalseForRelativePath() {
            // Act
            var isValid = PathValidator.IsValidLocalPath(@"Path\To\Source");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidFilePathReturnsFalseForRelativePathWithDriveLetter() {
            // Act
            var isValid = PathValidator.IsValidLocalPath(@"C:Path\To\Source");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidFilePathReturnsFalseForAbsolutePathWithoutDriveLetter() {
            // Act
            var isValid = PathValidator.IsValidLocalPath(@"\Path\To\Source");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidUncPathReturnsFalseForInvalidUncPathMissingShare() {
            // Act
            var isValid = PathValidator.IsValidUncPath(@"\\server\");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidUncPathReturnsFalseForInvalidUncPathMissingDoubleBackslash() {
            // Act
            var isValid = PathValidator.IsValidUncPath(@"\server");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidFilePathReturnsFalseForInvalidLocalPathBadCharacters() {
            // Act
            var isValid = PathValidator.IsValidLocalPath(@"C:\Path\*\:\""\Source");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidUncPathReturnsFalseForInvalidUncPathBadCharacters() {
            // Act
            var isValid = PathValidator.IsValidUncPath(@"\\server\share\*\:\""\Source");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidUrlReturnsTrueForValidUrl() {
            // Act
            var isValid = PathValidator.IsValidUrl(@"http://go.microsoft.com/fwlink/?LinkID=199193");

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsValidUrlReturnsTrueForValidHttpsUrl() {
            // Act
            var isValid = PathValidator.IsValidUrl(@"https://go.microsoft.com/fwlink/?LinkID=199193");

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsValidUrlReturnsTrueForValidUrlUsingIpAddress() {
            // Act
            var isValid = PathValidator.IsValidUrl(@"http://127.0.0.1/mysource");

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsValidUrlReturnsTrueForValidUrlUsingPorts() {
            // Act
            var isValid = PathValidator.IsValidUrl(@"http://127.0.0.1:8080/mysource");

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsValidUrlReturnsFalseForInvalidUrl() {
            // Act
            var isValid = PathValidator.IsValidUrl(@"http::/\/go.microsoft.com/fw&link/?LinkID=199193");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidUrlReturnsFalseForInvalidUrlAsLocalPath() {
            // Act
            var isValid = PathValidator.IsValidUrl(@"C:\Path\To\Source");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidUrlReturnsFalseForInvalidUrlAsUncPath() {
            // Act
            var isValid = PathValidator.IsValidUrl(@"\\server\share");

            // Assert
            Assert.False(isValid);
        }
    }
}
