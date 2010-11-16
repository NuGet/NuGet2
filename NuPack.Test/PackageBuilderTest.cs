using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace NuGet.Test {
    [TestClass]
    public class PackageBuilderTest {
        [TestMethod]
        public void OwnersFallsBackToAuthorsIfNoneSpecified() {
            // Arrange
            PackageBuilder builder = new PackageBuilder() {
                Id = "A",
                Version = new Version("1.0"),
                Description = "Description"
            };
            builder.Authors.Add("David");
            var ms = new MemoryStream();

            // Act
            Manifest.Create(builder).Save(ms);
            ms.Seek(0, SeekOrigin.Begin);

            // Assert
            Assert.AreEqual(@"<?xml version=""1.0""?>
<package xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <metadata>
    <id>A</id>
    <version>1.0</version>
    <authors>David</authors>
    <owners>David</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Description</description>
  </metadata>
</package>", ms.ReadToEnd());
        }

        [TestMethod]
        public void CreatePackageTrimsExtraWhitespace() {
            // Arrange
            PackageBuilder builder = new PackageBuilder() {
                Id = "                 A                 ",
                Version = new Version("1.0"),
                Description = "Descriptions                                         ",
                Summary = "                            Summary",
                Language = "     en-us   ",
            };
            builder.Authors.Add("David");
            builder.Owners.Add("John");
            builder.Tags.Add("t1");
            builder.Tags.Add("t2");
            builder.Tags.Add("t3");
            builder.Dependencies.Add(new PackageDependency("     X   "));
            var ms = new MemoryStream();

            // Act
            Manifest.Create(builder).Save(ms);
            ms.Seek(0, SeekOrigin.Begin);

            // Assert
            Assert.AreEqual(@"<?xml version=""1.0""?>
<package xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <metadata>
    <id>A</id>
    <version>1.0</version>
    <authors>David</authors>
    <owners>John</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Descriptions</description>
    <summary>Summary</summary>
    <language>en-us</language>
    <tags>#t1 #t2 #t3</tags>
    <dependencies>
      <dependency id=""X"" />
    </dependencies>
  </metadata>
</package>", ms.ReadToEnd());
        }

        [TestMethod]
        public void VersionFormatIsPreserved() {
            // Arrange
            PackageBuilder builder = new PackageBuilder() {
                Id = "A",
                Version = new Version("1.0"),
                Description = "Descriptions",
                Summary = "Summary",
            };
            builder.Authors.Add("David");
            builder.Dependencies.Add(new PackageDependency("B", new VersionSpec {
                MinVersion = new Version("1.0"),
                IsMinInclusive = true
            }));
            builder.Dependencies.Add(new PackageDependency("C", new VersionSpec {
                MinVersion = new Version("1.0"),
                MaxVersion = new Version("5.0"),
                IsMinInclusive = false
            }));
            var ms = new MemoryStream();

            // Act
            Manifest.Create(builder).Save(ms);
            ms.Seek(0, SeekOrigin.Begin);

            // Assert
            Assert.AreEqual(@"<?xml version=""1.0""?>
<package xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <metadata>
    <id>A</id>
    <version>1.0</version>
    <authors>David</authors>
    <owners>David</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Descriptions</description>
    <summary>Summary</summary>
    <dependencies>
      <dependency id=""B"" version=""1.0"" />
      <dependency id=""C"" version=""(1.0, 5.0)"" />
    </dependencies>
  </metadata>
</package>", ms.ReadToEnd());
        }


        [TestMethod]
        public void SavingPackageWithDuplicateDependenciesThrows() {
            // Arrange
            PackageBuilder builder = new PackageBuilder() {
                Id = "A",
                Version = new Version("1.0"),
                Description = "Descriptions",
                Summary = "Summary",
            };
            builder.Authors.Add("David");
            builder.Dependencies.Add(new PackageDependency("B", new VersionSpec {
                MinVersion = new Version("1.0"),
                IsMinInclusive = true
            }));
            builder.Dependencies.Add(new PackageDependency("B", new VersionSpec {
                MinVersion = new Version("1.0"),
                MaxVersion = new Version("5.0"),
                IsMinInclusive = false
            }));
            var ms = new MemoryStream();

            // Act
            ExceptionAssert.Throws<InvalidOperationException>(() => Manifest.Create(builder).Save(ms), "'A' already has a dependency defined for 'B'.");
        }

        [TestMethod]
        public void SavingPackageWithInvalidDependencyRangeThrows() {
            // Arrange
            PackageBuilder builder = new PackageBuilder() {
                Id = "A",
                Version = new Version("1.0"),
                Description = "Descriptions",
                Summary = "Summary",
            };
            builder.Authors.Add("David");
            builder.Dependencies.Add(new PackageDependency("B", new VersionSpec {
                MinVersion = new Version("1.0"),
                MaxVersion = new Version("1.0")
            }));
            var ms = new MemoryStream();

            // Act
            ExceptionAssert.Throws<InvalidOperationException>(() => Manifest.Create(builder).Save(ms), "Dependency 'B' has an invalid version.");
        }

        [TestMethod]
        public void SavingPackageWithInvalidDependencyVersionMaxLessThanMinThrows() {
            // Arrange
            PackageBuilder builder = new PackageBuilder() {
                Id = "A",
                Version = new Version("1.0"),
                Description = "Descriptions",
                Summary = "Summary",
            };
            builder.Authors.Add("David");
            builder.Dependencies.Add(new PackageDependency("B", new VersionSpec {
                MinVersion = new Version("2.0"),
                MaxVersion = new Version("1.0")
            }));
            var ms = new MemoryStream();

            // Act
            ExceptionAssert.Throws<InvalidOperationException>(() => Manifest.Create(builder).Save(ms), "Dependency 'B' has an invalid version.");
        }

        [TestMethod]
        public void SaveThrowsIfRequiredPropertiesAreMissing() {
            // Arrange
            var builder = new PackageBuilder();
            builder.Files.Add(new Mock<IPackageFile>().Object);

            // Act & Assert
            ExceptionAssert.Throws<ValidationException>(() => builder.Save(new MemoryStream()), @"Id is required.
Version is required.
Authors is required.
Description is required.");
        }

        [TestMethod]
        public void SaveThrowsIfNoFilesOrDependencies() {
            // Arrange
            var builder = new PackageBuilder();
            builder.Id = "A";
            builder.Version = new Version("1.0");
            builder.Description = "Description";

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => builder.Save(new MemoryStream()), "Cannot create a package that has no dependencies nor content.");
        }

        [TestMethod]
        public void PackageBuilderThrowsIfXmlIsMalformed() {
            // Arrange
            string spec1 = "kjdkfj";
            string spec2 = @"<?xml version=""1.0"" encoding=""utf-8""?>";
            string spec3 = @"<?xml version=""1.0"" encoding=""utf-8""?><package />";
            string spec4 = @"<?xml version=""1.0"" encoding=""utf-8""?><package><metadata></metadata></package>";

            // Act and Assert
            ExceptionAssert.Throws<XmlException>(() => new PackageBuilder(spec1.AsStream(), null), "Data at the root level is invalid. Line 1, position 1.");
            ExceptionAssert.Throws<XmlException>(() => new PackageBuilder(spec2.AsStream(), null), "Root element is missing.");
            ExceptionAssert.Throws<ValidationException>(() => new PackageBuilder(spec3.AsStream(), null), @"Id is required.
Version is required.
Authors is required.
Description is required.");
            ExceptionAssert.Throws<ValidationException>(() => new PackageBuilder(spec4.AsStream(), null), @"Id is required.
Version is required.
Authors is required.
Description is required.");
        }


        [TestMethod]
        public void PackageBuilderRequiredFields() {
            // Arrange
            string spec = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package><metadata>
    <id>Artem.XmlProviders</id>
    <version>2.5</version>
    <authors>Velio Ivanov</authors>
    <language>en-us</language>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
  </metadata></package>";

            string badSpec1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package><metadata>
    <version>2.5</version>
    <authors>Velio Ivanov</authors>
    <language>en-us</language>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
  </metadata></package>";

            string badSpec2 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package><metadata>
    <id>Artem.XmlProviders</id>
    <authors>Velio Ivanov</authors>
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
    <authors>Velio Ivanov</authors>
    <language>en-us</language>
  </metadata></package>";

            string badDependencies = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package><metadata>
    <id>Artem.XmlProviders</id>
    <version>2.5</version>
    <authors>Velio Ivanov</authors>
    <language>en-us</language>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
    <dependencies>
        <dependency />
    </dependencies>
  </metadata></package>";

            string missingFileSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package><metadata>
    <id>Artem.XmlProviders</id>
    <version>2.5</version>
    <authors>Velio Ivanov</authors>
    <language>en-us</language>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
    <dependencies>
        <dependency id=""foo"" />
    </dependencies>
  </metadata>
  <files>
    <file />
  </files>
</package>";

            // Act
            var packageBuilder = new PackageBuilder(spec.AsStream(), null);

            // Assert
            ExceptionAssert.Throws<ValidationException>(() => new PackageBuilder(badSpec1.AsStream(), null), "Id is required.");
            ExceptionAssert.Throws<ValidationException>(() => new PackageBuilder(badSpec2.AsStream(), null), "Version is required.");
            ExceptionAssert.Throws<ValidationException>(() => new PackageBuilder(badSpec3.AsStream(), null), "Authors is required.");
            ExceptionAssert.Throws<ValidationException>(() => new PackageBuilder(badSpec4.AsStream(), null), "Description is required.");
            ExceptionAssert.Throws<ValidationException>(() => new PackageBuilder(badDependencies.AsStream(), null), "Dependency Id is required.");
            ExceptionAssert.Throws<ValidationException>(() => new PackageBuilder(missingFileSource.AsStream(), null), "Source is required.");
            Assert.IsNotNull(packageBuilder); // Verify no exception was thrown
        }


        [TestMethod]
        public void ReadingPackageWithWrongTagsFormatFails() {
            // Arrange
            string spec = @"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
    <metadata>
    <id>Artem.XmlProviders  </id>
    <version>2.5</version>
    <title>Some awesome package       </title>
    <authors>Velio Ivanov</authors>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
    <language>en-US</language>
    <licenseUrl>http://somesite/somelicense.txt</licenseUrl>
    <tags>doo gar</tags>
  </metadata>
</package>";

            // Act
            ExceptionAssert.Throws<InvalidOperationException>(() => new PackageBuilder(spec.AsStream(), null), "The 'doo' tag is missing a #");
        }

        [TestMethod]
        public void ReadingManifestWithNamespaceBuilderFromStreamCopiesMetadata() {
            // Arrange
            string spec = @"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
    <metadata>
    <id>Artem.XmlProviders  </id>
    <version>2.5</version>
    <title>Some awesome package       </title>
    <authors>Velio Ivanov</authors>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
    <language>en-US</language>
    <licenseUrl>http://somesite/somelicense.txt</licenseUrl>
    <requireLicenseAcceptance>true</requireLicenseAcceptance>
    <tags>#t1      #t2    #foo-bar</tags>
  </metadata>
</package>";

            // Act
            PackageBuilder builder = new PackageBuilder(spec.AsStream(), null);
            var authors = builder.Authors.ToList();
            var owners = builder.Owners.ToList();
            var tags = builder.Tags.ToList();

            // Assert
            Assert.AreEqual("Artem.XmlProviders", builder.Id);
            Assert.AreEqual(new Version(2, 5), builder.Version);
            Assert.AreEqual("Some awesome package", builder.Title);
            Assert.AreEqual(1, builder.Authors.Count);
            Assert.AreEqual("Velio Ivanov", authors[0]);
            Assert.AreEqual("Velio Ivanov", owners[0]);
            Assert.AreEqual(3, builder.Tags.Count);
            Assert.AreEqual("t1", tags[0]);
            Assert.AreEqual("t2", tags[1]);
            Assert.AreEqual("foo-bar", tags[2]);
            Assert.AreEqual("en-US", builder.Language);
            Assert.AreEqual("Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).", builder.Description);
            Assert.AreEqual(new Uri("http://somesite/somelicense.txt"), builder.LicenseUrl);
            Assert.IsTrue(builder.RequireLicenseAcceptance);
        }

        [TestMethod]
        public void ReadingManifestWithSerializationNamespaceBuilderFromStreamCopiesMetadata() {
            // Arrange
            string spec = @"<?xml version=""1.0""?>
<package xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
    <metadata xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
    <id>Artem.XmlProviders</id>
    <version>2.5</version>
    <title>Some awesome package</title>
    <authors>Velio Ivanov</authors>
    <owners>John Doe</owners>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
    <language>en-US</language>
    <licenseUrl>http://somesite/somelicense.txt</licenseUrl>
    <requireLicenseAcceptance>true</requireLicenseAcceptance>
  </metadata>
</package>";

            // Act
            PackageBuilder builder = new PackageBuilder(spec.AsStream(), null);
            var authors = builder.Authors.ToList();
            var owners = builder.Owners.ToList();

            // Assert
            Assert.AreEqual("Artem.XmlProviders", builder.Id);
            Assert.AreEqual(new Version(2, 5), builder.Version);
            Assert.AreEqual("Some awesome package", builder.Title);
            Assert.AreEqual(1, builder.Authors.Count);
            Assert.AreEqual("Velio Ivanov", authors[0]);
            Assert.AreEqual(1, builder.Owners.Count);
            Assert.AreEqual("John Doe", owners[0]);
            Assert.AreEqual("en-US", builder.Language);
            Assert.AreEqual("Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).", builder.Description);
            Assert.AreEqual(new Uri("http://somesite/somelicense.txt"), builder.LicenseUrl);
            Assert.IsTrue(builder.RequireLicenseAcceptance);
        }

        [TestMethod]
        public void ReadingPackageManifestFromStreamCopiesMetadata() {
            // Arrange
            string spec = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>Artem.XmlProviders</id>
    <version>2.5</version>
    <title>Some awesome package</title>
    <authors>Velio Ivanov</authors>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
    <language>en-US</language>
    <licenseUrl>http://somesite/somelicense.txt</licenseUrl>
    <requireLicenseAcceptance>true</requireLicenseAcceptance>
  </metadata>
</package>";

            // Act
            PackageBuilder builder = new PackageBuilder(spec.AsStream(), null);
            var authors = builder.Authors.ToList();

            // Assert
            Assert.AreEqual("Artem.XmlProviders", builder.Id);
            Assert.AreEqual(new Version(2, 5), builder.Version);
            Assert.AreEqual("Some awesome package", builder.Title);
            Assert.AreEqual(1, builder.Authors.Count);
            Assert.AreEqual("Velio Ivanov", authors[0]);
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
    <authors>Velio Ivanov</authors>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
    <language>en-US</language>
    <licenseUrl></licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
  </metadata>
</package>";

            // Act
            ExceptionAssert.Throws<ValidationException>(() => new PackageBuilder(spec.AsStream(), null), "LicenseUrl cannot be empty.");
        }

        [TestMethod]
        public void PackageBuilderThrowsWhenLicenseUrlIsWhiteSpace() {
            // Arrange
            string spec = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>Artem.XmlProviders</id>
    <version>2.5</version>
    <authors>Velio Ivanov</authors>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
    <language>en-US</language>
    <licenseUrl>    </licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
  </metadata>
</package>";

            // Act
            ExceptionAssert.Throws<ValidationException>(() => new PackageBuilder(spec.AsStream(), null), "LicenseUrl cannot be empty.");
        }

        [TestMethod]
        public void PackageBuilderRequireLicenseAcceptedWithoutLicenseUrlThrows() {
            // Arrange
            string spec = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>Artem.XmlProviders</id>
    <version>2.5</version>
    <authors>Velio Ivanov</authors>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
    <language>en-US</language>
    <licenseUrl></licenseUrl>
    <requireLicenseAcceptance>true</requireLicenseAcceptance>
  </metadata>
</package>";

            // Act
            ExceptionAssert.Throws<ValidationException>(() => new PackageBuilder(spec.AsStream(), null), @"LicenseUrl cannot be empty.
Enabling license acceptance requires a license url.");
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
            ExceptionAssert.Throws<UriFormatException>(() => new PackageBuilder(spec.AsStream(), null), "Invalid URI: The format of the URI could not be determined.");
        }
    }
}