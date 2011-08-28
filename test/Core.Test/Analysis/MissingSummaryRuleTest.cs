using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Analysis.Rules;

namespace NuGet.Test.Analysis {
    [TestClass]
    public class MissingSummaryRuleTest {

        [TestMethod]
        public void PackageWithShortDescriptionHasNoIssue() {
            // Arrange
            var package = PackageUtility.CreatePackage("A", description: new string('a', 300));
            var rule = new MissingSummaryRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.IsFalse(issues.Any());
        }

        [TestMethod]
        public void PackageWithLongDescriptionYieldOneIssue() {
            // Arrange
            var package = PackageUtility.CreatePackage("A", description: new string('a', 301));
            var rule = new MissingSummaryRule();

            // Act
            IList<PackageIssue> issues = rule.Validate(package).ToList();

            // Assert
            Assert.AreEqual(1, issues.Count);
            PackageIssueTestHelper.AssertPackageIssue(
                issues[0],
                "Consider providing Summary text",
                "The Description text is long but the Summary text is empty. This means the Description text will be truncated in the 'Manage NuGet packages' dialog.",
                "Provide a brief summary of the package in the Summary field.");
        }

        [TestMethod]
        public void PackageWithLongDescriptionAndSummaryHasNoIssue() {
            // Arrange
            var package = PackageUtility.CreatePackage("A", description: new string('a', 301), summary: "summary");
            var rule = new MissingSummaryRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.IsFalse(issues.Any());
        }
    }
}