using System;
using System.Runtime.Versioning;
using Xunit;
using Xunit.Extensions;

namespace NuGet.VisualStudio.Test
{
    class PackageExtensionsTest
    {
        [Theory]
        [InlineData("Init.ps1")]
        [InlineData("Install.ps1")]
        [InlineData("Uninstall.ps1")]
        public void FindCompatiblePowerShellScriptFindScriptsUnderToolsFolder(string scriptName)
        {
            // Arrange
            var targetFramework = new FrameworkName("Silverlight", new Version("2.0"));
            var package = NuGet.Test.PackageUtility.CreatePackage("A", "1.0", tools: new[] { scriptName });

            // Act
            string scriptPath;
            bool result = package.FindCompatibleToolFiles(scriptName, targetFramework, out scriptPath);

            // Assert
            Assert.True(result);
            Assert.Equal("tools\\" + scriptName, scriptPath, StringComparer.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("Init.ps1")]
        [InlineData("Install.ps1")]
        [InlineData("Uninstall.ps1")]
        public void FindCompatiblePowerShellScriptDoesNotFindScriptsOutsideToolsFolder(string scriptName)
        {
            // Arrange
            var targetFramework = new FrameworkName("Silverlight", new Version("2.0"));
            var package = NuGet.Test.PackageUtility.CreatePackage("A", "1.0", content: new[] { scriptName });

            // Act
            string scriptPath;
            bool result = package.FindCompatibleToolFiles(scriptName, targetFramework, out scriptPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void FindCompatiblePowerShellScriptFindScriptsCompatibleWithTargetFramework()
        {
            // Arrange
            var targetFramework = new FrameworkName("Silverlight", new Version("3.5"));
            var package = NuGet.Test.PackageUtility.CreatePackage("A",
                                                       "1.0",
                                                       tools: new[] { "sl3\\install.ps1", "net35\\install.ps1", "[sl40]\\uninstall.ps1" });

            // Act
            string scriptPath;
            bool result = package.FindCompatibleToolFiles("install.ps1", targetFramework, out scriptPath);

            // Assert
            Assert.True(result);
            Assert.Equal("tools\\sl3\\install.ps1", scriptPath);
        }

        [Fact]
        public void FindCompatiblePowerShellScriptDoesNotFindScriptIfTargetFrameworkIsNotCompabitle()
        {
            // Arrange
            var targetFramework = new FrameworkName("Silverlight", new Version("3.5"));
            var package = NuGet.Test.PackageUtility.CreatePackage("A",
                                                       "1.0",
                                                       tools: new[] { "[netmf]\\install.ps1", "[net35]\\install.ps1", "[sl40]\\install.ps1" });

            // Act
            string scriptPath;
            bool result = package.FindCompatibleToolFiles("install.ps1", targetFramework, out scriptPath);

            // Assert
            Assert.False(result);
        }
    }
}