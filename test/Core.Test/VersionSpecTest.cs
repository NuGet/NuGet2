using Xunit;
using Xunit.Extensions;

namespace NuGet.Test
{

    public class VersionSpecTest
    {
        [Fact]
        public void ToStringExactVersion()
        {
            // Arrange
            var spec = new VersionSpec
            {
                IsMaxInclusive = true,
                IsMinInclusive = true,
                MaxVersion = new SemanticVersion("1.0"),
                MinVersion = new SemanticVersion("1.0"),
            };

            // Act
            string value = spec.ToString();

            // Assert
            Assert.Equal("[1.0]", value);
        }

        [Fact]
        public void ToStringMinVersionInclusive()
        {
            // Arrange
            var spec = new VersionSpec
            {
                IsMinInclusive = true,
                MinVersion = new SemanticVersion("1.0"),
            };

            // Act
            string value = spec.ToString();

            // Assert
            Assert.Equal("1.0", value);
        }

        [Fact]
        public void ToStringMinVersionExclusive()
        {
            // Arrange
            var spec = new VersionSpec
            {
                IsMinInclusive = false,
                MinVersion = new SemanticVersion("1.0"),
            };

            // Act
            string value = spec.ToString();

            // Assert
            Assert.Equal("(1.0, )", value);
        }

        [Fact]
        public void ToStringMaxVersionInclusive()
        {
            // Arrange
            var spec = new VersionSpec
            {
                IsMaxInclusive = true,
                MaxVersion = new SemanticVersion("1.0"),
            };

            // Act
            string value = spec.ToString();

            // Assert
            Assert.Equal("(, 1.0]", value);
        }

        [Fact]
        public void ToStringMaxVersionExclusive()
        {
            // Arrange
            var spec = new VersionSpec
            {
                IsMaxInclusive = false,
                MaxVersion = new SemanticVersion("1.0"),
            };

            // Act
            string value = spec.ToString();

            // Assert
            Assert.Equal("(, 1.0)", value);
        }

        [Fact]
        public void ToStringMinVersionExclusiveMaxInclusive()
        {
            // Arrange
            var spec = new VersionSpec
            {
                IsMaxInclusive = true,
                IsMinInclusive = false,
                MaxVersion = new SemanticVersion("3.0"),
                MinVersion = new SemanticVersion("1.0"),
            };

            // Act
            string value = spec.ToString();

            // Assert
            Assert.Equal("(1.0, 3.0]", value);
        }

        [Fact]
        public void ToStringMinVersionInclusiveMaxExclusive()
        {
            // Arrange
            var spec = new VersionSpec
            {
                IsMaxInclusive = false,
                IsMinInclusive = true,
                MaxVersion = new SemanticVersion("4.0"),
                MinVersion = new SemanticVersion("1.0"),
            };

            // Act
            string value = spec.ToString();

            // Assert
            Assert.Equal("[1.0, 4.0)", value);
        }

        [Fact]
        public void ToStringMinVersionInclusiveMaxInclusive()
        {
            // Arrange
            var spec = new VersionSpec
            {
                IsMaxInclusive = true,
                IsMinInclusive = true,
                MaxVersion = new SemanticVersion("5.0"),
                MinVersion = new SemanticVersion("1.0"),
            };

            // Act
            string value = spec.ToString();

            // Assert
            Assert.Equal("[1.0, 5.0]", value);
        }

        [Fact]
        public void ToStringMinVersionExclusiveMaxExclusive()
        {
            // Arrange
            var spec = new VersionSpec
            {
                IsMaxInclusive = false,
                IsMinInclusive = false,
                MaxVersion = new SemanticVersion("5.0"),
                MinVersion = new SemanticVersion("1.0"),
            };

            // Act
            string value = spec.ToString();

            // Assert
            Assert.Equal("(1.0, 5.0)", value);
        }

        [Theory]
        [InlineData("1.0.0", "2.0.0", "(1.0.0, 2.0.0)")]
        [InlineData("1.0.0+meta", "2.0.0+meta2", "(1.0.0, 2.0.0)")]
        [InlineData("1.0.0-beta.1", "2.0.0-10", "(1.0.0-beta.1, 2.0.0-10)")]
        public void ToStringSemVer200(string min, string max, string expected)
        {
            // Arrange
            var spec = new VersionSpec
            {
                IsMaxInclusive = false,
                IsMinInclusive = false,
                MaxVersion = new SemanticVersion(max),
                MinVersion = new SemanticVersion(min),
            };

            // Act
            string value = spec.ToString();

            // Assert
            Assert.Equal(expected, value);
        }

        [Theory]
        [InlineData("[1]", "[1.0]")]
        [InlineData("[4, 5]", "[4.0, 5.0]")]
        public void SingleDigitVersionsInRanges(string range, string expected)
        {
            // Arrange
            IVersionSpec spec;

            // Act
            var b = VersionUtility.TryParseVersionSpec(range, out spec);

            // Assert
            Assert.Equal(true, b);
            Assert.Equal(expected, spec.ToString());
        }
    }
}
