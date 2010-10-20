using System.Runtime.InteropServices;
using Castle.DynamicProxy.Generators;
using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;
using NuPack.VisualStudio.Cmdlets;
using System.Management.Automation;

namespace NuPack.VisualStudio.Test {
    [TestClass]
    public class GetProjectCmdletTest {
        [TestMethod]
        public void GetProjectCmdletReturnsDefaultProjectWhenNoFlagsAreSet() {
            // Arrange
            var cmdlet = new GetProjectCmdlet(TestUtils.GetSolutionManager());

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
            var cmdlet = new GetProjectCmdlet(TestUtils.GetSolutionManager());
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
            var cmdlet = new GetProjectCmdlet(TestUtils.GetSolutionManager());
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
            var cmdlet = new GetProjectCmdlet(TestUtils.GetSolutionManager());
            cmdlet.All = new SwitchParameter(isPresent: true);

            // Act
            var result = cmdlet.Invoke<Project>();
            
            // Assert
            Assert.AreEqual(3, result.Count());
        }
    }
}
