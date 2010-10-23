using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Castle.DynamicProxy.Generators;
using EnvDTE;
using Moq;
using NuPack.VisualStudio.Cmdlets;

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
            project.SetupGet(p => p.Kind).Returns(VSConstants.CsharpProjectKind);
            
            Mock<Properties> properties = new Mock<Properties>();
            Mock<Property> fullName = new Mock<Property>();
            fullName.Setup(c => c.Value).Returns(name);
            properties.Setup(p => p.Item(It.IsAny<string>())).Returns(fullName.Object);
            project.SetupGet(p => p.Properties).Returns(properties.Object);
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

        public static DTE GetDTE(bool isSolutionOpen = true) {
            Debug.Assert(lazyAction.Value);
            var solution = new Mock<Solution>();
            solution.SetupGet(c => c.IsOpen).Returns(isSolutionOpen);
            var dte = new Mock<DTE>();
            dte.SetupGet(c => c.Solution).Returns(solution.Object);

            return dte.Object;
        }
    }

    internal static class CmdletExtensions {
        public static IEnumerable<T> GetResults<T>(this NuPackBaseCmdlet cmdlet) {
            return GetResults(cmdlet).Cast<T>();
        }

        public static IEnumerable<object> GetResults(this NuPackBaseCmdlet cmdlet) {
            var result = new List<object>();
            cmdlet.CommandRuntime = new TestCommandRuntime(result);
            cmdlet.Execute();
            return result;
        }
    }
}
