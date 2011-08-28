using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Analysis.Rules;

namespace NuGet.Test.Analysis {
    [TestClass]
    public class NoneAssemblyInsideLibRuleTest {

        [TestMethod]
        public void NormalPackageHasNoIssue() {
            // Arrange
            var package = PackageUtility.CreatePackage("A", content: new[] { "one.js" });
            var rule = new NonAssemblyInsideLibRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.IsFalse(issues.Any());
        }

        [TestMethod]
        public void AssemblyInsideLibHasNoIssue() {
            // Arrange
            var package = PackageUtility.CreatePackage("A", assemblyReferences: new[] { "lib\\abc.dll" });
            var rule = new NonAssemblyInsideLibRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.IsFalse(issues.Any());
        }

        [TestMethod]
        public void NonAssemblyInsideLibHasIssue() {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A", 
                assemblyReferences: new[] { "lib\\one.dll", "lib\\abc.xml", "lib\\sl4\\wow.pdb", "lib\\net\\4.0\\kac.txt" });
            var rule = new NonAssemblyInsideLibRule();

            // Act
            IList<PackageIssue> issues = rule.Validate(package).ToList();

            // Assert
            Assert.AreEqual(3, issues.Count);
            AssertPackageIssueWithPath(issues[0], "lib\\abc.xml");
            AssertPackageIssueWithPath(issues[1], "lib\\sl4\\wow.pdb");
            AssertPackageIssueWithPath(issues[2], "lib\\net\\4.0\\kac.txt");
        }

        [TestMethod]
        public void PdbFileAndXmlFilesAreNotWarned() {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A",
                assemblyReferences: new[] { "lib\\one.dll", "lib\\one.xml", "lib\\sl4\\wow.pdb", "lib\\sl4\\wow.exe" });
            var rule = new NonAssemblyInsideLibRule();

            // Act
            IEnumerable<PackageIssue> issues = rule.Validate(package);

            // Assert
            Assert.IsFalse(issues.Any());
        }

        private void AssertPackageIssueWithPath(PackageIssue issue, string target) {
            PackageIssueTestHelper.AssertPackageIssue(
                issue,
                "Incompatible files in lib folder",
                "The file '" + target + "' is not a valid assembly. If it is a XML documentation file or a .pdb file, there is no matching .dll file specified in the same folder.",
                "Either remove this file from 'lib' folder or add a matching .dll for it.");
        }
    }
}