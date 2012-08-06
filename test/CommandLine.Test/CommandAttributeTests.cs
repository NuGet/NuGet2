using Xunit;

namespace NuGet.Test.NuGetCommandLine
{
    public class CommandAttributeTests
    {
        [Fact]
        public void GetDescription_ReturnsResourceIfTypeSet()
        {
            // Arrange
            CommandAttribute cmd = new CommandAttribute(typeof(MockResourceType), "MockCommand", "ResourceName");

            //Act
            var actual = cmd.Description;

            // Assert
            Assert.Equal("This is a Resource String.", actual);
        }

        [Fact]
        public void GetDescription_ReturnsDescriptionIfTypeNotSet()
        {
            // Arrange
            CommandAttribute cmd = new CommandAttribute("MockCommand", "ResourceName");

            //Act
            var actual = cmd.Description;

            // Assert
            Assert.Equal("ResourceName", actual);
        }

        [Fact]
        public void GetUsageSummary_ReturnsResourceIfTypeSet()
        {
            // Arrange
            CommandAttribute cmd = new CommandAttribute(typeof(MockResourceType),
                "MockCommand", "Description") { UsageSummary = "Not a Resource", UsageSummaryResourceName = "ResourceName" };

            //Act
            var actual = cmd.UsageSummary;

            // Assert
            Assert.Equal("This is a Resource String.", actual);
        }

        [Fact]
        public void GetUsageSummary_ReturnsUsageSummaryIfTypeNotSet()
        {
            // Arrange
            CommandAttribute cmd = new CommandAttribute(
                "MockCommand", "Description") { UsageSummary = "Not a Resource", UsageSummaryResourceName = "ResourceName" };

            //Act
            var actual = cmd.UsageSummary;

            // Assert
            Assert.Equal("Not a Resource", actual);
        }

        [Fact]
        public void GetUsageDescription_ReturnsResourceIfTypeSet()
        {
            // Arrange
            CommandAttribute cmd = new CommandAttribute(typeof(MockResourceType),
                "MockCommand", "Description") { UsageDescription = "Not a Resource", UsageDescriptionResourceName = "ResourceName" };

            //Act
            var actual = cmd.UsageDescription;

            // Assert
            Assert.Equal("This is a Resource String.", actual);
        }

        [Fact]
        public void GetUsageDescription_ReturnsUsageDescriptionIfTypeNotSet()
        {
            // Arrange
            CommandAttribute cmd = new CommandAttribute(
                "MockCommand", "Description") { UsageDescription = "Not a Resource", UsageDescriptionResourceName = "ResourceName" };

            //Act
            var actual = cmd.UsageDescription;

            // Assert
            Assert.Equal("Not a Resource", actual);
        }
    }
}
