using System.Linq;
using System.Management.Automation;
using EnvDTE;
using NuGet.VisualStudio.Test;
using Xunit;

namespace NuGet.PowerShell.Commands.Test
{

    public class GetProjectCommandTest
    {
        [Fact]
        public void GetProjectCmdletReturnsDefaultProjectWhenNoFlagsAreSet()
        {
            // Arrange
            var cmdlet = BuildCmdlet();

            // Act
            //var result = cmdlet.Invoke<Project>();
            var result = cmdlet.GetResults<Project>();
            var project = result.SingleOrDefault();

            // Assert
            Assert.NotNull(project);
            Assert.Equal(project.Name, "ConsoleApplication1");
        }

        [Fact]
        public void GetProjectCmdletReturnsAllProjectsWhenAllIsSet()
        {
            // Arrange
            var cmdlet = BuildCmdlet();
            cmdlet.All = new SwitchParameter(isPresent: true);

            // Act
            var result = cmdlet.GetResults<Project>();

            // Assert
            Assert.Equal(3, result.Count());
        }

        private static GetProjectCommand BuildCmdlet()
        {
            var projects = new[] { 
                TestUtils.GetProject("ConsoleApplication1"), TestUtils.GetProject("WebSite1"), TestUtils.GetProject("TestProject1") 
            };
            var solutionManager = TestUtils.GetSolutionManager(defaultProjectName: "ConsoleApplication1", projects: projects);
            return new GetProjectCommand(solutionManager, null);
        }
    }
}
