using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test.Analysis {
    internal static class PackageIssueTestHelper {
        public static void AssertPackageIssue(
            PackageIssue issue,
            string expectedTitle,
            string expectedDescription,
            string expectedSolution) {

            Assert.AreEqual(expectedTitle, issue.Title);
            Assert.AreEqual(expectedDescription, issue.Description);
            Assert.AreEqual(expectedSolution, issue.Solution);
        }
    }
}
