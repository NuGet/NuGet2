using System;
using System.Linq;
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
    }
}
