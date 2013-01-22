using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NuGet.Test.Integration.NuGetCommandLine
{
    public class NuGetInstallCommandTest
    {
        [Fact]
        public void InstallCommand_SaveOnExpandNuspec()
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
                    "-SaveOnExpand", "nuspec" };
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
        public void InstallCommand_SaveOnExpandNupkg()
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
                    "-SaveOnExpand", "nupkg" };
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
        public void InstallCommand_SaveOnExpandNuspecNupkg()
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
                    "-SaveOnExpand", "nupkg;nuspec" };
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

        // Test that after a package is installed with -SaveOnExpand nuspec, nuget.exe
        // can detect that the package is already installed when trying to install the same
        // package.
        [Fact]
        public void InstallCommand_SaveOnExpandNuspecReinstall()
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
                    "-SaveOnExpand", "nuspec" };
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
    }
}
