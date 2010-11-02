using System;
using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.VisualStudio.Test {
    [TestClass]
    public class ProjectSystemExtensionsTest {
        [TestMethod]
        public void GetPropertyValueThrowsArgumentExceptionReturnsNull() {
            // Arrange
            Project project = TestUtils.GetProject("Name",
                                                   propertyGetter: name => { throw new ArgumentException(); });

            // Act
            object value = project.GetPropertyValue<object>("Fake");

            // Assert
            Assert.IsNull(value);
        }
    }
}
