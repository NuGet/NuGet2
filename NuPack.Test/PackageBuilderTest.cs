using System;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuPack.Test {
    [TestClass]
    public class PackageBuilderTest {
        [TestMethod]
        public void ReadingPackageBuilderFromPackageCopiesAllPropertiesAndFiles() {
            // Arrange
            var package = PackageUtility.CreatePackage("A", "1.0", new[] { "a", "b", "c" });

            // Act
            PackageBuilder builder = PackageBuilder.ReadFrom(package);

            // Assert
            Assert.AreEqual(package.Id, builder.Id);
            Assert.AreEqual(package.Language, builder.Language);
            Assert.AreEqual(package.Version, builder.Version);
            Assert.AreEqual(package.Description, builder.Description);
            Assert.AreEqual(package.Category, builder.Category);
            Assert.AreEqual(package.LicenseUrl, builder.LicenseUrl);
            CollectionAssert.AreEqual(package.Dependencies.ToList(), builder.Dependencies);
            CollectionAssert.AreEqual(package.GetFiles().ToList(), builder.Files);
        }

        [TestMethod]
        public void PackageBuilderThrowsIfXmlIsMalformed() {
            // Arrange
            string spec1 = "kjdkfj";
            string spec2 = @"<?xml version=""1.0"" encoding=""utf-8""?>";
            string spec3 = @"<?xml version=""1.0"" encoding=""utf-8""?><package />";
            string spec4 = @"<?xml version=""1.0"" encoding=""utf-8""?><package><metadata></metadata></package>";

            // Act and Assert
            ExceptionAssert.Throws<XmlException>(() => PackageBuilder.ReadFrom(spec1.AsStream()));
            ExceptionAssert.Throws<XmlException>(() => PackageBuilder.ReadFrom(spec2.AsStream()));
            ExceptionAssert.Throws<InvalidOperationException>(() => PackageBuilder.ReadFrom(spec3.AsStream()));
            ExceptionAssert.Throws<InvalidOperationException>(() => PackageBuilder.ReadFrom(spec4.AsStream()));
        }


        [TestMethod]
        public void PackageBuilderRequiredFields() {
            // Arrange
            string spec = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package><metadata>
    <id>Artem.XmlProviders</id>
    <version>2.5</version>
    <authors><author>Velio Ivanov</author></authors>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
  </metadata></package>";
            
            string badSpec1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package><metadata>
    <version>2.5</version>
    <authors><author>Velio Ivanov</author></authors>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
  </metadata></package>";
            
            string badSpec2 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package><metadata>
    <id>Artem.XmlProviders</id>
    <authors><author>Velio Ivanov</author></authors>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
  </metadata></package>";

            string badSpec3 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package><metadata>
    <id>Artem.XmlProviders</id>
    <version>2.5</version>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
  </metadata></package>";

            string badSpec4 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package><metadata>
    <id>Artem.XmlProviders</id>
    <version>2.5</version>
    <authors><author>Velio Ivanov</author></authors>
  </metadata></package>";
            
            // Act
            var packageBuilder = PackageBuilder.ReadFrom(spec.AsStream());

            // Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => PackageBuilder.ReadFrom(badSpec1.AsStream()));
            ExceptionAssert.Throws<InvalidOperationException>(() => PackageBuilder.ReadFrom(badSpec2.AsStream()));
            ExceptionAssert.Throws<InvalidOperationException>(() => PackageBuilder.ReadFrom(badSpec3.AsStream()));
            ExceptionAssert.Throws<InvalidOperationException>(() => PackageBuilder.ReadFrom(badSpec4.AsStream()));
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
    <authors>
      <author>Velio Ivanov</author>
    </authors>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
    <language>en-US</language>
    <licenseUrl>http://somesite/somelicense.txt</licenseUrl>
    <requireLicenseAcceptance>true</requireLicenseAcceptance>
  </metadata>
</package>";

            // Act
            PackageBuilder builder = PackageBuilder.ReadFrom(spec.AsStream());

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
            ExceptionAssert.Throws<InvalidOperationException>(() => PackageBuilder.ReadFrom(spec.AsStream()));
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
            ExceptionAssert.Throws<InvalidOperationException>(() => PackageBuilder.ReadFrom(spec.AsStream()));
        }

        [TestMethod]
        public void PackageBuilderThrowsWhenLicenseUrlIsMalformed() {
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
    <licenseUrl>this-is-a-malformed-url</licenseUrl>
    <requireLicenseAcceptance>true</requireLicenseAcceptance>
  </metadata>
</package>";

            // Act
            ExceptionAssert.Throws<UriFormatException>(() => PackageBuilder.ReadFrom(spec.AsStream()));
        }
    }
}
