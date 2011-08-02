using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test.NuGetCommandLine {
    [TestClass]
    public class CommandAttributeTests {
        [TestMethod]
        public void GetDescription_ReturnsResourceIfTypeSet() {
            // Arrange
            CommandAttribute cmd = new CommandAttribute(typeof(MockResourceType), "MockCommand", "ResourceName");

            //Act
            var actual = cmd.Description;

            // Assert
            Assert.AreEqual("This is a Resource String.", actual);
        }

        [TestMethod]
        public void GetDescription_ReturnsDescriptionIfTypeNotSet() {
            // Arrange
            CommandAttribute cmd = new CommandAttribute("MockCommand", "ResourceName");

            //Act
            var actual = cmd.Description;

            // Assert
            Assert.AreEqual("ResourceName", actual);
        }

        [TestMethod]
        public void GetUsageSummary_ReturnsResourceIfTypeSet() {
            // Arrange
            CommandAttribute cmd = new CommandAttribute(typeof(MockResourceType),
                "MockCommand", "Description") { UsageSummary = "Not a Resource", UsageSummaryResourceName = "ResourceName" };

            //Act
            var actual = cmd.UsageSummary;

            // Assert
            Assert.AreEqual("This is a Resource String.", actual);
        }

        [TestMethod]
        public void GetUsageSummary_ReturnsUsageSummaryIfTypeNotSet() {
            // Arrange
            CommandAttribute cmd = new CommandAttribute(
                "MockCommand", "Description") { UsageSummary = "Not a Resource", UsageSummaryResourceName = "ResourceName" };

            //Act
            var actual = cmd.UsageSummary;

            // Assert
            Assert.AreEqual("Not a Resource", actual);
        }

        [TestMethod]
        public void GetUsageDescription_ReturnsResourceIfTypeSet() {
            // Arrange
            CommandAttribute cmd = new CommandAttribute(typeof(MockResourceType),
                "MockCommand", "Description") { UsageDescription = "Not a Resource", UsageDescriptionResourceName = "ResourceName" };

            //Act
            var actual = cmd.UsageDescription;

            // Assert
            Assert.AreEqual("This is a Resource String.", actual);
        }

        [TestMethod]
        public void GetUsageDescription_ReturnsUsageDescriptionIfTypeNotSet() {
            // Arrange
            CommandAttribute cmd = new CommandAttribute(
                "MockCommand", "Description") { UsageDescription = "Not a Resource", UsageDescriptionResourceName = "ResourceName" };

            //Act
            var actual = cmd.UsageDescription;

            // Assert
            Assert.AreEqual("Not a Resource", actual);
        }


        public class MockResourceType {
            public static string ResourceName {
                get { return "This is a Resource String."; }
            }
        }
    }
}
