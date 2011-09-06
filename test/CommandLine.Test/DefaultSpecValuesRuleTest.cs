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
