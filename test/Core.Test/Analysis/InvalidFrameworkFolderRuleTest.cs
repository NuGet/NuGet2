using System.Collections.Generic;
using System.Linq;
using NuGet.Analysis.Rules;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test.Analysis
{
    public class InvalidFrameworkFolderRuleTest
    {
        [Fact]
        public void PackageWithNoLibFolderHasNoIssue()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("A", content: new[] { "one", "two" });
            var rule = new InvalidFrameworkFolderRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.False(issues.Any());
        }

        [Fact]
        public void PackageWithValidFrameworkNamesHasNoIssue()
        {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A",
                content: new[] { "one", "two" },
                assemblyReferences: new[] { "lib\\sl4\\abc.dll" });
            var rule = new InvalidFrameworkFolderRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.False(issues.Any());
        }

        [Theory]
        [InlineData("coyote ugly")]
        [InlineData("dotnetjunky")]
        [InlineData("en-USA")]
        [InlineData("es-Spain")]
        [InlineData("ent")]
        [InlineData("portable-net 4")]
        [InlineData("portable-net4+portable")]
        [InlineData("portable-net4+sl-wp")]
        [InlineData("portable")]
        public void PackageWithInvalidFrameworkNamesHasOneIssue(string folder)
        {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A",
                content: new[] { "one", "two" },
                assemblyReferences: new[] { "lib\\" + folder + "\\abc.dll" });
            var rule = new InvalidFrameworkFolderRule();

            // Act
            IList<PackageIssue> issues = rule.Validate(package).ToList();

            // Assert
            Assert.Equal(1, issues.Count);
            PackageIssueTestHelper.AssertPackageIssue(
                issues[0],
                "Invalid framework folder.",
                "The folder '" + folder + "' under 'lib' is not recognized as a valid framework name or a supported culture identifier.",
                "Rename it to a valid framework name.");
        }

        [Theory]
        [InlineData("en")]
        [InlineData("en-US")]
        [InlineData("fr-FR")]
        [InlineData("fr")]
        public void PackageWithValidCultureFolderHasNoIssue(string culture)
        {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A",
                content: new[] { "one", "two" },
                assemblyReferences: new[] { "lib\\" + culture + "\\abc.dll" }, 
                language: culture);
            var rule = new InvalidFrameworkFolderRule();

            // Act
            IList<PackageIssue> issues = rule.Validate(package).ToList();

            // Assert
            Assert.Equal(0, issues.Count);
        }

        [Theory]
        [InlineData("en")]
        [InlineData("en-US")]
        [InlineData("fr-FR")]
        [InlineData("fr")]
        public void PackageWithValidCultureFolderButDoesNotSetLanguageAttributeHasOneIssue(string culture)
        {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A",
                content: new[] { "one", "two" },
                assemblyReferences: new[] { "lib\\" + culture + "\\abc.dll" });
            var rule = new InvalidFrameworkFolderRule();

            // Act
            IList<PackageIssue> issues = rule.Validate(package).ToList();

            // Assert
            Assert.Equal(1, issues.Count);
            PackageIssueTestHelper.AssertPackageIssue(
                issues[0],
                "Invalid framework folder.",
                "The folder '" + culture + "' under 'lib' is not recognized as a valid framework name or a supported culture identifier.",
                "Rename it to a valid framework name.");
        }
    }
}