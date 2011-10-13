using System.Collections.Generic;
using System.Linq;
using NuGet.Analysis.Rules;
using Xunit;

namespace NuGet.Test.Analysis
{

    public class MissingSummaryRuleTest
    {

        [Fact]
        public void PackageWithShortDescriptionHasNoIssue()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("A", description: new string('a', 300));
            var rule = new MissingSummaryRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.False(issues.Any());
        }

        [Fact]
        public void PackageWithLongDescriptionYieldOneIssue()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("A", description: new string('a', 301));
            var rule = new MissingSummaryRule();

            // Act
            IList<PackageIssue> issues = rule.Validate(package).ToList();

            // Assert
            Assert.Equal(1, issues.Count);
            PackageIssueTestHelper.AssertPackageIssue(
                issues[0],
                "Consider providing Summary text.",
                "The Description text is long but the Summary text is empty. This means the Description text will be truncated in the 'Manage NuGet Packages' dialog.",
                "Provide a brief summary of the package in the Summary field.");
        }

        [Fact]
        public void PackageWithLongDescriptionAndSummaryHasNoIssue()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("A", description: new string('a', 301), summary: "summary");
            var rule = new MissingSummaryRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.False(issues.Any());
        }
    }
}