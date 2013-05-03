using System.Runtime.Versioning;
using Xunit;

namespace NuGet.Test
{
    public class NetPortableProfileTableTest
    {
        [Fact]
        public void LoadSupportedFrameworkCorrectsTheWP7Framework()
        {
            // Arrange
            string content = @"
<Framework
    Identifier=""Silverlight""
    Profile=""WindowsPhone*""
    MinimumVersion=""4.0""
    DisplayName=""Windows Phone""
    MinimumVersionDisplayName=""7"" />";

            // Act
            FrameworkName fx = NetPortableProfileTable.LoadSupportedFramework(content.AsStream());

            // Assert
            Assert.Equal(new FrameworkName("Silverlight, Version=3.0, Profile=WindowsPhone"), fx);

        }

        [Fact]
        public void LoadSupportedFrameworkCorrectsTheWP71Framework()
        {
            // Arrange
            string content = @"
<Framework
    Identifier=""Silverlight""
    Profile=""WindowsPhone7*""
    MinimumVersion=""4.0""
    DisplayName=""Windows Phone""
    MinimumVersionDisplayName=""7.5"" />";

            // Act
            FrameworkName fx = NetPortableProfileTable.LoadSupportedFramework(content.AsStream());

            // Assert
            Assert.Equal(new FrameworkName("Silverlight, Version=4.0, Profile=WindowsPhone71"), fx);
        }
    }
}
