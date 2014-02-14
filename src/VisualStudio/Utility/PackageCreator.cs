using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.VisualStudio
{
    /// <summary>
    /// This class is used by functional tests to create packages.
    /// </summary>
    public static class PackageCreator
    {
        /// <summary>
        /// Creates a simple package.
        /// </summary>
        /// <param name="id">The id of the package.</param>
        /// <param name="version">The version of the package.</param>
        /// <param name="outputDirectory">The output directory.</param>
        /// <returns>The path of the nupkg file created.</returns>
        public static string CreatePackage(string id, string version, string outputDirectory)
        {
            PackageBuilder builder = new PackageBuilder()
            {
                Id = id,
                Version = new SemanticVersion(version),
                Description = "Descriptions",
            };
            builder.Authors.Add("test");

            var fileName = string.Format(CultureInfo.InvariantCulture, @"content\{0}_test1.txt", id);
            builder.Files.Add(new TestPackageFile(fileName));

            var packageFileName = Path.Combine(
                outputDirectory,
                string.Format(
                    CultureInfo.InvariantCulture, "{0}.{1}{2}",
                    id, version, Constants.PackageExtension));
            using (var stream = new FileStream(packageFileName, FileMode.CreateNew))
            {
                builder.Save(stream);
            }

            return packageFileName;
        }

        private class TestPackageFile : IPackageFile
        {
            public string Path { get; private set; }
            public string EffectivePath { get; private set; }
            public System.Runtime.Versioning.FrameworkName TargetFramework { get; private set; }

            public TestPackageFile(string path)
            {
                Path = path;
                string effectivePath;
                var fx = VersionUtility.ParseFrameworkNameFromFilePath(path, out effectivePath);
                EffectivePath = effectivePath;
                TargetFramework = fx;
            }

            public Stream GetStream()
            {
                return "test".AsStream();
            }

            public IEnumerable<System.Runtime.Versioning.FrameworkName> SupportedFrameworks
            {
                get
                {
                    return new[] { TargetFramework };
                }
            }
        }
    }    
}
