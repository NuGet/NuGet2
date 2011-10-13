using System.Collections.Generic;
using System.Linq;
using NuGet.Analysis.Rules;
using Xunit;

namespace NuGet.Test.Analysis
{

    public class MisplacedAssemblyRuleTest
    {

        [Fact]
        public void NoAssemblyHasNoIssue()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("A", content: new[] { "web.config", "jQuery.js" });
            var rule = new MisplacedAssemblyRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.False(issues.Any());
        }

        [Fact]
        public void AssemblyPlacedInsideFrameworkFolderHasNoIssue()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("A", assemblyReferences: new[] { "lib\\net\\abc.dll" });
            var rule = new MisplacedAssemblyRule();

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
            var rule = new MisplacedAssemblyRule();

            // Act
            IList<PackageIssue> issues = rule.Validate(package).ToList();

            // Assert
            Assert.Equal(1, issues.Count);
            PackageIssueTestHelper.AssertPackageIssue(
                issues[0],
                "Assembly not inside a framework folder.",
                "The assembly 'lib\\abc.exe' is placed directly under 'lib' folder. It is recommended that assemblies be placed inside a framework-specific folder.",
                "Move it into a framework-specific folder. If this assembly is targeted for multiple frameworks, ignore this warning."
            );
        }

        [Fact]
        public void TwoAssembliesPlacedUnderLibHasTwoIssues()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("A", assemblyReferences: new[] { "lib\\abc.dll", "lib\\def.winmd" });
            var rule = new MisplacedAssemblyRule();

            // Act
            IList<PackageIssue> issues = rule.Validate(package).ToList();

            // Assert
            Assert.Equal(2, issues.Count);
            PackageIssueTestHelper.AssertPackageIssue(
                issues[0],
                "Assembly not inside a framework folder.",
                "The assembly 'lib\\abc.dll' is placed directly under 'lib' folder. It is recommended that assemblies be placed inside a framework-specific folder.",
                "Move it into a framework-specific folder. If this assembly is targeted for multiple frameworks, ignore this warning."
                );

            PackageIssueTestHelper.AssertPackageIssue(
                issues[1],
                "Assembly not inside a framework folder.",
                "The assembly 'lib\\def.winmd' is placed directly under 'lib' folder. It is recommended that assemblies be placed inside a framework-specific folder.",
                "Move it into a framework-specific folder. If this assembly is targeted for multiple frameworks, ignore this warning."
                );
        }

        [Fact]
        public void TwoAssembliesPlacedOutsideLibHasOneIssues()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("A", assemblyReferences: new[] { "content\\abc.exe", "tools\\def.winmd" });
            var rule = new MisplacedAssemblyRule();

            // Act
            IList<PackageIssue> issues = rule.Validate(package).ToList();

            // Assert
            Assert.Equal(1, issues.Count);

            PackageIssueTestHelper.AssertPackageIssue(
                issues[0],
                "Assembly outside lib folder.",
                "The assembly 'tools\\def.winmd' is not inside the 'lib' folder and hence it won't be added as reference when the package is installed into a project.",
                "Move it into the 'lib' folder if it should be referenced."
                );
        }
    }
}
