using System.Collections.Generic;
using System.Linq;
using NuGet.Analysis.Rules;
using Xunit;

namespace NuGet.Test.Analysis {

    public class NoneAssemblyInsideLibRuleTest {

        [Fact]
        public void NormalPackageHasNoIssue() {
            // Arrange
            var package = PackageUtility.CreatePackage("A", content: new[] { "one.js" });
            var rule = new NonAssemblyInsideLibRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.False(issues.Any());
        }

        [Fact]
        public void AssemblyInsideLibHasNoIssue() {
            // Arrange
            var package = PackageUtility.CreatePackage("A", assemblyReferences: new[] { "lib\\abc.dll", "def\\def.winmd" });
            var rule = new NonAssemblyInsideLibRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.False(issues.Any());
        }

        [Fact]
        public void NonAssemblyInsideLibHasIssue() {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A",
                assemblyReferences: new[] { "lib\\one.dll", "lib\\abc.xml", "lib\\sl4\\wow.pdb", "lib\\net\\4.0\\kac.txt" });
            var rule = new NonAssemblyInsideLibRule();

            // Act
            IList<PackageIssue> issues = rule.Validate(package).ToList();

            // Assert
            Assert.Equal(3, issues.Count);
            AssertPackageIssueWithPath(issues[0], "lib\\abc.xml");
            AssertPackageIssueWithPath(issues[1], "lib\\sl4\\wow.pdb");
            AssertPackageIssueWithPath(issues[2], "lib\\net\\4.0\\kac.txt");
        }

        [Fact]
        public void PdbFileAndXmlFilesAreNotWarned() {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A",
                assemblyReferences: new[] { "lib\\one.dll", "lib\\one.xml", "lib\\sl4\\wow.pdb", "lib\\sl4\\wow.exe" });
            var rule = new NonAssemblyInsideLibRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.False(issues.Any());
        }

        private void AssertPackageIssueWithPath(PackageIssue issue, string target) {
            PackageIssueTestHelper.AssertPackageIssue(
                issue,
                "Incompatible files in lib folder.",
                "The file '" + target + "' is not a valid assembly. If it is an XML documentation file or a .pdb file, there is no matching assembly specified in the same folder.",
                "Either remove this file from 'lib' folder or add a matching .dll for it.");
        }
    }
}