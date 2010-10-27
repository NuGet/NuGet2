using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class FileBasedProjectSystemTest {
        [TestMethod]
        public void GetRootNamespaceReturnsEmptyString() {
            // Arrange
            var projectSystem = new FileBasedProjectSystem(@"c:\test");

            // Act
            string ns = projectSystem.GetPropertyValue("rootnamespace");

            // Assert
            Assert.AreEqual(String.Empty, ns);
        }

        [TestMethod]
        public void GetNullPropertyValueReturnsNull() {
            // Arrange
            var projectSystem = new FileBasedProjectSystem(@"c:\test");

            // Act
            string value = projectSystem.GetPropertyValue(null);

            // Assert
            Assert.IsNull(value);
        }
    }
}
