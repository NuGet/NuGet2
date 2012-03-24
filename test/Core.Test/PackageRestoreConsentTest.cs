using System;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test
{
    public class PackageRestoreConsentTest
    {
        [Fact]
        public void MissingSettingsKeyReturnsFalseForIsGranted()
        {
            // Arrange
            var settings = new Mock<ISettings>();
            var environmentReader = new Mock<IEnvironmentVariableReader>();

            var packageRestore = new PackageRestoreConsent(settings.Object, environmentReader.Object);

            // Act
            bool isGranted = packageRestore.IsGranted;

            // Assert
            Assert.False(isGranted);
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
        public void CorrectSettingsValueReturnsTrueForIsGranted(string settingsValue)
        {
            // Arrange
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValue("packageRestore", "enabled")).Returns(settingsValue);
            var environmentReader = new Mock<IEnvironmentVariableReader>();

            var packageRestore = new PackageRestoreConsent(settings.Object, environmentReader.Object);

            // Act
            bool isGranted = packageRestore.IsGranted;

            // Assert
            Assert.True(isGranted);
        }

        [Theory]
        [InlineData("2")]
        [InlineData("wrong")]
        public void InCorrectEnvironmentVariableReturnsFalseForIsGranted(string environmentValue)
        {
            // Arrange
            var settings = new Mock<ISettings>();

            var environmentReader = new Mock<IEnvironmentVariableReader>();
            environmentReader.Setup(
                r => r.GetEnvironmentVariable("EnableNuGetPackageRestore", It.IsAny<EnvironmentVariableTarget>())).
                Returns(environmentValue);

            var packageRestore = new PackageRestoreConsent(settings.Object, environmentReader.Object);

            // Act
            bool isGranted = packageRestore.IsGranted;

            // Assert
            Assert.False(isGranted);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("true")]
        public void CorrectEnvironmentVariableReturnsTrueForIsGranted(string environmentValue)
        {
            // Arrange
            var settings = new Mock<ISettings>();
            
            var environmentReader = new Mock<IEnvironmentVariableReader>();
            environmentReader.Setup(
                r => r.GetEnvironmentVariable("EnableNuGetPackageRestore", It.IsAny<EnvironmentVariableTarget>())).
                Returns(environmentValue);

            var packageRestore = new PackageRestoreConsent(settings.Object, environmentReader.Object);

            // Act
            bool isGranted = packageRestore.IsGranted;

            // Assert
            Assert.True(isGranted);
        }
    }
}
