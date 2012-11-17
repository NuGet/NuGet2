using System;
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
            var command = new SetApiKeyCommand(packageSourceProvider.Object, settings: NullSettings.Instance);
            command.Arguments.Add("foo");

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => command.Execute(), 
                "\"SetValue\" cannot be called on a NullSettings. This may be caused on account of insufficient permissions to read or write to \"%AppData%\\NuGet\\NuGet.config\".");
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
