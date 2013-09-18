using System;
using Moq;
using NuGet.Test.Mocks;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test
{
    public class PackageRestoreConsentTest
    {
        [Fact]
        public void MissingSettingsKeyReturnsTrueForIsGranted()
        {
            // Arrange
            var settings = new Mock<ISettings>();
            var environmentReader = new Mock<IEnvironmentVariableReader>();
            var mockFileSystem = new MockFileSystem();
            var configurationDefaults = new ConfigurationDefaults(mockFileSystem, "NuGetDefaults.config");

            var packageRestore = new PackageRestoreConsent(settings.Object, environmentReader.Object, configurationDefaults);

            // Act
            bool isGranted = packageRestore.IsGranted;

            // Assert
            Assert.True(isGranted);
        }

        [Fact]
        public void IncorrectSettingsValueReturnsFalseForIsGranted()
        {
            // Arrange
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValue("packageRestore", "enabled")).Returns("wrong value");
            var environmentReader = new Mock<IEnvironmentVariableReader>();

            var packageRestore = new PackageRestoreConsent(settings.Object, environmentReader.Object);

            // Act
            bool isGranted = packageRestore.IsGranted;

            // Assert
            Assert.False(isGranted);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("true")]
        [InlineData("1 ")]
        [InlineData(" TRUE")]
        public void CorrectSettingsValueReturnsTrueForIsGranted(string settingsValue)
        {
            // Arrange
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValue("packageRestore", "enabled")).Returns(settingsValue);
            var environmentReader = new Mock<IEnvironmentVariableReader>();

            var packageRestore = new PackageRestoreConsent(settings.Object, environmentReader.Object);

            // Act
            bool isGranted = packageRestore.IsGranted;
            bool isGrantedInSettings = packageRestore.IsGrantedInSettings;

            // Assert
            Assert.True(isGranted);
            Assert.True(isGrantedInSettings);
        }

        [Theory]
        [InlineData("2")]
        [InlineData("wrong")]
        public void InCorrectEnvironmentVariableReturnsTrueForIsGranted(string environmentValue)
        {
            // Arrange
            var settings = Mock.Of<ISettings>();

            var environmentReader = new Mock<IEnvironmentVariableReader>();
            environmentReader.Setup(
                r => r.GetEnvironmentVariable("EnableNuGetPackageRestore")).
                Returns(environmentValue);

            var mockFileSystem = new MockFileSystem();
            var configurationDefaults = new ConfigurationDefaults(mockFileSystem, "NuGetDefaults.config");

            var packageRestore = new PackageRestoreConsent(settings, environmentReader.Object, configurationDefaults);

            // Act
            bool isGranted = packageRestore.IsGranted;

            // Assert
            Assert.True(isGranted);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("   1")]
        [InlineData("1  ")]
        [InlineData("true")]
        [InlineData(" True")]
        [InlineData(" True   ")]
        public void CorrectEnvironmentVariableReturnsTrueForIsGranted(string environmentValue)
        {
            // Arrange
            var settings = new Mock<ISettings>();

            var environmentReader = new Mock<IEnvironmentVariableReader>();
            environmentReader.Setup(
                r => r.GetEnvironmentVariable("EnableNuGetPackageRestore")).
                Returns(environmentValue);

            var packageRestore = new PackageRestoreConsent(settings.Object, environmentReader.Object);

            // Act
            bool isGranted = packageRestore.IsGranted;

            // Assert
            Assert.True(isGranted);
        }

        [Theory]
        [InlineData("1", "")]
        [InlineData("", "1")]
        [InlineData("0", "1")]
        [InlineData("false", "true")]
        [InlineData("true", "false")]
        public void GrantingConsentInEitherSettingOrEnvironmentGrantsConsent(string settingsValue, string environmentValue)
        {
            // Arrange
            var settings = new Mock<ISettings>();
            var environmentReader = new Mock<IEnvironmentVariableReader>(); 
            
            settings.Setup(s => s.GetValue("packageRestore", "enabled")).Returns(settingsValue);
            environmentReader.Setup(r => r.GetEnvironmentVariable("EnableNuGetPackageRestore")).Returns(environmentValue);

            var packageRestore = new PackageRestoreConsent(settings.Object, environmentReader.Object);

            // Act
            bool isGranted = packageRestore.IsGranted;

            // Assert
            Assert.True(isGranted);
        }

        [Fact]
        public void SettingIsGrantedToFalseSetsTheFlagInConfigFile()
        {
            // Arrange
            var settings = new Mock<ISettings>();
            var environmentReader = new Mock<IEnvironmentVariableReader>();

            var packageRestore = new PackageRestoreConsent(settings.Object, environmentReader.Object);

            // Act
            packageRestore.IsGrantedInSettings = false;

            // Assert            
            settings.Verify(s => s.SetValue("packageRestore", "enabled", false.ToString()), Times.Once());
        }

        [Fact]
        public void SettingIsGrantedToTrueSetsTheFlagInConfigFile()
        {
            // Arrange
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValue("packageRestore", "enabled"));
            var environmentReader = new Mock<IEnvironmentVariableReader>();

            var packageRestore = new PackageRestoreConsent(settings.Object, environmentReader.Object);

            // Act
            packageRestore.IsGrantedInSettings = true;

            // Assert
            settings.Verify(s => s.SetValue("packageRestore", "enabled", "True"), Times.Once());
        }


        [Theory]
        // If there is no explicit user consent in user settings, the value in NuGetDefaults
        // will be used
        [InlineData(null, "true", true)]
        [InlineData(" ", "true", true)]
        [InlineData(null, "false", false)]
        [InlineData(" ", "false", false)]
        [InlineData(" ", "blah", false)]

        // When explicit user consent is given in user settings, that value is always used.
        [InlineData("true", "false", true)]
        [InlineData("true", "blah", true)]
        [InlineData("false", "true", false)]
        [InlineData("blah", "true", false)]

        // When there is no explicit user consent, nor value in NuGetDefaults,
        // user consent is considered to be granted.
        [InlineData(null, "", true)]
        [InlineData(" ", " ", true)]
        [InlineData("", " ", true)]
        public void UserGrantSettings(string valueInUserSettings, string valueInNuGetDefault, bool isGranted)
        {
            // Arrange
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValue("packageRestore", "enabled")).Returns(valueInUserSettings);

            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile("NuGetDefaults.config",
                String.Format(
@"
<configuration>
  <packageRestore>
    <add key=""enabled"" value=""{0}"" />
  </packageRestore>
</configuration>",
                valueInNuGetDefault));
            var configurationDefaults = new ConfigurationDefaults(mockFileSystem, "NuGetDefaults.config");

            var environmentReader = new Mock<IEnvironmentVariableReader>();
            var packageRestore = new PackageRestoreConsent(settings.Object, environmentReader.Object, configurationDefaults);

            // Assert
            Assert.Equal(packageRestore.IsGrantedInSettings, isGranted);
        }

        [Fact]
        public void SettingIsAutomaticToFalseSetsTheFlagInConfigFile()
        {
            // Arrange
            var settings = new Mock<ISettings>();
            var environmentReader = new Mock<IEnvironmentVariableReader>();
            var packageRestore = new PackageRestoreConsent(settings.Object, environmentReader.Object);

            // Act
            packageRestore.IsAutomatic = false;

            // Assert
            settings.Verify(s => s.SetValue("packageRestore", "automatic", false.ToString()), Times.Once());
        }

        [Fact]
        public void SettingIsAutomaticToTrueSetsTheFlagInConfigFile()
        {
            // Arrange
            var settings = new Mock<ISettings>();
            var environmentReader = new Mock<IEnvironmentVariableReader>();
            var packageRestore = new PackageRestoreConsent(settings.Object, environmentReader.Object);

            // Act
            packageRestore.IsAutomatic = true;

            // Assert
            settings.Verify(s => s.SetValue("packageRestore", "automatic", true.ToString()), Times.Once());
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("1", true)]
        [InlineData("0", false)]
        [InlineData("false", false)]
        [InlineData("blah", false)]
        public void TestIsAutomaticSettings(string valueInUserSettings, bool isAutomatic)
        {
            // Arrange
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValue("packageRestore", "automatic")).Returns(valueInUserSettings);
            var environmentReader = new Mock<IEnvironmentVariableReader>();
            var packageRestore = new PackageRestoreConsent(settings.Object, environmentReader.Object);

            // Assert
            Assert.Equal(isAutomatic, packageRestore.IsAutomatic);
        }

        // Tests that if there is no setting for key "automatic", then property IsAutomatic
        // returns the value of IsGrantedInSettings.
        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        public void TestIsAutomaticDefaultToIsGranted(string grantSetting, bool isAutomatic)
        {
            // Arrange
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValue("packageRestore", "enabled")).Returns(grantSetting);
            var environmentReader = new Mock<IEnvironmentVariableReader>();
            var packageRestore = new PackageRestoreConsent(settings.Object, environmentReader.Object);

            // Assert
            Assert.Equal(isAutomatic, packageRestore.IsAutomatic);
        }
    }
}
