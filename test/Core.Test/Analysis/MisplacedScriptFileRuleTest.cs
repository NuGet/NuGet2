using System.Collections.Generic;
using System.Linq;
using NuGet.Analysis.Rules;
using Xunit;

namespace NuGet.Test.Analysis
{
    public class MisplacedScriptFileRuleTest
    {
        [Fact]
        public void NoScriptHasNoIssue()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("A", content: new[] { "web.config", "jQuery.js" });
            var rule = new MisplacedScriptFileRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.False(issues.Any());
        }

        [Fact]
        public void ScriptsOutsideToolsFolder()
        {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A",
                content: new[] { "install.ps1" },
                assemblyReferences: new[] { "init.ps1" }
            );
            var rule = new MisplacedScriptFileRule();

            // Act
            IList<PackageIssue> issues = rule.Validate(package).ToList();

            // Assert
            Assert.Equal(2, issues.Count);

            PackageIssueTestHelper.AssertPackageIssue(
                issues[0],
                "PowerShell file outside tools folder.",
                "The script file 'content\\install.ps1' is outside the 'tools' folder and hence will not be executed during installation of this package.",
                "Move it into the 'tools' folder.");

            PackageIssueTestHelper.AssertPackageIssue(
                issues[1],
                "PowerShell file outside tools folder.",
                "The script file 'init.ps1' is outside the 'tools' folder and hence will not be executed during installation of this package.",
                "Move it into the 'tools' folder.");
        }

        [Fact]
        public void UnrecognizedScriptsInsideToolsFolder()
        {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A",
                tools: new[] { "hello.ps1", "install.ps1", "abc.ps1" }
            );
            var rule = new MisplacedScriptFileRule();

            // Act
            IList<PackageIssue> issues = rule.Validate(package).ToList();

            // Assert
            Assert.Equal(2, issues.Count);

            PackageIssueTestHelper.AssertPackageIssue(
                issues[0],
                "Unrecognized PowerShell file.",
                "The script file 'tools\\hello.ps1' is not recognized by NuGet and hence will not be executed during installation of this package.",
                "Rename it to install.ps1, uninstall.ps1 or init.ps1 and place it directly under 'tools'.");

            PackageIssueTestHelper.AssertPackageIssue(
                issues[1],
                "Unrecognized PowerShell file.",
                "The script file 'tools\\abc.ps1' is not recognized by NuGet and hence will not be executed during installation of this package.",
                "Rename it to install.ps1, uninstall.ps1 or init.ps1 and place it directly under 'tools'.");
        }

        [Fact]
        public void InstallScriptUnderFrameworkFolderDoesNotIssueWarning()
        {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A",
                tools: new[] { "init.ps1", "portable-wp8+sl4\\install.ps1", "silverlight5\\uninstall.ps1" }
            );
            var rule = new MisplacedScriptFileRule();

            // Act
            IList<PackageIssue> issues = rule.Validate(package).ToList();

            // Assert
            Assert.Equal(0, issues.Count);
        }
    }
}