using Xunit;

namespace NuGet.Test.NuGetCommandLine
{

    public class OptionAttributeTests
    {
        [Fact]
        public void GetDescription_ReturnsResourceIfTypeSet()
        {
            // Arrange
            OptionAttribute cmd = new OptionAttribute(typeof(MockResourceType), "ResourceName");

            // Act
            var actual = cmd.Description;

            // Assert
            Assert.Equal("This is a Resource String.", actual);
        }

        [Fact]
        public void GetDescription_ReturnsDescriptionIfTypeNotSet()
        {
            // Arrange
            OptionAttribute cmd = new OptionAttribute("ResourceName");

            // Act
            var actual = cmd.Description;

            // Assert
            Assert.Equal("ResourceName", actual);
        }
    }
}
