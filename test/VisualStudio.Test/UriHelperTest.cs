using Xunit;

namespace NuGet.VisualStudio.Test
{

    public class UriHelperTest
    {

        [Fact]
        public void TestInvalidSources()
        {
            // Arrange
            string[] testValues = new[] { null, "", "link", "c:\\dir", "\\username\folder", "127.0.0.1", "localhost", "crash;\\_andBurn", "ftp://bing.com", "gopher://kill.it", "http://" };

            TestValues(testValues, false);
        }

        [Fact]
        public void TestValidSources()
        {
            // Arrange
            string[] testValues = new[] { "http://bing.com", "http://microsoft.com", "https://paypal.com", "http://library" };

            TestValues(testValues, true);
        }

        private static void TestValues(string[] testValues, bool valid)
        {
            foreach (var value in testValues)
            {
                // Act
                bool isValid = UriHelper.IsHttpSource(value);

                // Assert
                Assert.Equal(valid, isValid);
            }
        }
    }
}
