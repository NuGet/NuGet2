using System;
using System.Net;
using Moq;
using Xunit;

namespace NuGet.Test
{
    public class SettingsCredentialProviderTest
    {
        [Fact]
        public void ConstructorThrowsIfUnderlyingCredentialProviderIsNull()
        {
            // Act and Assert
            ExceptionAssert.ThrowsArgNull(() => new SettingsCredentialProvider(credentialProvider: null, packageSourceProvider: null), "credentialProvider");
        }

        [Fact]
        public void ConstructorThrowsIfSettingsIsNull()
        {
            // Act and Assert
            ExceptionAssert.ThrowsArgNull(() => new SettingsCredentialProvider(credentialProvider: NullCredentialProvider.Instance, packageSourceProvider: null),
                "packageSourceProvider");
        }

        [Fact]
        public void GetCredentialQueriesUnderlyingProviderIfProxyCredentialsAreRequested()
        {
            // Arrange
            var underlyingProvider = new Mock<ICredentialProvider>(MockBehavior.Strict);
            underlyingProvider.Setup(p => p.GetCredentials(It.IsAny<Uri>(), It.IsAny<IWebProxy>(), CredentialType.ProxyCredentials, false))
                              .Returns<ICredentials>(null).Verifiable();

            var packageSourceProvider = new PackageSourceProvider(NullSettings.Instance, null, null, null);
            var settingsCredentialProvider = new SettingsCredentialProvider(underlyingProvider.Object, packageSourceProvider);

            // Act
            var value = settingsCredentialProvider.GetCredentials(new Uri("http://nuget.org"), new Mock<IWebProxy>().Object, CredentialType.ProxyCredentials, false);

            // Assert
            Assert.Null(value);
            underlyingProvider.Verify();
        }

        [Fact]
        public void GetCredentialQueriesUnderlyingProviderIfCredentialsAreNotAvailableInSettings()
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            packageSourceProvider.Setup(s => s.LoadPackageSources())
                                 .Returns(new[] { new PackageSource("https://not-nuget.org", "Official") { UserName = "user", Password = "pass" } })
                                 .Verifiable();

            var underlyingProvider = new Mock<ICredentialProvider>(MockBehavior.Strict);
            underlyingProvider.Setup(p => p.GetCredentials(It.IsAny<Uri>(), It.IsAny<IWebProxy>(), CredentialType.RequestCredentials, false))
                              .Returns<ICredentials>(null).Verifiable();


            var settingsCredentialProvider = new SettingsCredentialProvider(underlyingProvider.Object, packageSourceProvider.Object);

            // Act
            var value = settingsCredentialProvider.GetCredentials(new Uri("https://nuget.org"), new Mock<IWebProxy>().Object, CredentialType.RequestCredentials, false);

            // Assert
            Assert.Null(value);
            underlyingProvider.Verify();
            packageSourceProvider.Verify();
        }

        [Fact]
        public void GetCredentialQueriesReturnsCredentialsFromSourceProviderIfAvailable()
        {
            // Arrange
            string userName = "User";
            string password = "My precious!";

            var sourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            sourceProvider.Setup(s => s.LoadPackageSources())
                          .Returns(new[] { new PackageSource("https://nuget.org") { UserName = userName, Password = password } })
                          .Verifiable();
            var underlyingProvider = new Mock<ICredentialProvider>(MockBehavior.Strict);
            var settingsCredentialProvider = new SettingsCredentialProvider(underlyingProvider.Object, sourceProvider.Object);

            // Act
            var value = settingsCredentialProvider.GetCredentials(new Uri("https://nuget.org"), new Mock<IWebProxy>().Object, CredentialType.RequestCredentials, false);

            // Assert
            sourceProvider.Verify();
            Assert.IsType<NetworkCredential>(value);
            var networkCredential = (NetworkCredential)value;
            Assert.Equal(userName, networkCredential.UserName);
            Assert.Equal(password, networkCredential.Password);
        }

        [Fact]
        public void GetCredentialQueriesReturnsCredentialsFromSourceProviderIfRetrying()
        {
            // Arrange
            string userName = "User";
            string password = "My precious!";

            var sourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            sourceProvider.Setup(s => s.LoadPackageSources())
                          .Returns(new[] { new PackageSource("https://nuget.org") { UserName = userName, Password = password } })
                          .Verifiable();
            var underlyingProvider = new Mock<ICredentialProvider>(MockBehavior.Strict);
            underlyingProvider.Setup(s => s.GetCredentials(new Uri("https://nuget.org"), It.IsAny<IWebProxy>(), CredentialType.RequestCredentials, true))
                              .Returns(new NetworkCredential(userName, password))
                              .Verifiable();
            var settingsCredentialProvider = new SettingsCredentialProvider(underlyingProvider.Object, sourceProvider.Object);

            // Act
            var value = settingsCredentialProvider.GetCredentials(new Uri("https://nuget.org"), new Mock<IWebProxy>().Object, CredentialType.RequestCredentials, true);

            // Assert
            underlyingProvider.Verify();
            Assert.IsType<NetworkCredential>(value);
            var networkCredential = (NetworkCredential)value;
            Assert.Equal(userName, networkCredential.UserName);
            Assert.Equal(password, networkCredential.Password);
        }

        [Fact]
        public void GetCredentialQueriesReturnsCredentialsFromSourceProviderIfAvailableWhenSourceListContainsPhysicalUris()
        {
            // Arrange
            string userName = "User";
            string password = "My precious!";

            var sourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            sourceProvider.Setup(s => s.LoadPackageSources())
                          .Returns(new[] 
                                  { 
                                    new PackageSource(@"x:\build-outputs\packages") { UserName = userName, Password = password },
                                    new PackageSource(@"\\ci-drive\outputs\release\packages\") { UserName = userName, Password = password },
                                    new PackageSource("https://nuget.org") { UserName = userName, Password = password },
                                  })
                          .Verifiable();
            var underlyingProvider = new Mock<ICredentialProvider>(MockBehavior.Strict);
            var settingsCredentialProvider = new SettingsCredentialProvider(underlyingProvider.Object, sourceProvider.Object);

            // Act
            var value = settingsCredentialProvider.GetCredentials(new Uri("https://nuget.org"), new Mock<IWebProxy>().Object, CredentialType.RequestCredentials, false);

            // Assert
            sourceProvider.Verify();
            Assert.IsType<NetworkCredential>(value);
            var networkCredential = (NetworkCredential)value;
            Assert.Equal(userName, networkCredential.UserName);
            Assert.Equal(password, networkCredential.Password);
        }
    }
}
