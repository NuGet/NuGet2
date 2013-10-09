using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Text;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test.Integration.NuGetCommandLine
{
    public class NuGetInstallCommandTest
    {
        [Fact]
        public void InstallCommand_PackageSaveModeNuspec()
        {
            var tempPath = Path.GetTempPath();
            var source = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var outputDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());

            try
            {
                // Arrange            
                Util.CreateDirectory(source);
                Util.CreateDirectory(outputDirectory);

                var packageFileName = PackageCreater.CreatePackage(
                    "testPackage1", "1.1.0", source);

                // Act
                string[] args = new string[] { 
                    "install", "testPackage1", 
                    "-OutputDirectory", outputDirectory,
                    "-Source", source, 
                    "-PackageSaveMode", "nuspec" };
                int r = Program.Main(args);

                // Assert
                Assert.Equal(0, r);

                var nuspecFile = Path.Combine(
                    outputDirectory,
                    @"testPackage1.1.1.0\testPackage1.1.1.0.nuspec");

                Assert.True(File.Exists(nuspecFile));
                var nupkgFiles = Directory.GetFiles(outputDirectory, "*.nupkg", SearchOption.AllDirectories);
                Assert.Equal(0, nupkgFiles.Length);
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(outputDirectory);
                Util.DeleteDirectory(source);
            }
        }

        [Fact]
        public void InstallCommand_PackageSaveModeNupkg()
        {
            var tempPath = Path.GetTempPath();
            var source = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var outputDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());

            try
            {
                // Arrange            
                Util.CreateDirectory(source);
                Util.CreateDirectory(outputDirectory);

                var packageFileName = PackageCreater.CreatePackage(
                    "testPackage1", "1.1.0", source);

                // Act
                string[] args = new string[] { 
                    "install", "testPackage1", 
                    "-OutputDirectory", outputDirectory,
                    "-Source", source, 
                    "-PackageSaveMode", "nupkg" };
                int r = Program.Main(args);

                // Assert
                Assert.Equal(0, r);

                var nupkgFile = Path.Combine(
                    outputDirectory,
                    @"testPackage1.1.1.0\testPackage1.1.1.0.nupkg");

                Assert.True(File.Exists(nupkgFile));
                var nuspecFiles = Directory.GetFiles(outputDirectory, "*.nuspec", SearchOption.AllDirectories);
                Assert.Equal(0, nuspecFiles.Length);
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(outputDirectory);
                Util.DeleteDirectory(source);
            }
        }

        [Fact]
        public void InstallCommand_PackageSaveModeNuspecNupkg()
        {
            var tempPath = Path.GetTempPath();
            var source = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var outputDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());

            try
            {
                // Arrange            
                Util.CreateDirectory(source);
                Util.CreateDirectory(outputDirectory);

                var packageFileName = PackageCreater.CreatePackage(
                    "testPackage1", "1.1.0", source);

                // Act
                string[] args = new string[] { 
                    "install", "testPackage1", 
                    "-OutputDirectory", outputDirectory,
                    "-Source", source, 
                    "-PackageSaveMode", "nupkg;nuspec" };
                int r = Program.Main(args);

                // Assert
                Assert.Equal(0, r);

                var nupkgFile = Path.Combine(
                    outputDirectory,
                    @"testPackage1.1.1.0\testPackage1.1.1.0.nupkg");
                var nuspecFile = Path.ChangeExtension(nupkgFile, "nuspec");

                Assert.True(File.Exists(nupkgFile));
                Assert.True(File.Exists(nuspecFile));
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(outputDirectory);
                Util.DeleteDirectory(source);
            }
        }

        // Test that after a package is installed with -PackageSaveMode nuspec, nuget.exe
        // can detect that the package is already installed when trying to install the same
        // package.
        [Fact]
        public void InstallCommand_PackageSaveModeNuspecReinstall()
        {
            var tempPath = Path.GetTempPath();
            var source = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var outputDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());

            try
            {
                // Arrange            
                Util.CreateDirectory(source);
                Util.CreateDirectory(outputDirectory);

                var packageFileName = PackageCreater.CreatePackage(
                    "testPackage1", "1.1.0", source);

                string[] args = new string[] { 
                    "install", "testPackage1", 
                    "-OutputDirectory", outputDirectory,
                    "-Source", source, 
                    "-PackageSaveMode", "nuspec" };
                int r = Program.Main(args);
                Assert.Equal(0, r);

                // Act
                MemoryStream memoryStream = new MemoryStream();
                TextWriter writer = new StreamWriter(memoryStream);
                Console.SetOut(writer);
                r = Program.Main(args);
                writer.Close();
                var output = Encoding.Default.GetString(memoryStream.ToArray());

                // Assert
                var expectedOutput = "'testPackage1 1.1.0' already installed." +
                    Environment.NewLine;
                Assert.Equal(expectedOutput, output);
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(outputDirectory);
                Util.DeleteDirectory(source);
            }
        }

        // Test that PackageSaveMode specified in nuget.config file is used.
        [Fact]
        public void InstallCommand_PackageSaveModeInConfigFile()
        {
            var tempPath = Path.GetTempPath();
            var source = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var outputDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());

            try
            {
                // Arrange            
                Util.CreateDirectory(source);
                Util.CreateDirectory(outputDirectory);

                var packageFileName = Util.CreateTestPackage(
                    "testPackage1", "1.1.0", source);

                var configFile = Path.Combine(source, "nuget.config");                
                Util.CreateFile(Path.GetDirectoryName(configFile), Path.GetFileName(configFile), "<configuration/>");
                string[] args = new string[] { 
                    "config", "-Set", "PackageSaveMode=nuspec",
                    "-ConfigFile", configFile };
                int r = Program.Main(args);
                Assert.Equal(0, r);

                // Act
                args = new string[] { 
                    "install", "testPackage1", 
                    "-OutputDirectory", outputDirectory,
                    "-Source", source, 
                    "-ConfigFile", configFile };
                r = Program.Main(args);

                // Assert
                Assert.Equal(0, r);

                var nuspecFile = Path.Combine(
                    outputDirectory,
                    @"testPackage1.1.1.0\testPackage1.1.1.0.nuspec");

                Assert.True(File.Exists(nuspecFile));
                var nupkgFiles = Directory.GetFiles(outputDirectory, "*.nupkg", SearchOption.AllDirectories);
                Assert.Equal(0, nupkgFiles.Length);
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(outputDirectory);
                Util.DeleteDirectory(source);
			}
		}

        // Tests that when package restore is enabled and -RequireConsent is specified,
        // the opt out message is displayed.
        [Theory]
        [InlineData("packages.config")]
        [InlineData("packages.proj1.config")]
        public void InstallCommand_OptOutMessage(string configFileName)
        {
            // Arrange
            var tempPath = Path.GetTempPath();
            var workingPath = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var repositoryPath = Path.Combine(workingPath, Guid.NewGuid().ToString());
            var proj1Directory = Path.Combine(workingPath, "proj1");
            var currentDirectory = Directory.GetCurrentDirectory();
            var targetDir = ConfigurationManager.AppSettings["TargetDir"];
            var nugetexe = Path.Combine(targetDir, "nuget.exe");

            try
            {
                Util.CreateDirectory(workingPath);
                Util.CreateDirectory(repositoryPath);
                Util.CreateDirectory(proj1Directory);

                Util.CreateTestPackage("packageA", "1.1.0", repositoryPath);
                Util.CreateTestPackage("packageB", "2.2.0", repositoryPath);

                Util.CreateFile(workingPath, "my.config",
                    @"
<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageRestore>
    <add key=""enabled"" value=""True"" />
  </packageRestore>
</configuration>");

                Util.CreateFile(proj1Directory, "proj1.csproj",
                    @"<Project ToolsVersion='4.0' DefaultTargets='Build' 
    xmlns='http://schemas.microsoft.com/developer/msbuild/2003'>
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <OutputPath>out</OutputPath>
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
    <None Include='packages.config' />
  </ItemGroup>
</Project>");
                Util.CreateFile(proj1Directory, configFileName,
@"<packages>
  <package id=""packageA"" version=""1.1.0"" targetFramework=""net45"" />
</packages>");
                // Act 
                var r = CommandRunner.Run(
                    nugetexe,
                    proj1Directory,
                    "install " + configFileName + " -Source " + repositoryPath + @" -ConfigFile ..\my.config -RequireConsent -Verbosity detailed",
                    waitForExit: true);

                // Assert
                Assert.Equal(0, r.Item1);
                string optOutMessage = String.Format(
                    CultureInfo.CurrentCulture,
                    NuGetResources.RestoreCommandPackageRestoreOptOutMessage,
                    NuGet.Resources.NuGetResources.PackageRestoreConsentCheckBoxText.Replace("&", ""));
                Assert.Contains(optOutMessage, r.Item2);                
            }
            finally
            {
                Directory.SetCurrentDirectory(currentDirectory);
                Util.DeleteDirectory(workingPath);
            }
        }

        // Tests that when package restore is enabled, but -RequireConsent is not specified,
        // the opt out message is not displayed.
        [Theory]
        [InlineData("packages.config")]
        [InlineData("packages.proj1.config")]
        public void InstallCommand_NoOptOutMessage(string configFileName)
        {
            // Arrange
            var tempPath = Path.GetTempPath();
            var workingPath = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var repositoryPath = Path.Combine(workingPath, Guid.NewGuid().ToString());
            var proj1Directory = Path.Combine(workingPath, "proj1");
            var currentDirectory = Directory.GetCurrentDirectory();
            var targetDir = ConfigurationManager.AppSettings["TargetDir"];
            var nugetexe = Path.Combine(targetDir, "nuget.exe");

            try
            {
                Util.CreateDirectory(workingPath);
                Util.CreateDirectory(repositoryPath);
                Util.CreateDirectory(proj1Directory);

                Util.CreateTestPackage("packageA", "1.1.0", repositoryPath);
                Util.CreateTestPackage("packageB", "2.2.0", repositoryPath);

                Util.CreateFile(workingPath, "my.config",
                    @"
<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageRestore>
    <add key=""enabled"" value=""True"" />
  </packageRestore>
</configuration>");

                Util.CreateFile(proj1Directory, "proj1.csproj",
                    @"<Project ToolsVersion='4.0' DefaultTargets='Build' 
    xmlns='http://schemas.microsoft.com/developer/msbuild/2003'>
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <OutputPath>out</OutputPath>
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
    <None Include='packages.config' />
  </ItemGroup>
</Project>");
                Util.CreateFile(proj1Directory, configFileName,
@"<packages>
  <package id=""packageA"" version=""1.1.0"" targetFramework=""net45"" />
</packages>");
                // Act 
                var r = CommandRunner.Run(
                    nugetexe,
                    proj1Directory,
                    "install " + configFileName + " -Source " + repositoryPath + @" -ConfigFile ..\my.config",
                    waitForExit: true);

                // Assert
                Assert.Equal(0, r.Item1);
                string optOutMessage = String.Format(
                    CultureInfo.CurrentCulture,
                    NuGetResources.RestoreCommandPackageRestoreOptOutMessage,
                    NuGet.Resources.NuGetResources.PackageRestoreConsentCheckBoxText.Replace("&", ""));
                Assert.DoesNotContain(optOutMessage, r.Item2);
            }
            finally
            {
                Directory.SetCurrentDirectory(currentDirectory);
                Util.DeleteDirectory(workingPath);
            }
        }
    }
}
