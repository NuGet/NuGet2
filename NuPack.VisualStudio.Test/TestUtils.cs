using System.Linq;
using EnvDTE;
using Moq;
using System;
using Castle.DynamicProxy.Generators;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace NuPack.VisualStudio.Test {
    internal static class TestUtils {
        private static readonly Func<bool> actionWrapper = () => { AttributesToAvoidReplicating.Add<TypeIdentifierAttribute>();  return true; };
        private static readonly Lazy<bool> lazyAction = new Lazy<bool>(actionWrapper);

        public static Project GetProject(string name) {
            Debug.Assert(lazyAction.Value, "Lazy action must have been initialized by now");

            Mock<Project> project = new Mock<Project>();
            project.SetupGet(p => p.Name).Returns(name);
            project.SetupGet(p => p.FullName).Returns(name);
            project.SetupGet(p => p.UniqueName).Returns(name);
            return project.Object;
        }

        public static ISolutionManager GetSolutionManager() {
            var solutionManager = new Mock<ISolutionManager>();
            var projects = new[] { 
                GetProject("ConsoleApplication1"), GetProject("WebSite1"), GetProject("TestProject1") };
            solutionManager.SetupGet(c => c.DefaultProjectName).Returns("ConsoleApplication1");
            solutionManager.Setup(c => c.GetProjects()).Returns(projects);
            solutionManager.Setup(c => c.GetProject(It.IsAny<string>()))
                .Returns((string name) => projects.FirstOrDefault(p => p.Name == name));

            return solutionManager.Object;
        }
    }
}
