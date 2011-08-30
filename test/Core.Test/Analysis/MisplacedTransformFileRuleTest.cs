using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Analysis.Rules;

namespace NuGet.Test.Analysis {
    [TestClass]
    public class MisplacedTransformFileRuleTest {

        [TestMethod]
        public void NoTransformFileHasNoIssue() {
            // Arrange
            var package = PackageUtility.CreatePackage("A", content: new[] { "one.js" });
            var rule = new MisplacedTransformFileRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.IsFalse(issues.Any());
        }

        [TestMethod]
        public void TransformFileInsideContentFolderHasNoIssue() {
            // Arrange
            var package = PackageUtility.CreatePackage("A", content: new[] { "one.js.pp", "web.config.transform" });
            var rule = new MisplacedTransformFileRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.IsFalse(issues.Any());
        }

        [TestMethod]
        public void TransformFileOutsideContentFolderHasIssue() {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A", 
                content: new [] { "say.cs.pp", "app.config.transform" },
                tools: new[] { "web.config.transform" }, 
                assemblyReferences: new[] { "one.cs.pp" } );
            var rule = new MisplacedTransformFileRule();

            // Act
            IList<PackageIssue> issues = rule.Validate(package).ToList();

            // Assert
            Assert.AreEqual(2, issues.Count);

            PackageIssueTestHelper.AssertPackageIssue(
                issues[0],
                "Transform file outside content folder.",
                "The transform file 'tools\\web.config.transform' is outside the 'content' folder and hence will not be transformed during installation of this package.",
                "Move it into the 'content' folder.");

            PackageIssueTestHelper.AssertPackageIssue(
                issues[1],
                "Transform file outside content folder.",
                "The transform file 'one.cs.pp' is outside the 'content' folder and hence will not be transformed during installation of this package.",
                "Move it into the 'content' folder.");
        }
    }
}
