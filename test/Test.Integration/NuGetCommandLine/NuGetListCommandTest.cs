using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test.Integration.NuGetCommandLine
{
    public class ListCommandTest
    {
        [Fact]
        public void ListCommand_WithUserSpecifiedSource()
        {
            // Arrange
            var tempPath = Path.GetTempPath();
            var repositoryPath = Path.Combine(tempPath, Guid.NewGuid().ToString());
            Util.CreateDirectory(repositoryPath);
            Util.CreateTestPackage("testPackage1", "1.1.0", repositoryPath);
            Util.CreateTestPackage("testPackage2", "2.0.0", repositoryPath);

            string[] args = new string[] { "list", "-Source", repositoryPath };
            MemoryStream memoryStream = new MemoryStream();
            TextWriter writer = new StreamWriter(memoryStream);
            Console.SetOut(writer);

            // Act
            int r = Program.Main(args);
            writer.Close();

            // Assert
            Assert.Equal(0, r);
            var output = Encoding.Default.GetString(memoryStream.ToArray());
            Assert.Equal("testPackage1 1.1.0\r\ntestPackage2 2.0.0\r\n", output);
        }

        [Fact]
        public void ListCommand_WithUserSpecifiedConfigFile()
        {
            // Arrange
            var tempPath = Path.GetTempPath();
            var repositoryPath = Path.Combine(tempPath, Guid.NewGuid().ToString());
            Util.CreateDirectory(repositoryPath);
            Util.CreateTestPackage("testPackage1", "1.1.0", repositoryPath);
            Util.CreateTestPackage("testPackage2", "2.0.0", repositoryPath);

            // create the config file
            var configFile = Path.GetTempFileName();
            File.Delete(configFile);
            string[] args = new string[] { 
                "sources", 
                "Add", 
                "-Name", 
                "test_source", 
                "-Source",
                repositoryPath,
                "-ConfigFile",
                configFile
            };
            int r = Program.Main(args);
            Assert.Equal(0, r);

            // Act: execute the list command
            args = new string[] { "list", "-Source", "test_source", "-ConfigFile", configFile };
            MemoryStream memoryStream = new MemoryStream();
            TextWriter writer = new StreamWriter(memoryStream);
            Console.SetOut(writer);

            r = Program.Main(args);
            writer.Close();
            File.Delete(configFile);           

            // Assert
            Assert.Equal(0, r);
            var output = Encoding.Default.GetString(memoryStream.ToArray());
            Assert.Equal("testPackage1 1.1.0\r\ntestPackage2 2.0.0\r\n", output);
        }
    }
}
