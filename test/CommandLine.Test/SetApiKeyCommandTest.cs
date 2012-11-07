using Moq;
using NuGet.Commands;
using NuGet.Common;
using Xunit;

namespace NuGet.Test
{
    public class SetApiKeyCommandTest
    {
        [Fact]
        public void SetApiKeyThrowsIfPackageSourceProviderIsNull()
        {
            // Act and Assert
            ExceptionAssert.ThrowsArgNull(() => new SetApiKeyCommand(packageSourceProvider: null, settings: null), "packageSourceProvider");
        }

        [Fact]
        public void SetApiKeyThrowsIfSettingsFileIsNull()
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>();

            // Act and Assert
            ExceptionAssert.ThrowsArgNull(() => new SetApiKeyCommand(packageSourceProvider.Object, settings: null), "settings");
        }

        [Fact]
        public void SetApiKeyThrowsIfSettingsFileIsNullSettings()
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>();

            // Act and Assert
            ExceptionAssert.ThrowsArgumentException(
                () => new SetApiKeyCommand(packageSourceProvider.Object, settings: NullSettings.Instance), 
                "settings", 
                @"Could not access or create config file at %AppData%\NuGet\NuGet.config.");
        }

        [Fact]
        public void SetApiKeyCommandUsesSettingsFile()
        {
            // Arrange
            var apiKey = "A";
            var settingsFile = new Mock<ISettings>(MockBehavior.Strict);
            settingsFile.Setup(c => c.SetValue("apikeys", NuGetConstants.DefaultGalleryServerUrl, It.IsAny<string>())).Verifiable();
            settingsFile.Setup(c => c.SetValue("apikeys", NuGetConstants.DefaultSymbolServerUrl, It.IsAny<string>())).Verifiable();
            var packageSourceProvider = new Mock<IPackageSourceProvider>();

            // Act
            var setApiKey = new SetApiKeyCommand(packageSourceProvider.Object, settingsFile.Object)
            {
                Console = new Mock<IConsole>().Object
            };

            setApiKey.Arguments.Add(apiKey);
            setApiKey.ExecuteCommand();

            // Assert
            settingsFile.VerifyAll();
        }
    }
}
