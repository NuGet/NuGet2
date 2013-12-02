using System;
using Xunit;
using Xunit.Extensions;

namespace NuGet.VisualStudio.Test
{

    public class PathValidatorTest
    {
        [Fact]
        public void IsValidFilePathReturnsTrueForValidLocalPath()
        {
            if (Environment.OSVersion.Platform == PlatformID.MacOSX ||
                Environment.OSVersion.Platform == PlatformID.Unix)
            {
                // Act
                var isValid = PathValidator.IsValidLocalPath("/Path/To/Source");

                // Assert
                Assert.True(isValid);
            }
            else
            {
                // Act
                var isValid = PathValidator.IsValidLocalPath(@"C:\Path\To\Source");

                // Assert
                Assert.True(isValid);
            }
        }

        [Fact]
        public void IsValidUncPathReturnsTrueForValidUncPath()
        {
            if (Environment.OSVersion.Platform == PlatformID.MacOSX ||
                Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return;
            }

            // Act
            var isValid = PathValidator.IsValidUncPath(@"\\server\share\Path\To\Source");

            // Assert
            Assert.True(isValid);
        }

        [Theory]
        [InlineData(new object[] { @"ABC:\Path\To\Source" })]
        [InlineData(new object[] { @"C:\Path\<\Source" })]
        public void IsValidFilePathReturnsFalseForInvalidLocalPathFormat(string path)
        {
            if (Environment.OSVersion.Platform == PlatformID.MacOSX ||
                Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return;
            }

            // Act
            var isValid = PathValidator.IsValidLocalPath(path);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidFilePathReturnsFalseForRelativePath()
        {
            var path = @"Path\To\Source";
            if (Environment.OSVersion.Platform == PlatformID.MacOSX ||
                Environment.OSVersion.Platform == PlatformID.Unix)
            {
                path = path.Replace('\\', '/');
            }

            // Act
            var isValid = PathValidator.IsValidLocalPath(path);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidFilePathReturnsFalseForRelativePathWithDriveLetter()
        {
            if (Environment.OSVersion.Platform == PlatformID.MacOSX ||
                Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return;
            }

            // Act
            var isValid = PathValidator.IsValidLocalPath(@"C:Path\To\Source");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidFilePathReturnsFalseForAbsolutePathWithoutDriveLetterOnWindows()
        {
            if (Environment.OSVersion.Platform == PlatformID.MacOSX ||
                Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return;
            }

            // Act
            var isValid = PathValidator.IsValidLocalPath(@"\Path\To\Source");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidUncPathReturnsFalseForInvalidUncPathMissingShare()
        {
            if (Environment.OSVersion.Platform == PlatformID.MacOSX ||
                Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return;
            }

            // Act
            var isValid = PathValidator.IsValidUncPath(@"\\server\");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidUncPathReturnsFalseForInvalidUncPathMissingDoubleBackslash()
        {
            if (Environment.OSVersion.Platform == PlatformID.MacOSX ||
                Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return;
            }

            // Act
            var isValid = PathValidator.IsValidUncPath(@"\server");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidFilePathReturnsFalseForInvalidLocalPathBadCharacters()
        {
            var path = @"C:\Path\*\:\""\Source";
            if (Environment.OSVersion.Platform == PlatformID.MacOSX ||
                Environment.OSVersion.Platform == PlatformID.Unix)
            {
                path = @"/Path/*/:/""/Source";
            }

            // Act
            var isValid = PathValidator.IsValidLocalPath(path);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidUncPathReturnsFalseForInvalidUncPathBadCharacters()
        {
            if (Environment.OSVersion.Platform == PlatformID.MacOSX ||
                Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return;
            }

            // Act
            var isValid = PathValidator.IsValidUncPath(@"\\server\share\*\:\""\Source");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidUrlReturnsTrueForValidUrl()
        {
            // Act
            var isValid = PathValidator.IsValidUrl(@"http://go.microsoft.com/fwlink/?LinkID=199193");

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsValidUrlReturnsTrueForValidHttpsUrl()
        {
            // Act
            var isValid = PathValidator.IsValidUrl(@"https://go.microsoft.com/fwlink/?LinkID=199193");

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsValidUrlReturnsTrueForValidUrlUsingIpAddress()
        {
            // Act
            var isValid = PathValidator.IsValidUrl(@"http://127.0.0.1/mysource");

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsValidUrlReturnsTrueForValidUrlUsingPorts()
        {
            // Act
            var isValid = PathValidator.IsValidUrl(@"http://127.0.0.1:8080/mysource");

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsValidUrlReturnsFalseForInvalidUrl()
        {
            // Act
            var isValid = PathValidator.IsValidUrl(@"http::/\/go.microsoft.com/fw&link/?LinkID=199193");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidUrlReturnsFalseForInvalidUrlAsLocalPath()
        {
            // Act
            var isValid = PathValidator.IsValidUrl(@"C:\Path\To\Source");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidUrlReturnsFalseForInvalidUrlAsUncPath()
        {
            // Act
            var isValid = PathValidator.IsValidUrl(@"\\server\share");

            // Assert
            Assert.False(isValid);
        }
    }
}
