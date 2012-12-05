using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test.Integration.NuGetCommandLine
{
    public static class Util
    {
        /// <summary>
        /// Creates a test package.
        /// </summary>
        /// <param name="packageId">The id of the created package.</param>
        /// <param name="version">The version of the created package.</param>
        /// <param name="path">The directory where the package is created.</param>
        /// <returns>The name of the created package file.</returns>
        public static string CreateTestPackage(string packageId, string version, string path)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var tempPath = Path.GetTempPath();
            var packageDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());
            CreateDirectory(packageDirectory);

            Directory.SetCurrentDirectory(packageDirectory);
            string[] args = new string[] { "spec", packageId };
            int r = Program.Main(args);
            Assert.Equal(0, r);

            args = new string[] { "pack", "-Version", version };
            r = Program.Main(args);
            Assert.Equal(0, r);

            var packageFileName = string.Format("{0}.{1}.nupkg", packageId, version);
            File.Move(packageFileName, Path.Combine(path, packageFileName));

            Directory.SetCurrentDirectory(currentDirectory);
            Directory.Delete(packageDirectory, true);

            return Path.Combine(path, packageFileName);
        }

        /// <summary>
        /// Creates the specified directory. If it exists, it's first deleted before 
        /// it's created. Thus, the directory is guaranteed to be empty.
        /// </summary>
        /// <param name="directory">The directory to be created.</param>
        public static void CreateDirectory(string directory)
        {
            Util.DeleteDirectory(directory);
            Directory.CreateDirectory(directory);
        }

        /// <summary>
        /// Deletes the specified directory.
        /// </summary>
        /// <param name="packageDirectory">The directory to be deleted.</param>
        public static void DeleteDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
