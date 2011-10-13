using System.Collections.Generic;
using System.Linq;
using NuGet.Analysis.Rules;
using Xunit;

namespace NuGet.Test.Analysis
{

    public class MisplacedTransformFileRuleTest
    {

        [Fact]
        public void NoTransformFileHasNoIssue()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("A", content: new[] { "one.js" });
            var rule = new MisplacedTransformFileRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.False(issues.Any());
        }

        [Fact]
        public void TransformFileInsideContentFolderHasNoIssue()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("A", content: new[] { "one.js.pp", "web.config.transform" });
            var rule = new MisplacedTransformFileRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.False(issues.Any());
        }

        [Fact]
        public void TransformFileOutsideContentFolderHasIssue()
        {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A",
                content: new[] { "say.cs.pp", "app.config.transform" },
                tools: new[] { "web.config.transform" },
                assemblyReferences: new[] { "one.cs.pp" });
            var rule = new MisplacedTransformFileRule();

            // Act
            IList<PackageIssue> issues = rule.Validate(package).ToList();

            // Assert
            Assert.Equal(2, issues.Count);

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
