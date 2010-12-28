using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class VersionSpecTest {
        [TestMethod]
        public void ToStringExactVersion() {
            // Arrange
            var spec = new VersionSpec {
                IsMaxInclusive = true,
                IsMinInclusive = true,
                MaxVersion = new Version("1.0"),
                MinVersion = new Version("1.0"),
            };

            // Act
            string value = spec.ToString();

            // Assert
            Assert.AreEqual("[1.0]", value);
        }

        [TestMethod]
        public void ToStringMinVersionInclusive() {
            // Arrange
            var spec = new VersionSpec {
                IsMinInclusive = true,
                MinVersion = new Version("1.0"),
            };

            // Act
            string value = spec.ToString();

            // Assert
            Assert.AreEqual("1.0", value);
        }

        [TestMethod]
        public void ToStringMinVersionExclusive() {
            // Arrange
            var spec = new VersionSpec {
                IsMinInclusive = false,
                MinVersion = new Version("1.0"),
            };

            // Act
            string value = spec.ToString();

            // Assert
            Assert.AreEqual("(1.0, )", value);
        }

        [TestMethod]
        public void ToStringMaxVersionInclusive() {
            // Arrange
            var spec = new VersionSpec {
                IsMaxInclusive = true,
                MaxVersion = new Version("1.0"),
            };

            // Act
            string value = spec.ToString();

            // Assert
            Assert.AreEqual("(, 1.0]", value);
        }

        [TestMethod]
        public void ToStringMaxVersionExclusive() {
            // Arrange
            var spec = new VersionSpec {
                IsMaxInclusive = false,
                MaxVersion = new Version("1.0"),
            };

            // Act
            string value = spec.ToString();

            // Assert
            Assert.AreEqual("(, 1.0)", value);
        }

        [TestMethod]
        public void ToStringMinVersionExclusiveMaxInclusive() {
            // Arrange
            var spec = new VersionSpec {
                IsMaxInclusive = true,
                IsMinInclusive = false,
                MaxVersion = new Version("3.0"),
                MinVersion = new Version("1.0"),
            };

            // Act
            string value = spec.ToString();

            // Assert
            Assert.AreEqual("(1.0, 3.0]", value);
        }

        [TestMethod]
        public void ToStringMinVersionInclusiveMaxExclusive() {
            // Arrange
            var spec = new VersionSpec {
                IsMaxInclusive = false,
                IsMinInclusive = true,
                MaxVersion = new Version("4.0"),
                MinVersion = new Version("1.0"),
            };

            // Act
            string value = spec.ToString();

            // Assert
            Assert.AreEqual("[1.0, 4.0)", value);
        }

        [TestMethod]
        public void ToStringMinVersionInclusiveMaxInclusive() {
            // Arrange
            var spec = new VersionSpec {
                IsMaxInclusive = true,
                IsMinInclusive = true,
                MaxVersion = new Version("5.0"),
                MinVersion = new Version("1.0"),
            };

            // Act
            string value = spec.ToString();

            // Assert
            Assert.AreEqual("[1.0, 5.0]", value);
        }

        [TestMethod]
        public void ToStringMinVersionExclusiveMaxExclusive() {
            // Arrange
            var spec = new VersionSpec {
                IsMaxInclusive = false,
                IsMinInclusive = false,
                MaxVersion = new Version("5.0"),
                MinVersion = new Version("1.0"),
            };

            // Act
            string value = spec.ToString();

            // Assert
            Assert.AreEqual("(1.0, 5.0)", value);
        }
    }
}
