using System.Globalization;
using System.Threading;
using Xunit;

namespace NuGet.Test
{
    public class LocalizedResourceManagerTest
    {
        [Fact]
        public void GetStringReturnsLocalizedResourceIfAvailable()
        {
            // Arrange
            CultureInfo culture = Thread.CurrentThread.CurrentUICulture;

            // Act
            string resource;
            try
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
                resource = LocalizedResourceManager.GetString("InstallCommandPackageRestoreConsentNotFound");
            }
            finally
            {
                Thread.CurrentThread.CurrentUICulture = culture;
            }

            // Assert
            var expected = LocalizedResourceManager.GetString("InstallCommandPackageRestoreConsentNotFound_fra");
            Assert.Equal(expected, resource);
        }
    }
}
