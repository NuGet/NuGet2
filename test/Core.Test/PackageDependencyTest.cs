using Xunit;

namespace NuGet.Test
{

    public class PackageDependencyTest
    {
        [Fact]
        public void ToStringExactVersion()
        {
            // Arrange
            PackageDependency dependency = PackageDependency.CreateDependency("A", "[1.0]");

            // Act
            string value = dependency.ToString();

            // Assert
            Assert.Equal("A (= 1.0)", value);
        }

        [Fact]
        public void ToStringMinVersionInclusive()
        {
            // Arrange
            PackageDependency dependency = PackageDependency.CreateDependency("A", "1.0");

            // Act
            string value = dependency.ToString();

            // Assert
            Assert.Equal("A (\u2265 1.0)", value);
        }

        [Fact]
        public void ToStringMinVersionExclusive()
        {
            // Arrange
            PackageDependency dependency = PackageDependency.CreateDependency("A", "(1.0,)");

            // Act
            string value = dependency.ToString();

            // Assert
            Assert.Equal("A (> 1.0)", value);
        }

        [Fact]
        public void ToStringMaxVersionInclusive()
        {
            // Arrange
            PackageDependency dependency = PackageDependency.CreateDependency("A", "[,1.0]");

            // Act
            string value = dependency.ToString();

            // Assert
            Assert.Equal("A (\u2264 1.0)", value);
        }

        [Fact]
        public void ToStringMaxVersionExclusive()
        {
            // Arrange
            PackageDependency dependency = PackageDependency.CreateDependency("A", "[,1.0)");

            // Act
            string value = dependency.ToString();

            // Assert
            Assert.Equal("A (< 1.0)", value);
        }

        [Fact]
        public void ToStringMinVersionExclusiveMaxInclusive()
        {
            // Arrange
            PackageDependency dependency = PackageDependency.CreateDependency("A", "(1.0,5.0]");

            // Act
            string value = dependency.ToString();

            // Assert
            Assert.Equal("A (> 1.0 && \u2264 5.0)", value);
        }

        [Fact]
        public void ToStringMinVersionInclusiveMaxExclusive()
        {
            // Arrange
            PackageDependency dependency = PackageDependency.CreateDependency("A", "[1.0,5.0)");

            // Act
            string value = dependency.ToString();

            // Assert
            Assert.Equal("A (\u2265 1.0 && < 5.0)", value);
        }

        [Fact]
        public void ToStringMinVersionInclusiveMaxInclusive()
        {
            // Arrange
            PackageDependency dependency = PackageDependency.CreateDependency("A", "[1.0,5.0]");

            // Act
            string value = dependency.ToString();

            // Assert
            Assert.Equal("A (\u2265 1.0 && \u2264 5.0)", value);
        }

        [Fact]
        public void ToStringMinVersionExclusiveMaxExclusive()
        {
            // Arrange
            PackageDependency dependency = PackageDependency.CreateDependency("A", "(1.0,5.0)");

            // Act
            string value = dependency.ToString();

            // Assert
            Assert.Equal("A (> 1.0 && < 5.0)", value);
        }
    }
}
