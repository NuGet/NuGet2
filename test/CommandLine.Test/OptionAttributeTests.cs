using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test.NuGetCommandLine {
    [TestClass]
    public class OptionAttributeTests {
        [TestMethod]
        public void GetDescription_ReturnsResourceIfTypeSet() {
            // Arrange
            OptionAttribute cmd = new OptionAttribute(typeof(MockResourceType), "ResourceName");

            // Act
            var actual = cmd.Description;

            // Assert
            Assert.AreEqual("This is a Resource String.", actual);
        }

        [TestMethod]
        public void GetDescription_ReturnsDescriptionIfTypeNotSet() {
            // Arrange
            OptionAttribute cmd = new OptionAttribute("ResourceName");

            // Act
            var actual = cmd.Description;

            // Assert
            Assert.AreEqual("ResourceName", actual);
        }

        private class MockResourceType {
            public static string ResourceName { get { return "This is a Resource String."; } }
        }
    }
}
