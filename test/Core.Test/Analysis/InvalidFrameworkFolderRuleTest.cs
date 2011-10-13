using System.Collections.Generic;
using System.Linq;
using NuGet.Analysis.Rules;
using Xunit;

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

        [Fact]
        public void PackageWithInvalidFrameworkNamesHasOneIssue()
        {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A",
                content: new[] { "one", "two" },
                assemblyReferences: new[] { "lib\\coyote ugly\\abc.dll" });
            var rule = new InvalidFrameworkFolderRule();

            // Act
            IList<PackageIssue> issues = rule.Validate(package).ToList();

            // Assert
            Assert.Equal(1, issues.Count);
            PackageIssueTestHelper.AssertPackageIssue(
                issues[0],
                "Invalid framework folder.",
                "The folder 'coyote ugly' under 'lib' is not recognized as a valid framework name.",
                "Rename it to a valid framework name.");
        }
    }
}