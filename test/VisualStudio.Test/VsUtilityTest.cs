using EnvDTE;
using Moq;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    public class VsUtilityTest
    {
        // Tests that VsUtility.GetFullPath(project) returns correct value
        // for unloaded project.
        [Fact]
        public void GetFullPathOfUnloadedProject()
        {
            // Arrange
            var solution = new Mock<Solution>();
            solution.SetupGet(s => s.FullName).Returns(@"c:\a\s.sln");

            var dte = new Mock<DTE>();
            dte.SetupGet(d => d.Solution).Returns(solution.Object);

            var project = new Mock<Project>();
            project.SetupGet(p => p.DTE).Returns(dte.Object);
            project.SetupGet(p => p.UniqueName).Returns(@"b\c.csproj");
            project.SetupGet(p => p.Kind).Returns(VsConstants.UnloadedProjectTypeGuid);

            // Act
            var projectDirectory = VsUtility.GetFullPath(project.Object);

            // Assert
            Assert.Equal(@"c:\a\b", projectDirectory);
        }
    }
}
