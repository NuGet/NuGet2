using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Analysis.Rules;

namespace NuGet.Test.Analysis {
    [TestClass]
    public class MisplacedAssemblyRuleTest {

        [TestMethod]
        public void NoAssemblyHasNoIssue() {
            // Arrange
            var package = PackageUtility.CreatePackage("A", content: new[] { "web.config", "jQuery.js" });
            var rule = new MisplacedAssemblyRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.IsFalse(issues.Any());
        }

        [TestMethod]
        public void AssemblyPlacedInsideFrameworkFolderHasNoIssue() {
            // Arrange
            var package = PackageUtility.CreatePackage("A", assemblyReferences: new[] { "lib\\net\\abc.dll" });
            var rule = new MisplacedAssemblyRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.IsFalse(issues.Any());
        }

        [TestMethod]
        public void AssemblyPlacedUnderLibHasOneIssue() {
            // Arrange
            var package = PackageUtility.CreatePackage("A", assemblyReferences: new[] { "lib\\abc.exe" });
            var rule = new MisplacedAssemblyRule();

            // Act
            IList<PackageIssue> issues = rule.Validate(package).ToList();

            // Assert
            Assert.AreEqual(1, issues.Count);
            PackageIssueTestHelper.AssertPackageIssue(
                issues[0],
                "Assembly not inside a framework folder.",
                "The assembly 'lib\\abc.exe' is placed directly under 'lib' folder. It is recommended that assemblies be placed inside a framework-specific folder.",
                "Move it into a framework-specific folder."
                );
        }

        [TestMethod]
        public void TwoAssembliesPlacedUnderLibHasTwoIssues() {
            // Arrange
            var package = PackageUtility.CreatePackage("A", assemblyReferences: new[] { "lib\\abc.exe", "lib\\def.dll" });
            var rule = new MisplacedAssemblyRule();

            // Act
            IList<PackageIssue> issues = rule.Validate(package).ToList();

            // Assert
            Assert.AreEqual(2, issues.Count);
            PackageIssueTestHelper.AssertPackageIssue(
                issues[0],
                "Assembly not inside a framework folder.",
                "The assembly 'lib\\abc.exe' is placed directly under 'lib' folder. It is recommended that assemblies be placed inside a framework-specific folder.",
                "Move it into a framework-specific folder."
                );

            PackageIssueTestHelper.AssertPackageIssue(
                issues[1],
                "Assembly not inside a framework folder.",
                "The assembly 'lib\\def.dll' is placed directly under 'lib' folder. It is recommended that assemblies be placed inside a framework-specific folder.",
                "Move it into a framework-specific folder."
                );
        }

        [TestMethod]
        public void TwoAssembliesPlacedOutsideLibHasOneIssues() {
            // Arrange
            var package = PackageUtility.CreatePackage("A", assemblyReferences: new[] { "content\\abc.exe", "tools\\def.dll" });
            var rule = new MisplacedAssemblyRule();

            // Act
            IList<PackageIssue> issues = rule.Validate(package).ToList();

            // Assert
            Assert.AreEqual(1, issues.Count);

            PackageIssueTestHelper.AssertPackageIssue(
                issues[0],
                "Assembly outside lib folder.",
                "The assembly 'tools\\def.dll' is not inside the 'lib' folder and hence it won't be added as reference when the package is installed into a project.",
                "Move it into 'lib' folder."
                );
        }
    }
}
