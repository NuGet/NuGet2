using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Commands;
using NuGet.Common;

namespace NuGet.Test {
    [TestClass]
    public class SetApiKeyCommandTest {
        [TestMethod]
        public void SetApiKeyThrowsIfPackageSourceProviderIsNull() {
            // Act and Assert
            ExceptionAssert.ThrowsArgNull(() => new SetApiKeyCommand(packageSourceProvider: null, settings: null), "packageSourceProvider");
        }

        [TestMethod]
        public void SetApiKeyThrowsIfSettingsFileIsNull() {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            
            // Act and Assert
            ExceptionAssert.ThrowsArgNull(() => new SetApiKeyCommand(packageSourceProvider.Object, settings: null), "settings");
        }

        [TestMethod]
        public void SetApiKeyCommandUsesSettingsFile() {
            // Arrange
            var apiKey = "A";
            var settingsFile = new Mock<ISettings>(MockBehavior.Strict);
            settingsFile.Setup(c => c.SetValue("apikeys", NuGetConstants.DefaultGalleryServerUrl, It.IsAny<string>())).Verifiable();
            settingsFile.Setup(c => c.SetValue("apikeys", NuGetConstants.DefaultSymbolServerUrl, It.IsAny<string>())).Verifiable();
            var packageSourceProvider = new Mock<IPackageSourceProvider>();

            // Act
            var setApiKey = new SetApiKeyCommand(packageSourceProvider.Object, settingsFile.Object) {
                Console = new Mock<IConsole>().Object
            };
            
            setApiKey.Arguments.Add(apiKey);
            setApiKey.ExecuteCommand();

            // Assert
            settingsFile.VerifyAll();
        }
    }
}
