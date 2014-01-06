using System;
using System.Runtime.Versioning;
using Xunit;
using Xunit.Extensions;

namespace NuGet.VisualStudio.Test
{
    public class PackageExtensionsTest
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
            IPackageFile scriptFile;
            bool result = package.FindCompatibleToolFiles(scriptName, targetFramework, out scriptFile);

            // Assert
            Assert.True(result);
            Assert.Null(scriptFile.TargetFramework);
            Assert.Equal("tools\\" + scriptName, scriptFile.Path, StringComparer.OrdinalIgnoreCase);
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
            IPackageFile scriptFile;
            bool result = package.FindCompatibleToolFiles(scriptName, targetFramework, out scriptFile);

            // Assert
            Assert.False(result);
            Assert.Null(scriptFile);
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
            IPackageFile scriptFile;
            bool result = package.FindCompatibleToolFiles("install.ps1", targetFramework, out scriptFile);

            // Assert
            Assert.True(result);
            Assert.Equal("tools\\sl3\\install.ps1", scriptFile.Path);
            Assert.Equal(scriptFile.TargetFramework, new FrameworkName("Silverlight", new Version("3.0")));
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
            IPackageFile scriptFile;
            bool result = package.FindCompatibleToolFiles("install.ps1", targetFramework, out scriptFile);

            // Assert
            Assert.False(result);
            Assert.Null(scriptFile);
        }
    }
}