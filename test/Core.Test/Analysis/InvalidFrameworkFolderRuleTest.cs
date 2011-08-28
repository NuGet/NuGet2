using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Analysis.Rules;

namespace NuGet.Test.Analysis {
    [TestClass]
    public class InvalidFrameworkFolderRuleTest {
        [TestMethod]
        public void PackageWithNoLibFolderHasNoIssue() {
            // Arrange
            var package = PackageUtility.CreatePackage("A", content: new[] { "one", "two" });
            var rule = new InvalidFrameworkFolderRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.IsFalse(issues.Any());
        }

        [TestMethod]
        public void PackageWithValidFrameworkNamesHasNoIssue() {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A", 
                content: new[] { "one", "two" },
                assemblyReferences: new[] { "lib\\sl4\\abc.dll" } );
            var rule = new InvalidFrameworkFolderRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.IsFalse(issues.Any());
        }

        [TestMethod]
        public void PackageWithInvalidFrameworkNamesHasOneIssue() {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A",
                content: new[] { "one", "two" },
                assemblyReferences: new[] { "lib\\coyot ugly\\abc.dll" });
            var rule = new InvalidFrameworkFolderRule();

            // Act
            IList<PackageIssue> issues = rule.Validate(package).ToList();

            // Assert
            Assert.AreEqual(1, issues.Count);
            PackageIssueTestHelper.AssertPackageIssue(
                issues[0],
                "Invalid framework folder",
                "The folder 'coyot ugly' under 'lib' is not recognized as a valid framework name.",
                "Rename it to a valid framework name.");
        }
    }
}
