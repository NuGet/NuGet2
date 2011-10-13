using Xunit;

namespace NuGet.Test.Analysis
{
    internal static class PackageIssueTestHelper
    {
        public static void AssertPackageIssue(
            PackageIssue issue,
            string expectedTitle,
            string expectedDescription,
            string expectedSolution)
        {

            Assert.Equal(expectedTitle, issue.Title);
            Assert.Equal(expectedDescription, issue.Description);
            Assert.Equal(expectedSolution, issue.Solution);
        }
    }
}
