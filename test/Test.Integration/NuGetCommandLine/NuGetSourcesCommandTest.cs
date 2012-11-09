using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test.Integration.NuGetCommandLine
{
    public class SourcesCommandTest
    {
        [Fact]
        public void SourcesCommandTest_AddSource()
        {
            try
            {
                // Arrange
                NugetProgramStatic.BackupAndDeleteDefaultConfigurationFile();

                string[] args = new string[] { 
                    "sources",
                    "Add",
                    "-Name",
                    "test_source",
                    "-Source",
                    "http://test_source"            
                };

                // Act
                int result = Program.Main(args);

                // Assert
                Assert.Equal(0, result);
                var settings = Settings.LoadDefaultSettings(null);
                var source = settings.GetValue("packageSources", "test_source");
                Assert.Equal("http://test_source", source);
            }
            finally
            {
                // Cleanup
                NugetProgramStatic.RestoreDefaultConfigurationFile();
            }
        }

        [Fact]
        public void SourcesCommandTest_AddWithUserNamePassword()
        {
            try
            {
                // Arrange
                NugetProgramStatic.BackupAndDeleteDefaultConfigurationFile();

                string[] args = new string[] { 
                    "sources",
                    "Add",
                    "-Name",
                    "test_source",
                    "-Source",
                    "http://test_source",
                    "-UserName",
                    "test_user_name",
                    "-Password",
                    "test_password"            
                };

                // Act
                int result = Program.Main(args);

                // Assert
                Assert.Equal(0, result);

                var settings = Settings.LoadDefaultSettings(null);
                var source = settings.GetValue("packageSources", "test_source");
                Assert.Equal("http://test_source", source);

                var credentials = settings.GetNestedValues(
                    "packageSourceCredentials", "test_source");
                Assert.Equal(2, credentials.Count);

                Assert.Equal("Username", credentials[0].Key);
                Assert.Equal("test_user_name", credentials[0].Value);

                Assert.Equal("Password", credentials[1].Key);
                var password = EncryptionUtility.DecryptString(credentials[1].Value);
                Assert.Equal("test_password", password);
            }
            finally
            {
                // Cleanup
                NugetProgramStatic.RestoreDefaultConfigurationFile();
            }
        }

        [Fact]
        public void SourcesCommandTest_AddWithUserNamePassword_UserDefinedConfigFile()
        {
            // Arrange
            var configFile = Path.GetTempFileName();
            File.Delete(configFile);

            string[] args = new string[] { 
                "sources",
                "Add",
                "-Name",
                "test_source",
                "-Source",
                "http://test_source",
                "-UserName",
                "test_user_name",
                "-Password",
                "test_password",
                "-ConfigFile",
                configFile
            };

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);

            var settings = Settings.LoadDefaultSettings(
                new PhysicalFileSystem(Path.GetDirectoryName(configFile)),
                Path.GetFileName(configFile));
            var source = settings.GetValue("packageSources", "test_source");
            Assert.Equal("http://test_source", source);

            var credentials = settings.GetNestedValues(
                "packageSourceCredentials", "test_source");
            Assert.Equal(2, credentials.Count);

            Assert.Equal("Username", credentials[0].Key);
            Assert.Equal("test_user_name", credentials[0].Value);

            Assert.Equal("Password", credentials[1].Key);
            var password = EncryptionUtility.DecryptString(credentials[1].Value);
            Assert.Equal("test_password", password);
        }
    }
}
