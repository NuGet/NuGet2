using System;
using System.IO;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuPack.Test {
    /// <summary>
    /// Summary description for XmlManifestReaderTest
    /// </summary>
    [TestClass]
    public class XmlManifestReaderTest {
        [TestMethod]
        public void InvalidSchemaThrows() {
            ExceptionAssert.Throws<InvalidOperationException>(() => XmlManifestReader.ValidateSchema(new XDocument(new XElement("package"))), "The element 'package' in namespace 'http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd' has incomplete content. List of possible elements expected: 'metadata' in namespace 'http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd'.");
        }

        [TestMethod]
        public void PackageBuilderSaveValidatesSchema() {
            // Arrange
            var builder = new PackageBuilder();
            builder.Id = "package-id";
            builder.Version = new Version(1, 0);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => builder.Save(new MemoryStream()), "The element 'metadata' in namespace 'http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd' has incomplete content. List of possible elements expected: 'language, description, authors' in namespace 'http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd'.");
        }
    }
}
