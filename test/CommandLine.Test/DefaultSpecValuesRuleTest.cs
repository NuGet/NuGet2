using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace NuGet.Test {
    [TestClass]
    public class DefaultSpecValuesRuleTest {
        [TestMethod]
        public void RuleReturnsIssueIfProjectUrlIsSampleValue() {
            // Arrange
            var package = new Mock<IPackage>();
            package.Setup(c => c.ProjectUrl).Returns(new Uri("http://PROJECT_URL_HERE_OR_DELETE_THIS_LINE"));
            var rule = new DefaultManifestValuesRule();

            // Act
            var result = rule.Validate(package.Object);

            // Assert
            Assert.IsTrue(result.Any());
            Assert.AreEqual("Remove sample nuspec values.", result.First().Title);
            Assert.AreEqual("The value \"http://PROJECT_URL_HERE_OR_DELETE_THIS_LINE\" for ProjectUrl is a sample value and should be removed.", result.First().Description);
            Assert.AreEqual("Remove this value from the nuspec and rebuild your package.", result.First().Solution);
        }

        [TestMethod]
        public void RuleReturnsIssueIfIconUrlIsSampleValue() {
            // Arrange
            var package = new Mock<IPackage>();
            package.Setup(c => c.IconUrl).Returns(new Uri("http://ICON_URL_HERE_OR_DELETE_THIS_LINE"));
            var rule = new DefaultManifestValuesRule();

            // Act
            var result = rule.Validate(package.Object);

            // Assert
            Assert.IsTrue(result.Any());
            Assert.AreEqual("Remove sample nuspec values.", result.First().Title);
            Assert.AreEqual("The value \"http://ICON_URL_HERE_OR_DELETE_THIS_LINE\" for IconUrl is a sample value and should be removed.", result.First().Description);
            Assert.AreEqual("Remove this value from the nuspec and rebuild your package.", result.First().Solution);
        }

        [TestMethod]
        public void RuleReturnsIssueIfLicenseUrlIsSampleValue() {
            // Arrange
            var package = new Mock<IPackage>();
            package.Setup(c => c.LicenseUrl).Returns(new Uri("http://LICENSE_URL_HERE_OR_DELETE_THIS_LINE"));
            var rule = new DefaultManifestValuesRule();

            // Act
            var result = rule.Validate(package.Object);

            // Assert
            Assert.IsTrue(result.Any());
            Assert.AreEqual("Remove sample nuspec values.", result.First().Title);
            Assert.AreEqual("The value \"http://LICENSE_URL_HERE_OR_DELETE_THIS_LINE\" for LicenseUrl is a sample value and should be removed.", result.First().Description);
            Assert.AreEqual("Remove this value from the nuspec and rebuild your package.", result.First().Solution);
        }

        [TestMethod]
        public void RuleReturnsIssueIfTagIsSampleValue() {
            // Arrange
            var package = new Mock<IPackage>();
            package.Setup(c => c.Tags).Returns("Tag1 Tag2");
            var rule = new DefaultManifestValuesRule();

            // Act
            var result = rule.Validate(package.Object);

            // Assert
            Assert.IsTrue(result.Any());
            Assert.AreEqual("Remove sample nuspec values.", result.First().Title);
            Assert.AreEqual("The value \"Tag1 Tag2\" for Tags is a sample value and should be removed.", result.First().Description);
            Assert.AreEqual("Remove this value from the nuspec and rebuild your package.", result.First().Solution);
        }

        [TestMethod]
        public void RuleReturnsIssueIfReleaseNotesIsSampleValue() {
            // Arrange
            var package = new Mock<IPackage>();
            package.Setup(c => c.ReleaseNotes).Returns("Summary of changes made in this release of the package.");
            var rule = new DefaultManifestValuesRule();

            // Act
            var result = rule.Validate(package.Object);

            // Assert
            Assert.IsTrue(result.Any());
            Assert.AreEqual("Remove sample nuspec values.", result.First().Title);
            Assert.AreEqual("The value \"Summary of changes made in this release of the package.\" for ReleaseNotes is a sample value and should be removed.", result.First().Description);
            Assert.AreEqual("Remove this value from the nuspec and rebuild your package.", result.First().Solution);
        }

        [TestMethod]
        public void RuleReturnsIssueIfDescriptionIsSampleValue() {
            // Arrange
            var package = new Mock<IPackage>();
            package.Setup(c => c.Description).Returns("Package description");
            var rule = new DefaultManifestValuesRule();

            // Act
            var result = rule.Validate(package.Object);

            // Assert
            Assert.IsTrue(result.Any());
            Assert.AreEqual("Remove sample nuspec values.", result.First().Title);
            Assert.AreEqual("The value \"Package description\" for Description is a sample value and should be removed.", result.First().Description);
            Assert.AreEqual("Remove this value from the nuspec and rebuild your package.", result.First().Solution);
        }

        [TestMethod]
        public void RuleReturnsIssueIfDependencyIsSampleValue() {
            // Arrange
            var package = new Mock<IPackage>();
            var dependencies = new List<PackageDependency> {
                new PackageDependency("SampleDependency", new VersionSpec(new Version("1.0")))
            };
            package.Setup(c => c.Dependencies).Returns(dependencies);
            var rule = new DefaultManifestValuesRule();

            // Act
            var result = rule.Validate(package.Object);

            // Assert
            Assert.IsTrue(result.Any());
        }
    }
}
