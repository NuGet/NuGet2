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
            var setApiKeyCommand = new SetApiKeyCommand();
            setApiKeyCommand.SourceProvider = null;            
            ExceptionAssert.Throws<InvalidOperationException>(() => setApiKeyCommand.ExecuteCommand(), "Property SourceProvider is null.");
        }

        [Fact]
        public void SetApiKeyThrowsIfSettingsFileIsNull()
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            var setApiKeyCommand = new SetApiKeyCommand();
            setApiKeyCommand.SourceProvider = packageSourceProvider.Object;
            setApiKeyCommand.Settings = null;

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => setApiKeyCommand.ExecuteCommand(), "Property Settings is null.");
        }

        [Fact]
        public void SetApiKeyThrowsIfSettingsFileIsNullSettings()
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            var setApiKeyCommand = new SetApiKeyCommand();
            setApiKeyCommand.SourceProvider = packageSourceProvider.Object;
            setApiKeyCommand.Settings = NullSettings.Instance;
            setApiKeyCommand.Arguments.Add("foo");

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => setApiKeyCommand.ExecuteCommand(),
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

            var setApiKey = new SetApiKeyCommand()
            {
                SourceProvider = packageSourceProvider.Object,
                Settings = settingsFile.Object,
                Console = new Mock<IConsole>().Object
            };

            // Act
            setApiKey.Arguments.Add(apiKey);
            setApiKey.ExecuteCommand();

            // Assert
            settingsFile.VerifyAll();
        }
    }
}
