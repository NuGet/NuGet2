using System;
using System.IO;
using Moq;
using Xunit;

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
            PackageBuilder builder = new PackageBuilder()
            {
                Id = packageId,
                Version = new SemanticVersion(version),
                Description = "Descriptions",
            };
            builder.Authors.Add("test");
            builder.Files.Add(CreatePackageFile(@"content\test1.txt"));

            var packageFileName = Path.Combine(path, packageId + "." + version + ".nupkg");
            using (var stream = new FileStream(packageFileName, FileMode.CreateNew))
            {
                builder.Save(stream);
            }

            return packageFileName;
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

        /// <summary>
        /// Creates a file with the specified content.
        /// </summary>
        /// <param name="directory">The directory of the created file.</param>
        /// <param name="fileName">The name of the created file.</param>
        /// <param name="fileContent">The content of the created file.</param>
        public static void CreateFile(string directory, string fileName, string fileContent)
        {
            var fileFullName = Path.Combine(directory, fileName);
            using (var writer = new StreamWriter(fileFullName))
            {
                writer.Write(fileContent);
            }
        }

        private static IPackageFile CreatePackageFile(string name)
        {
            var file = new Mock<IPackageFile>();
            file.SetupGet(f => f.Path).Returns(name);
            file.Setup(f => f.GetStream()).Returns(new MemoryStream());

            string effectivePath;
            var fx = VersionUtility.ParseFrameworkNameFromFilePath(name, out effectivePath);
            file.SetupGet(f => f.EffectivePath).Returns(effectivePath);
            file.SetupGet(f => f.TargetFramework).Returns(fx);

            return file.Object;
        }
    }
}
