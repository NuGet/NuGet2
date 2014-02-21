using EnvDTE;
using Moq;
using Xunit;
using Xunit.Extensions;

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

        [Theory]
        [InlineData("FullPath", @"c:\a\b", @"c:\a\b")]
        [InlineData("ProjectDirectory", @"c:\a\b", @"c:\a\b")]
        [InlineData("FullName", @"c:\a\b\c.csproj", @"c:\a\b")]
        public void GetFullPathFallback(string propName, string propValue, string expected)
        {
            // Arrange
            var solution = new Mock<Solution>();
            solution.SetupGet(s => s.FullName).Returns(@"c:\a\s.sln");

            var dte = new Mock<DTE>();
            dte.SetupGet(d => d.Solution).Returns(solution.Object);

            var property = new Mock<EnvDTE.Property>();
            property.SetupGet(p => p.Value).Returns(propValue);

            var properties = new Mock<EnvDTE.Properties>();
            properties.Setup(p => p.Item(propName)).Returns(property.Object);

            var project = new Mock<Project>();
            project.SetupGet(p => p.DTE).Returns(dte.Object);
            project.SetupGet(p => p.UniqueName).Returns(@"b\c.csproj");
            project.SetupGet(p => p.Kind).Returns(VsConstants.CsharpProjectTypeGuid);
            project.SetupGet(p => p.Properties).Returns(properties.Object);

            if (propName == "FullName")
            {
                project.SetupGet(p => p.FullName).Returns(propValue);
            }

            // Act
            var projectDirectory = VsUtility.GetFullPath(project.Object);

            // Assert
            Assert.Equal(@"c:\a\b", projectDirectory);
        }
    }
}
