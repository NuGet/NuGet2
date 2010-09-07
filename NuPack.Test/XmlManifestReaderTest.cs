using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Linq;

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
    }
}
