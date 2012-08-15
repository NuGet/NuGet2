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
            Assert.Equal("La fonctionnalité de restauration des packages est désactivée par défaut. Pour donner votre accord, ouvrez la boîte de dialogue Options de Visual Studio, cliquez sur le nœud Gestionnaire de package et activez l’option « Autoriser NuGet à télécharger les packages manquants lors de la génération ». Vous pouvez également donner votre accord en définissant la variable d’environnement « EnableNuGetPackageRestore » sur « true ».", 
                         resource);
        }
    }
}
