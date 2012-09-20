using System.Linq;
using NuGet.Analysis.Rules;
using Xunit;

namespace NuGet.Test.Analysis
{
    public class WinRTNameObsoleteRuleTest 
    {
        [Fact]
        public void WinRTGenerateAnIssueForWinRTUnderContent()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("A", "1.0", new[] { "winRT\\one.txt" });

            var rule = new WinRTNameIsObsoleteRule();

            // Act
            var results = rule.Validate(package).ToList();

            // Assert
            Assert.Equal(1, results.Count);
            PackageIssueTestHelper.AssertPackageIssue(
                results[0],
                "The framework name 'WinRT' is obsolete.",
                "The file at 'content\\winRT\\one.txt' uses the obsolete 'WinRT' as the framework folder.",
                "Replace 'WinRT' or 'WinRT45' with 'NetCore45'.");
        }

        [Fact]
        public void WinRTGenerateAnIssueForWinRTUnderLib()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("A", "1.0", assemblyReferences: new[] { "lib\\winRT45\\one.dll" });

            var rule = new WinRTNameIsObsoleteRule();

            // Act
            var results = rule.Validate(package).ToList();

            // Assert
            Assert.Equal(1, results.Count);
            PackageIssueTestHelper.AssertPackageIssue(
                results[0],
                "The framework name 'WinRT' is obsolete.",
                "The file at 'lib\\winRT45\\one.dll' uses the obsolete 'WinRT' as the framework folder.",
                "Replace 'WinRT' or 'WinRT45' with 'NetCore45'.");
        }

        [Fact]
        public void WinRTGenerateAnIssueForWinRTUnderTools()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("A", "1.0", tools: new[] { "winRT45\\install.ps1" });

            var rule = new WinRTNameIsObsoleteRule();

            // Act
            var results = rule.Validate(package).ToList();

            // Assert
            Assert.Equal(1, results.Count);
            PackageIssueTestHelper.AssertPackageIssue(
                results[0],
                "The framework name 'WinRT' is obsolete.",
                "The file at 'tools\\winRT45\\install.ps1' uses the obsolete 'WinRT' as the framework folder.",
                "Replace 'WinRT' or 'WinRT45' with 'NetCore45'.");
        }

        [Fact]
        public void ValidateReturnsNoIssueIfThereIsNoWinRT()
        {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A", 
                "1.0", 
                content: new [] { "silverlight\\help.txt" },
                assemblyReferences: new [] { "lib\\me.winmd" },
                tools: new[] { "windows8\\install.ps1" });

            var rule = new WinRTNameIsObsoleteRule();

            // Act
            var results = rule.Validate(package).ToList();

            // Assert
            Assert.Equal(0, results.Count);
        }
    }
}
