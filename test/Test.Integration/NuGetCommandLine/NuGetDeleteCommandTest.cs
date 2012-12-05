using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test.Integration.NuGetCommandLine
{
    public class NuGetDeleteCommandTest
    {
        // Tests deleting a package from a source that is a file system directory.
        [Fact]
        public void DeleteCommand_DeleteFromFileSystemSource()
        {
            var tempPath = Path.GetTempPath();
            var source = Path.Combine(tempPath, Guid.NewGuid().ToString());            
            try
            {
                // Arrange            
                Util.CreateDirectory(source);
                var packageFileName = Util.CreateTestPackage("testPackage1", "1.1.0", source);
                Assert.True(File.Exists(packageFileName));

                // Act
                string[] args = new string[] { 
                    "delete", "testPackage1", "1.1.0", 
                    "-Source", source, "-NonInteractive" };
                int r = Program.Main(args);

                // Assert
                Assert.Equal(0, r);
                Assert.False(File.Exists(packageFileName));

            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(source);
            }
        }

        // Same as DeleteCommand_DeleteFromFileSystemSource, except that the directory is specified
        // in unix style.
        [Fact]
        public void DeleteCommand_DeleteFromFileSystemSourceUnixStyle()
        {
            var tempPath = Path.GetTempPath();
            var source = Path.Combine(tempPath, Guid.NewGuid().ToString());
            source = source.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            try
            {
                // Arrange
                Util.CreateDirectory(source);
                var packageFileName = Util.CreateTestPackage("testPackage1", "1.1.0", source);
                Assert.True(File.Exists(packageFileName));

                // Act
                string[] args = new string[] { 
                    "delete", "testPackage1", "1.1.0", 
                    "-Source", source, "-NonInteractive" };
                int r = Program.Main(args);

                // Assert
                Assert.Equal(0, r);
                Assert.False(File.Exists(packageFileName));
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(source);
            }
        }
    }
}
