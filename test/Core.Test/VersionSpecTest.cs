using Xunit;

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
    }
}
