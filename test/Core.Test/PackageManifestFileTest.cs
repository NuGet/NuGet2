using System.IO;
using System.Linq;
using NuGet.Test.Utility;
using Xunit;

namespace NuGet.Test
{
    public class PackageManifestFileTest
    {
        [Fact]
        public void PackageLoadsManifestFileData()
        {
            // Arrange
            var spec = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
    <metadata>
        <id>Test</id>
        <version>1.0</version>
        <authors>Ant</authors>
        <description>A test package</description>
    </metadata>
    <files>
        <file src=""my.txt"" target=""content"">
          <properties>
            <property name=""propName"" value=""propValue"" />
          </properties>
        </file>
    </files>
</package>";

            var builder = new PackageBuilder(spec.AsStream(), null);
            builder.Files.AddRange(
                PackageUtility.CreateFiles(new[]
                    {
                        PathFixUtility.FixPath(@"content\my.txt")
                    }));

            var ms = new MemoryStream();
            //var ms = File.Create("C:\\temp\\temp.zip");
            builder.Save(ms);
            ms.Seek(0, SeekOrigin.Begin);

            // Act
            var package = new ZipPackage(ms);

            Assert.NotNull(package.ManifestFiles);
            Assert.Equal(1, package.ManifestFiles.Count());
        }
    }
}