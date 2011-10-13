using NuGet.Test;
using Xunit;

namespace NuGet.VisualStudio.Test
{

    public class PathHelperTest
    {

        [Fact]
        public void ThrowsIfInputIsNull()
        {
            // Act & Assert
            ExceptionAssert.ThrowsArgNull(() => PathHelper.SmartTruncate(null, 10), "path");
        }

        [Fact]
        public void ThrowsIfMaxLengthIsLessThan6()
        {
            // Act & Assert
            ExceptionAssert.ThrowsArgOutOfRange(() => PathHelper.SmartTruncate("", 5), "maxWidth", 6, null, true);
            ExceptionAssert.ThrowsArgOutOfRange(() => PathHelper.SmartTruncate("", -4), "maxWidth", 6, null, true);
        }

        [Fact]
        public void ReturnsTheSameStringIfItIsEqualToMaxWidthValue()
        {
            // Arrange
            string input = "abcdef";
            int maxWidth = 6;

            // Act
            string output = PathHelper.SmartTruncate(input, maxWidth);

            // Assert
            Assert.Equal(input, output);
        }

        [Fact]
        public void ReturnsTheSameStringIfItIsShorterThanMaxWidthValue()
        {
            // Arrange
            string input = @"c:\user\documents\projects";
            int maxWidth = 30;

            // Act
            string output = PathHelper.SmartTruncate(input, maxWidth);

            // Assert
            Assert.Equal(input, output);
        }

        [Fact]
        public void TruncateIfInputIsLongerThanMaxWidth()
        {
            // Arrange
            string input = @"c:\user\documents\projects";
            int maxWidth = 20;

            // Act
            string output = PathHelper.SmartTruncate(input, maxWidth);

            // Assert
            Assert.Equal(@"c:\...\projects\", output);
        }

        [Fact]
        public void TruncateIfInputIsLongerThanMaxWidth2()
        {
            // Arrange
            string input = @"c:\user\documents\projects\";
            int maxWidth = 26;

            // Act
            string output = PathHelper.SmartTruncate(input, maxWidth);

            // Assert
            Assert.Equal(@"c:\...\projects\", output);
        }

        [Fact]
        public void TruncateFolderNameIfItIsTooLong()
        {
            // Arrange
            string input = @"c:\thisisaverylongname";
            int maxWidth = 10;

            // Act
            string output = PathHelper.SmartTruncate(input, maxWidth);

            // Assert
            Assert.True(output.Length == maxWidth);
            Assert.Equal(@"c:\...ame\", output);
        }

        [Fact]
        public void EscapePSPathTest()
        {
            TestEscapePSPath("", "''");
            TestEscapePSPath("abc", "'abc'");
            TestEscapePSPath("a$$b", "'a$$b'");
            TestEscapePSPath("'a$$'b", "\"'a`$`$'b\"");
            TestEscapePSPath("hello world", "'hello world'");
            TestEscapePSPath("Gun 'n Roses", "\"Gun 'n Roses\"");
            TestEscapePSPath("Hello [ Kitty ]", "'Hello `[ Kitty `]'");
            TestEscapePSPath("Foo []", "'Foo `[`]'");
            TestEscapePSPath("Fo'o []", "\"Fo'o `[`]\"");
            TestEscapePSPath("Bar [", "'Bar `['");
            TestEscapePSPath("Bar ]", "'Bar `]'");
            TestEscapePSPath(@"c:\users\name\Foo]\Console.sln", @"'c:\users\name\Foo`]\Console.sln'");
        }

        private static void TestEscapePSPath(string input, string expectedOutput)
        {
            string output = PathHelper.EscapePSPath(input);
            Assert.Equal(expectedOutput, output);
        }
    }
}