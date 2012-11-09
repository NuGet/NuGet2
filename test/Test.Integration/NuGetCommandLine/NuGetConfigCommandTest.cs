using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test.Integration.NuGetCommandLine
{
    public class ConfigCommandTest
    {
        [Fact]
        public void ConfigCommand_ChangeDefaultConfigFile()
        {
            try
            {
                // Arrange
                NugetProgramStatic.BackupAndDeleteDefaultConfigurationFile();            
                string[] args = new string[] { 
                    "config", 
                    "-Set", 
                    "Name1=Value1",
                    "-Set",
                    "HTTP_PROXY=http://127.0.0.1",
                    "-Set",
                    @"HTTP_PROXY.USER=domain\user"
                };

                // Act
                int result = Program.Main(args);

                // Assert
                Assert.Equal(0, result);

                var settings = Settings.LoadDefaultSettings(null);
                var values = settings.GetValues("config");
                AssertEqualCollections(values, new[] { "Name1", "Value1", "HTTP_PROXY", "http://127.0.0.1", "HTTP_PROXY.USER", @"domain\user" });

            }
            finally
            {
                // Cleanup
                NugetProgramStatic.RestoreDefaultConfigurationFile();
            }
        }

        [Fact]
        public void ConfigCommand_ChangeUserDefinedConfigFile()
        {
            var configFile = Path.GetTempFileName();
            File.Delete(configFile);

            string[] args = new string[] { 
                "config", 
                "-Set", 
                "Name1=Value1",
                "-Set",
                "HTTP_PROXY=http://127.0.0.1",
                "-Set",
                @"HTTP_PROXY.USER=domain\user",
                "-ConfigFile",
                configFile
            };

            int result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);

            var settings = Settings.LoadDefaultSettings(
                new PhysicalFileSystem(Path.GetDirectoryName(configFile)),
                Path.GetFileName(configFile));
            var values = settings.GetValues("config");
            AssertEqualCollections(values, new[] { "Name1", "Value1", "HTTP_PROXY", "http://127.0.0.1", "HTTP_PROXY.USER", @"domain\user" });

            // cleanup
            File.Delete(configFile);
        }

        private void AssertEqualCollections(IList<KeyValuePair<string, string>> actual, string[] expected)
        {
            Assert.Equal(actual.Count, expected.Length / 2);
            for (int i = 0; i < actual.Count; ++i)
            {
                Assert.Equal(expected[2 * i], actual[i].Key);
                Assert.Equal(expected[2 * i + 1], actual[i].Value);
            }
        }
    }
}