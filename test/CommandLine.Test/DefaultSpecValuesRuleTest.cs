using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;

namespace NuGet.Test {
    public class DefaultSpecValuesRuleTest {
        [Fact]
        public void RuleReturnsIssueIfProjectUrlIsSampleValue() {
            // Arrange
            var package = new Mock<IPackage>();
            package.Setup(c => c.ProjectUrl).Returns(new Uri("http://PROJECT_URL_HERE_OR_DELETE_THIS_LINE"));
            var rule = new DefaultManifestValuesRule();

            // Act
            var result = rule.Validate(package.Object);

            // Assert
            Assert.True(result.Any());
        }

        [Fact]
        public void RuleReturnsIssueIfIconUrlIsSampleValue() {
            // Arrange
            var package = new Mock<IPackage>();
            package.Setup(c => c.IconUrl).Returns(new Uri("http://ICON_URL_HERE_OR_DELETE_THIS_LINE"));
            var rule = new DefaultManifestValuesRule();

            // Act
            var result = rule.Validate(package.Object);

            // Assert
            Assert.True(result.Any());
        }

        [Fact]
        public void RuleReturnsIssueIfLicenseUrlIsSampleValue() {
            // Arrange
            var package = new Mock<IPackage>();
            package.Setup(c => c.LicenseUrl).Returns(new Uri("http://LICENSE_URL_HERE_OR_DELETE_THIS_LINE"));
            var rule = new DefaultManifestValuesRule();

            // Act
            var result = rule.Validate(package.Object);

            // Assert
            Assert.True(result.Any());
        }

        [Fact]
        public void RuleReturnsIssueIfTagIsSampleValue() {
            // Arrange
            var package = new Mock<IPackage>();
            package.Setup(c => c.Tags).Returns("Tag1 Tag2");
            var rule = new DefaultManifestValuesRule();

            // Act
            var result = rule.Validate(package.Object);

            // Assert
            Assert.True(result.Any());
        }

        [Fact]
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
            Assert.True(result.Any());
        }
    }
}
