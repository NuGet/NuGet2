using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Analysis.Rules;
using Xunit;

namespace NuGet.Test.Analysis
{

    public class MisplacedFileRuleTest
    {
        [Fact]
        public void FilesPlacedUnderContentProduceIssues()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("A", content: new[] { "web.config", @"net40\jquery.ui.js", "jQuery.js" });
            var rule = new MisplacedFileRule();

            // Act
            var issues = rule.Validate(package).ToList();

            // Assert
            Assert.Equal(2, issues.Count);
            AssertIssues(issues, new[] { "content\\web.config", "content\\jQuery.js" });
        }

        [Fact]
        public void FilesPlacedInsideFrameworkFolderHasNoIssue()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("A", 
                assemblyReferences: new[] { "lib\\net\\abc.dll" }, 
                content: new[] { "portable\\test.js" });
            var rule = new MisplacedFileRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.False(issues.Any());
        }

        [Fact]
        public void AssemblyPlacedUnderLibHasOneIssue()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("A", assemblyReferences: new[] { "lib\\abc.exe" });
            var rule = new MisplacedFileRule();

            // Act
            var issues = rule.Validate(package).ToList();

            // Assert
            AssertIssues(issues, new[] { "lib\\abc.exe" });
        }

        [Fact]
        public void TwoAssembliesPlacedUnderLibHasTwoIssues()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("A", assemblyReferences: new[] { "lib\\abc.dll", "lib\\def.winmd" });
            var rule = new MisplacedFileRule();

            // Act
            var issues = rule.Validate(package).ToList();

            // Assert
            AssertIssues(issues, new[] { "lib\\abc.dll", "lib\\def.winmd" });
        }

        [Fact]
        public void TwoAssembliesPlacedOutsideLibHasOneIssues()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("A", assemblyReferences: new[] { "content\\net40\\abc.exe", "tools\\net45\\def.winmd" });
            var rule = new MisplacedFileRule();

            // Act
            IList<PackageIssue> issues = rule.Validate(package).ToList();

            // Assert
            Assert.Equal(1, issues.Count);

            PackageIssueTestHelper.AssertPackageIssue(
                issues[0],
                "Assembly outside lib folder.",
                "The assembly 'tools\\net45\\def.winmd' is not inside the 'lib' folder and hence it won't be added as reference when the package is installed into a project.",
                "Move it into the 'lib' folder if it should be referenced."
                );
        }

        private static void AssertIssues(List<PackageIssue> issues, string[] fileNames)
        {
            Assert.Equal(fileNames.Length, issues.Count);

            for (int i = 0; i < fileNames.Length; i++)
            {
                PackageIssueTestHelper.AssertPackageIssue(
                    issues[i],
                    "File not inside a framework folder.",
                    string.Format(
                        "The file '{0}' is directly placed under the '{1}' folder. Support for file paths that do not specify frameworks will be deprecated in the future.",
                        fileNames[i],
                        Path.GetDirectoryName(fileNames[i])),
                    "Move it into a framework-specific folder and run the pack command specifying the package type."
                );
            }
        }
    }
}
