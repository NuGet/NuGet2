using System;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuPack.Test {
    [TestClass]
    public class PackageBuilderTest {
        [TestMethod]
        public void PackageBuilderThrowsIfXmlIsMalformed() {
            // Arrange
            string spec1 = "kjdkfj";
            string spec2 = @"<?xml version=""1.0"" encoding=""utf-8""?>";
            string spec3 = @"<?xml version=""1.0"" encoding=""utf-8""?><package />";
            string spec4 = @"<?xml version=""1.0"" encoding=""utf-8""?><package><metadata></metadata></package>";

            // Act and Assert
            ExceptionAssert.Throws<XmlException>(() => new PackageBuilder(spec1.AsStream(), null));
            ExceptionAssert.Throws<XmlException>(() => new PackageBuilder(spec2.AsStream(), null));
            ExceptionAssert.Throws<InvalidOperationException>(() => new PackageBuilder(spec3.AsStream(), null));
            ExceptionAssert.Throws<InvalidOperationException>(() => new PackageBuilder(spec4.AsStream(), null));
        }


        [TestMethod]
        public void PackageBuilderRequiredFields() {
            // Arrange
            string spec = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package><metadata>
    <id>Artem.XmlProviders</id>
    <version>2.5</version>
    <authors><author>Velio Ivanov</author></authors>
    <language>en-us</language>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
  </metadata></package>";
            
            string badSpec1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package><metadata>
    <version>2.5</version>
    <authors><author>Velio Ivanov</author></authors>
    <language>en-us</language>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
  </metadata></package>";
            
            string badSpec2 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package><metadata>
    <id>Artem.XmlProviders</id>
    <authors><author>Velio Ivanov</author></authors>
    <language>en-us</language>    
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
  </metadata></package>";

            string badSpec3 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package><metadata>
    <id>Artem.XmlProviders</id>
    <version>2.5</version>
    <language>en-us</language>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
  </metadata></package>";

            string badSpec4 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package><metadata>
    <id>Artem.XmlProviders</id>
    <version>2.5</version>
    <authors><author>Velio Ivanov</author></authors>
    <language>en-us</language>
  </metadata></package>";

            string badSpec5 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package><metadata>
    <id>Artem.XmlProviders</id>
    <version>2.5</version>
    <authors><author>Velio Ivanov</author></authors>
  </metadata></package>";
            
            // Act
            var packageBuilder = new PackageBuilder(spec.AsStream(), null);

            // Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => new PackageBuilder(badSpec1.AsStream(), null));
            ExceptionAssert.Throws<InvalidOperationException>(() => new PackageBuilder(badSpec2.AsStream(), null));
            ExceptionAssert.Throws<InvalidOperationException>(() => new PackageBuilder(badSpec3.AsStream(), null));
            ExceptionAssert.Throws<InvalidOperationException>(() => new PackageBuilder(badSpec4.AsStream(), null));
            ExceptionAssert.Throws<InvalidOperationException>(() => new PackageBuilder(badSpec5.AsStream(), null));
            Assert.IsNotNull(packageBuilder); // Verify no exception was thrown
        }


        [TestMethod]
        public void ReadingPackageBuilderFromStreamCopiesMetadata() {
            // Arrange
            string spec = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>Artem.XmlProviders</id>
    <version>2.5</version>
    <authors>Velio Ivanov</authors>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
    <language>en-US</language>
    <licenseUrl>http://somesite/somelicense.txt</licenseUrl>
    <requireLicenseAcceptance>true</requireLicenseAcceptance>
  </metadata>
</package>";

            // Act
            PackageBuilder builder = new PackageBuilder(spec.AsStream(), null);

            // Assert
            Assert.AreEqual("Artem.XmlProviders", builder.Id);
            Assert.AreEqual(new Version(2, 5), builder.Version);
            Assert.AreEqual(1, builder.Authors.Count);
            Assert.AreEqual("Velio Ivanov", builder.Authors[0]);
            Assert.AreEqual("en-US", builder.Language);
            Assert.AreEqual("Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).", builder.Description);
            Assert.AreEqual(new Uri("http://somesite/somelicense.txt"), builder.LicenseUrl);
            Assert.IsTrue(builder.RequireLicenseAcceptance);
        }

        [TestMethod]
        public void PackageBuilderThrowsWhenLicenseUrlIsPresentButEmpty() {
            // Arrange
            string spec = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>Artem.XmlProviders</id>
    <version>2.5</version>
    <authors>
      <author>Velio Ivanov</author>
    </authors>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
    <language>en-US</language>
    <licenseUrl></licenseUrl>
    <requireLicenseAcceptance>true</requireLicenseAcceptance>
  </metadata>
</package>";

            // Act
            ExceptionAssert.Throws<InvalidOperationException>(() => new PackageBuilder(spec.AsStream(), null));
        }

        [TestMethod]
        public void PackageBuilderThrowsWhenLicenseUrlIsWhiteSpace() {
            // Arrange
            string spec = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>Artem.XmlProviders</id>
    <version>2.5</version>
    <authors>
      <author>Velio Ivanov</author>
    </authors>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
    <language>en-US</language>
    <licenseUrl>    </licenseUrl>
    <requireLicenseAcceptance>true</requireLicenseAcceptance>
  </metadata>
</package>";

            // Act
            ExceptionAssert.Throws<InvalidOperationException>(() => new PackageBuilder(spec.AsStream(), null));
        }

        [TestMethod]
        public void PackageBuilderThrowsWhenLicenseUrlIsMalformed() {
            // Arrange
            string spec = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>Artem.XmlProviders</id>
    <version>2.5</version>
    <authors>Velio Ivanov</authors>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
    <language>en-US</language>
    <licenseUrl>this-is-a-malformed-url</licenseUrl>
    <requireLicenseAcceptance>true</requireLicenseAcceptance>
  </metadata>
</package>";

            // Act
            ExceptionAssert.Throws<UriFormatException>(() => new PackageBuilder(spec.AsStream(), null));
        }
    }
}
