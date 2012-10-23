using System.Linq;
using NuGet.Analysis.Rules;
using Xunit;

namespace NuGet.Test.Analysis
{
    public class InitScriptNotUnderToolsRuleTest
    {
        [Fact]
        public void InitFileUnderToolsGenerateNoIssues()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("A", "1.0", tools: new[] { "init.ps1", "install.ps1", "uninstall.ps1" });

            // Act
            var issues = new InitScriptNotUnderToolsRule().Validate(package);

            // Assert
            Assert.False(issues.Any());
        }

        [Fact]
        public void InitFileNotUnderToolsGenerateIssues()
        {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A", "1.0", tools: new[] { "net40\\init.ps1", "sl3\\init.ps1", "uninstall.ps1", "init.ps1", "winrt45\\install.ps1" });

            // Act
            var issues = new InitScriptNotUnderToolsRule().Validate(package).ToList();

            // Assert
            Assert.Equal(2, issues.Count);
            AssertIssue(issues[0], "tools\\net40\\init.ps1");
            AssertIssue(issues[1], "tools\\sl3\\init.ps1");
        }

        private void AssertIssue(PackageIssue packageIssue, string file)
        {
            Assert.Equal("Init.ps1 script will be ignored.", packageIssue.Title);
            Assert.Equal("Place the file directly under 'tools' folder.", packageIssue.Solution);
            Assert.Equal("The file '" + file + "' will be ignored by NuGet because it is not directly under 'tools' folder.", packageIssue.Description);
        }

    }
}
