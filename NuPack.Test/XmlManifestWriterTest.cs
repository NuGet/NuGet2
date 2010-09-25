using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuPack.Test {
    [TestClass]
    public class XmlManifestWriterTest {
        [TestMethod]
        public void SavePersistsMetadataFromPackageBuilderToStream() {
            // Arrange
            var builder = new PackageBuilder() {
                Id = "A",
                Version = new Version("1.0"),
                LicenseUrl = new Uri("http://somesite/somelicense.txt"),
                Language = "fr-FR",
                LastModifiedBy = "John",
                Category = "Category",
                Description = "Description"
            };
            builder.Authors.Add("A1");
            builder.Authors.Add("A2");
            builder.Keywords.Add("KW1");
            builder.Keywords.Add("KW2");
            var writer = new XmlManifestWriter(builder);
            var stream = new MemoryStream();

            // Act
            writer.Save(stream);

            // Assert
            stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>A</id>
    <version>1.0</version>
    <description>Description</description>
    <authors>
      <author>A1</author>
      <author>A2</author>
    </authors>
    <licenseUrl>http://somesite/somelicense.txt</licenseUrl>
    <language>fr-FR</language>
    <lastmodifiedby>John</lastmodifiedby>
    <category>Category</category>
    <keywords>KW1, KW2</keywords>
    <created>0001-01-01T00:00:00</created>
    <modified>0001-01-01T00:00:00</modified>
  </metadata>
</package>", stream.ReadToEnd());
        }
    }
}
