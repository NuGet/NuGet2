using System;
using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.VisualStudio.Test {
    [TestClass]
    public class VsProjectSystemTest {
        [TestMethod]
        public void GetPropertyValueUnknownPropertyReturnsNull() {
            // Arrange
            VsProjectSystem projectSystem = new VsProjectSystem(TestUtils.GetProject("Name"));

            // Assert
            var value = projectSystem.GetPropertyValue("notexist");

            // Assert
            Assert.IsNull(value);
        }

        [TestMethod]
        public void GetPropertyValueThrowsArgumentExceptionReturnsNull() {
            // Vs throws an argument exception when trying to index into an invalid property

            // Arrange
            Project project = TestUtils.GetProject("Name",
                                                   propertyGetter: name => { throw new ArgumentException(); });
            VsProjectSystem projectSystem = new VsProjectSystem(project);

            // Assert
            var value = projectSystem.GetPropertyValue("notexist");

            // Assert
            Assert.IsNull(value);
        }
    }
}
