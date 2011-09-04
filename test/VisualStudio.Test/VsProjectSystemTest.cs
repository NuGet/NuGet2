using System;
using EnvDTE;
using Xunit;

namespace NuGet.VisualStudio.Test {
    
    public class VsProjectSystemTest {
        [Fact]
        public void GetPropertyValueUnknownPropertyReturnsNull() {
            // Arrange
            VsProjectSystem projectSystem = new VsProjectSystem(TestUtils.GetProject("Name"));

            // Assert
            var value = projectSystem.GetPropertyValue("notexist");

            // Assert
            Assert.Null(value);
        }

        [Fact]
        public void GetPropertyValueThrowsArgumentExceptionReturnsNull() {
            // Vs throws an argument exception when trying to index into an invalid property

            // Arrange
            Project project = TestUtils.GetProject("Name",
                                                   propertyGetter: name => { throw new ArgumentException(); });
            VsProjectSystem projectSystem = new VsProjectSystem(project);

            // Assert
            var value = projectSystem.GetPropertyValue("notexist");

            // Assert
            Assert.Null(value);
        }
    }
}
