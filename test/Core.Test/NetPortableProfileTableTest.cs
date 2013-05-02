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

        [Fact]
        public void BuiltInProfileReportsProperFrameworkVersion()
        {
            var profile = NetPortableProfileTable.GetProfile("Profile1");

            // Assert
            Assert.Equal("v4.0", profile.FrameworkVersion);
        }

        [Fact]
        public void BuiltInCustomProfileStringReportsProperFrameworkVersion()
        {
            var profile = NetPortableProfile.Parse("win+net40+sl40+wp+Xbox40");

            // Assert
            Assert.Equal("v4.0", profile.FrameworkVersion);
        }

        [Fact]
        public void BuiltIn45ProfileReportsProperFrameworkVersion()
        {
            var profile = NetPortableProfileTable.GetProfile("Profile7");

            // Assert
            Assert.Equal("v4.5", profile.FrameworkVersion);

            System.Console.WriteLine(profile.CustomProfileString);
        }

        [Fact]
        public void BuiltIn45CustomProfileStringReportsProperFrameworkVersion()
        {
            var profile = NetPortableProfile.Parse("win+net45");

            // Assert
            Assert.Equal("v4.5", profile.FrameworkVersion);
        }

    }
}
