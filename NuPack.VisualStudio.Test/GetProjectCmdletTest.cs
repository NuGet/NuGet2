using System.Linq;
using System.Management.Automation;
using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.VisualStudio.Cmdlets;

namespace NuGet.VisualStudio.Test {
    [TestClass]
    public class GetProjectCmdletTest {
        [TestMethod]
        public void GetProjectCmdletReturnsDefaultProjectWhenNoFlagsAreSet() {
            // Arrange
            var cmdlet = BuildCmdlet();

            // Act
            var result = cmdlet.Invoke<Project>();
            var project = result.SingleOrDefault();

            // Assert
            Assert.IsNotNull(project);
            Assert.AreEqual(project.Name, "ConsoleApplication1");
        }

        [TestMethod]
        public void GetProjectCmdletReturnsProjectWhenProjectNameIsSpecified() {
            // Arrange
            var cmdlet = BuildCmdlet();
            cmdlet.Name = "WebSite1";

            // Act
            var result = cmdlet.Invoke<Project>();
            var project = result.SingleOrDefault();

            // Assert
            Assert.IsNotNull(project);
            Assert.AreEqual(project.Name, "WebSite1");
        }

        [TestMethod]
        public void GetProjectCmdletDoesNotThrowWhenProjectNameDoesNotExist() {
            // Arrange
            var cmdlet = BuildCmdlet();
            cmdlet.Name = "WebSite2";

            // Act
            var result = cmdlet.Invoke<Project>();
            var project = result.SingleOrDefault();

            // Assert
            Assert.IsNull(project);
        }

        [TestMethod]
        public void GetProjectCmdletReturnsAllProjectsWhenAllIsSet() {
            // Arrange
            var cmdlet = BuildCmdlet();
            cmdlet.All = new SwitchParameter(isPresent: true);

            // Act
            var result = cmdlet.Invoke<Project>();

            // Assert
            Assert.AreEqual(3, result.Count());
        }

        private static GetProjectCmdlet BuildCmdlet() {
            var projects = new[] { 
                TestUtils.GetProject("ConsoleApplication1"), TestUtils.GetProject("WebSite1"), TestUtils.GetProject("TestProject1") 
            };
            var solutionManager = TestUtils.GetSolutionManager(defaultProjectName: "ConsoleApplication1", projects: projects);
            return new GetProjectCmdlet(solutionManager);
        }
    }
}
