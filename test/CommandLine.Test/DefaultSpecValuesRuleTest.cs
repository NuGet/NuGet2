using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;

namespace NuGet.Test
{
    public class DefaultSpecValuesRuleTest
    {
        [Fact]
        public void RuleReturnsIssueIfProjectUrlIsSampleValue()
        {
            // Arrange
            var package = new Mock<IPackage>();
            package.Setup(c => c.ProjectUrl).Returns(new Uri("http://PROJECT_URL_HERE_OR_DELETE_THIS_LINE"));
            var rule = new DefaultManifestValuesRule();

            // Act
            var result = rule.Validate(package.Object);

            // Assert
            Assert.True(result.Any());
            Assert.Equal("Remove sample nuspec values.", result.First().Title);
            Assert.Equal("The value \"http://PROJECT_URL_HERE_OR_DELETE_THIS_LINE\" for ProjectUrl is a sample value and should be removed.", result.First().Description);
            Assert.Equal("Replace with an appropriate value or remove and it and rebuild your package.", result.First().Solution);
        }

        [Fact]
        public void RuleReturnsIssueIfIconUrlIsSampleValue()
        {
            // Arrange
            var package = new Mock<IPackage>();
            package.Setup(c => c.IconUrl).Returns(new Uri("http://ICON_URL_HERE_OR_DELETE_THIS_LINE"));
            var rule = new DefaultManifestValuesRule();

            // Act
            var result = rule.Validate(package.Object);

            // Assert
            Assert.True(result.Any());
            Assert.Equal("Remove sample nuspec values.", result.First().Title);
            Assert.Equal("The value \"http://ICON_URL_HERE_OR_DELETE_THIS_LINE\" for IconUrl is a sample value and should be removed.", result.First().Description);
            Assert.Equal("Replace with an appropriate value or remove and it and rebuild your package.", result.First().Solution);
        }

        [Fact]
        public void RuleReturnsIssueIfLicenseUrlIsSampleValue()
        {
            // Arrange
            var package = new Mock<IPackage>();
            package.Setup(c => c.LicenseUrl).Returns(new Uri("http://LICENSE_URL_HERE_OR_DELETE_THIS_LINE"));
            var rule = new DefaultManifestValuesRule();

            // Act
            var result = rule.Validate(package.Object);

            // Assert
            Assert.True(result.Any());
            Assert.Equal("Remove sample nuspec values.", result.First().Title);
            Assert.Equal("The value \"http://LICENSE_URL_HERE_OR_DELETE_THIS_LINE\" for LicenseUrl is a sample value and should be removed.", result.First().Description);
            Assert.Equal("Replace with an appropriate value or remove and it and rebuild your package.", result.First().Solution);
        }

        [Fact]
        public void RuleReturnsIssueIfTagIsSampleValue()
        {
            // Arrange
            var package = new Mock<IPackage>();
            package.Setup(c => c.Tags).Returns("Tag1 Tag2");
            var rule = new DefaultManifestValuesRule();

            // Act
            var result = rule.Validate(package.Object);

            // Assert
            Assert.True(result.Any());
            Assert.Equal("Remove sample nuspec values.", result.First().Title);
            Assert.Equal("The value \"Tag1 Tag2\" for Tags is a sample value and should be removed.", result.First().Description);
            Assert.Equal("Replace with an appropriate value or remove and it and rebuild your package.", result.First().Solution);
        }

        [Fact]
        public void RuleReturnsIssueIfReleaseNotesIsSampleValue()
        {
            // Arrange
            var package = new Mock<IPackage>();
            package.Setup(c => c.ReleaseNotes).Returns("Summary of changes made in this release of the package.");
            var rule = new DefaultManifestValuesRule();

            // Act
            var result = rule.Validate(package.Object);

            // Assert
            Assert.True(result.Any());
            Assert.Equal("Remove sample nuspec values.", result.First().Title);
            Assert.Equal("The value \"Summary of changes made in this release of the package.\" for ReleaseNotes is a sample value and should be removed.", result.First().Description);
            Assert.Equal("Replace with an appropriate value or remove and it and rebuild your package.", result.First().Solution);
        }

        [Fact]
        public void RuleReturnsIssueIfDescriptionIsSampleValue()
        {
            // Arrange
            var package = new Mock<IPackage>();
            package.Setup(c => c.Description).Returns("Package description");
            var rule = new DefaultManifestValuesRule();

            // Act
            var result = rule.Validate(package.Object);

            // Assert
            Assert.True(result.Any());
            Assert.Equal("Remove sample nuspec values.", result.First().Title);
            Assert.Equal("The value \"Package description\" for Description is a sample value and should be removed.", result.First().Description);
            Assert.Equal("Replace with an appropriate value or remove and it and rebuild your package.", result.First().Solution);
        }

        [Fact]
        public void RuleReturnsIssueIfTagIsSampleValueWithSpaces()
        {
            // Arrange
            var package = new Mock<IPackage>();
            package.Setup(c => c.Tags).Returns(" Tag1 Tag2 ");
            var rule = new DefaultManifestValuesRule();

            // Act
            var result = rule.Validate(package.Object);

            // Assert
            Assert.True(result.Any());
            Assert.Equal("Remove sample nuspec values.", result.First().Title);
            Assert.Equal("The value \"Tag1 Tag2\" for Tags is a sample value and should be removed.", result.First().Description);
            Assert.Equal("Replace with an appropriate value or remove and it and rebuild your package.", result.First().Solution);
        }

        [Fact]
        public void RuleReturnsIssueIfDependencyIsSampleValue()
        {
            // Arrange
            var package = new Mock<IPackage>();
            var dependencies = new List<PackageDependency> {
                new PackageDependency("SampleDependency", new VersionSpec(new SemanticVersion("1.0")))
            };

            var dependencySets = new List<PackageDependencySet> {
                new PackageDependencySet(null, dependencies)
            };

            package.Setup(c => c.DependencySets).Returns(dependencySets);
            var rule = new DefaultManifestValuesRule();

            // Act
            var result = rule.Validate(package.Object);

            // Assert
            Assert.True(result.Any());
        }
    }
}
