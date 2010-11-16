using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class PackageDependencyTest {       
        [TestMethod]
        public void ToStringExactVersion() {
            // Arrange
            PackageDependency dependency = PackageDependency.CreateDependency("A", "[1.0]");

            // Act
            string value = dependency.ToString();

            // Assert
            Assert.AreEqual("A (= 1.0)", value);
        }

        [TestMethod]
        public void ToStringMinVersionInclusive() {
            // Arrange
            PackageDependency dependency = PackageDependency.CreateDependency("A", "1.0");

            // Act
            string value = dependency.ToString();

            // Assert
            Assert.AreEqual("A (\u2265 1.0)", value);
        }

        [TestMethod]
        public void ToStringMinVersionExclusive() {
            // Arrange
            PackageDependency dependency = PackageDependency.CreateDependency("A", "(1.0,)");

            // Act
            string value = dependency.ToString();

            // Assert
            Assert.AreEqual("A (> 1.0)", value);
        }

        [TestMethod]
        public void ToStringMaxVersionInclusive() {
            // Arrange
            PackageDependency dependency = PackageDependency.CreateDependency("A", "[,1.0]");

            // Act
            string value = dependency.ToString();

            // Assert
            Assert.AreEqual("A (\u2264 1.0)", value);
        }

        [TestMethod]
        public void ToStringMaxVersionExclusive() {
            // Arrange
            PackageDependency dependency = PackageDependency.CreateDependency("A", "[,1.0)");

            // Act
            string value = dependency.ToString();

            // Assert
            Assert.AreEqual("A (< 1.0)", value);
        }

        [TestMethod]
        public void ToStringMinVersionExclusiveMaxInclusive() {
            // Arrange
            PackageDependency dependency = PackageDependency.CreateDependency("A", "(1.0,5.0]");

            // Act
            string value = dependency.ToString();

            // Assert
            Assert.AreEqual("A (> 1.0 && \u2264 5.0)", value);
        }

        [TestMethod]
        public void ToStringMinVersionInclusiveMaxExclusive() {
            // Arrange
            PackageDependency dependency = PackageDependency.CreateDependency("A", "[1.0,5.0)");

            // Act
            string value = dependency.ToString();

            // Assert
            Assert.AreEqual("A (\u2265 1.0 && < 5.0)", value);
        }

        [TestMethod]
        public void ToStringMinVersionInclusiveMaxInclusive() {
            // Arrange
            PackageDependency dependency = PackageDependency.CreateDependency("A", "[1.0,5.0]");

            // Act
            string value = dependency.ToString();

            // Assert
            Assert.AreEqual("A (\u2265 1.0 && \u2264 5.0)", value);
        }

        [TestMethod]
        public void ToStringMinVersionExclusiveMaxExclusive() {
            // Arrange
            PackageDependency dependency = PackageDependency.CreateDependency("A", "(1.0,5.0)");

            // Act
            string value = dependency.ToString();

            // Assert
            Assert.AreEqual("A (> 1.0 && < 5.0)", value);
        }
    }
}
