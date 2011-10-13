using Moq;
using Xunit;
using Xunit.Extensions;

namespace NuGet.VisualStudio.Test
{
    public class SourceControlHelperTest
    {
        [Fact]
        public void IsSourceControlDisabledReturnsFalseIfSettingsHasNoValue()
        {
            // Arrange
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetValue("solution", "disableSourceControlIntegration")).Returns("").Verifiable();

            // Act
            bool isDisabled = settings.Object.IsSourceControlDisabled();

            // Assert
            Assert.False(isDisabled);
            settings.Verify();
        }

        [Theory]
        [InlineData(new object[] { " " })]
        [InlineData(new object[] { "blah" })]
        [InlineData(new object[] { "false" })]
        public void IsSourceControlDisabledReturnsFalseIfSettingsValueIsNotBooleanTrue(string value)
        {
            // Arrange
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetValue("solution", "disableSourceControlIntegration")).Returns(value).Verifiable();

            // Act
            bool isDisabled = settings.Object.IsSourceControlDisabled();

            // Assert
            Assert.False(isDisabled);
            settings.Verify();
        }

        [Theory]
        [InlineData(new object[] { "true" })]
        [InlineData(new object[] { "True" })]
        [InlineData(new object[] { "tRuE" })]
        [InlineData(new object[] { "TRUE" })]
        public void IsSourceControlDisabledReturnsTrueIfSettingsValueIsBooleanTrue(string value)
        {
            // Arrange
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetValue("solution", "disableSourceControlIntegration")).Returns(value).Verifiable();

            // Act
            bool isDisabled = settings.Object.IsSourceControlDisabled();

            // Assert
            Assert.True(isDisabled);
        }
    }
}
