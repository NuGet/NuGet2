using NuGetConsole.Host;
using Xunit;

namespace PowerShellHost.Test
{
    public class CommandExpansionTest
    {
        [Fact]
        public void AdjustExpansionsReturnsNullIfParametersAreNull()
        {
            // Arrange
            string result = null;
            string leftWord = null;
            string[] expansions = null;

            // Act
            result = CommandExpansion.AdjustExpansions(leftWord, ref expansions);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void AdjustExpansionsReturnsNonNullCommonWord()
        {
            // Arrange
            string result = null;
            string leftWord = "jQuery.";
            string[] expansions = new string[] { "jQuery.UI", "jQuery.Validation" };

            // Act
            result = CommandExpansion.AdjustExpansions(leftWord, ref expansions);

            // Assert
            Assert.Equal(leftWord, result);
            Assert.Equal("UI", expansions[0]);
            Assert.Equal("Validation", expansions[1]);
        }

        [Fact]
        public void AdjustExpansionsReturnsNullIfExpansionsHaveNoCommonWord()
        {
            // Arrange
            string result = null;
            string leftWord = "App1.";
            string[] expansions = new string[] { @".\App1.sln", @".\App1.suo" };

            // Act
            result = CommandExpansion.AdjustExpansions(leftWord, ref expansions);

            // Assert
            Assert.Null(result);
            Assert.Equal(@".\App1.sln", expansions[0]);
            Assert.Equal(@".\App1.suo", expansions[1]);
        }
    }
}
