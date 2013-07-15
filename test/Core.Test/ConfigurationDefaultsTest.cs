using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{
    public class ConfigurationDefaultsTest
    {
        private ConfigurationDefaults GetConfigurationDefaults(string configurationDefaultsContent)
        {
            var mockFileSystem = new MockFileSystem();
            var configurationDefaultsPath = "NuGetDefaults.config";
            mockFileSystem.AddFile(configurationDefaultsPath, configurationDefaultsContent);
            return new ConfigurationDefaults(mockFileSystem, configurationDefaultsPath);
        }

        [Fact]
        public void CreateConfigurationDefaultsReturnsNonNullConfigurationDefaults()
        {
            // Arrange
            ConfigurationDefaults ConfigurationDefaults = GetConfigurationDefaults(@"<configuration></configuration>");

            // Act & Assert
            Assert.NotNull(ConfigurationDefaults);
        }

        [Fact]
        public void CreateConfigurationDefaultsThrowsWhenXmlIsInvalid()
        {
            // Act & Assert
            ExceptionAssert.Throws<XmlException>(() => GetConfigurationDefaults(@"<configuration>"));
        }

        [Fact]
        public void GetDefaultPushSourceReadsTheCorrectValue()
        {
            // Arrange
            string configurationDefaultsContent = @"
<configuration>
     <config>
        <add key='DefaultPushSource' value='http://contoso.com/packages/' />
    </config>
</configuration>";

            // Act & Assert
            ConfigurationDefaults ConfigurationDefaults = GetConfigurationDefaults(configurationDefaultsContent);

            Assert.Equal(ConfigurationDefaults.DefaultPushSource, "http://contoso.com/packages/");
        }

        [Fact]
        public void GetDefaultPushSourceReturnsNull()
        {
            // Arrange
            string configurationDefaultsContent = @"
<configuration>
</configuration>";

            // Act & Assert
            ConfigurationDefaults ConfigurationDefaults = GetConfigurationDefaults(configurationDefaultsContent);

            Assert.Null(ConfigurationDefaults.DefaultPushSource);
        }

        [Fact]
        public void GetDefaultPackageSourcesReturnsValidPackageSources()
        {
            // Arrange
            string configurationDefaultsContent = @"
<configuration>
    <packageSources>
        <add key='Contoso Package Source' value='http://contoso.com/packages/' />
        <add key='NuGet Official Feed' value='http://www.nuget.org/api/v2/' />
    </packageSources>
    <disabledPackageSources>
        <add key='NuGet Official Feed' value='true' />
    </disabledPackageSources>
</configuration>";

            // Act & Assert
            ConfigurationDefaults ConfigurationDefaults = GetConfigurationDefaults(configurationDefaultsContent);
            Assert.NotNull(ConfigurationDefaults.DefaultPackageSources);

            List<PackageSource> defaultPackageSources = ConfigurationDefaults.DefaultPackageSources.ToList();

            Assert.Equal(defaultPackageSources.Count, 2);

            Assert.Equal(defaultPackageSources[0].Name, "Contoso Package Source");
            Assert.True(defaultPackageSources[0].IsEnabled);
            Assert.True(defaultPackageSources[0].IsOfficial);

            Assert.Equal(defaultPackageSources[1].Name, "NuGet Official Feed");
            Assert.False(defaultPackageSources[1].IsEnabled);
            Assert.True(defaultPackageSources[1].IsOfficial);
        }

        [Fact]
        public void GetDefaultPackageSourcesReturnsEmptyList()
        {
            // Arrange
            string configurationDefaultsContent = @"
<configuration>
</configuration>";

            // Act & Assert
            ConfigurationDefaults ConfigurationDefaults = GetConfigurationDefaults(configurationDefaultsContent);
            Assert.True(ConfigurationDefaults.DefaultPackageSources.IsEmpty());
        }
    }
}
