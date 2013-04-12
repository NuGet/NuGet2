using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Castle.DynamicProxy.Generators;
using EnvDTE;
using Moq;

namespace NuGet.VisualStudio.Test
{
    internal static class TestUtils
    {
        private static readonly Func<bool> actionWrapper = () => { AttributesToAvoidReplicating.Add<TypeIdentifierAttribute>(); return true; };
        private static readonly Lazy<bool> lazyAction = new Lazy<bool>(actionWrapper);

        public static Project GetProject(string name,
                                         string kind = VsConstants.CsharpProjectTypeGuid,
                                         IEnumerable<string> projectFiles = null,
                                         Func<string, Property> propertyGetter = null)
        {
            EnsureTypeIdentifierAttribute();


            Mock<Project> project = new Mock<Project>();
            Mock<DTE> dte = new Mock<DTE>();
            project.SetupGet(p => p.Name).Returns(name);
            project.SetupGet(p => p.FullName).Returns(name);
            project.SetupGet(p => p.UniqueName).Returns(name);
            project.SetupGet(p => p.Kind).Returns(kind);
            project.SetupGet(p => p.DTE).Returns(dte.Object);
            dte.SetupGet(d => d.SourceControl).Returns((SourceControl)null);

            Mock<Properties> properties = new Mock<Properties>();
            if (propertyGetter != null)
            {
                properties.Setup(p => p.Item(It.IsAny<string>())).Returns<string>(propertyGetter);
            }

            Mock<Property> fullName = new Mock<Property>();
            fullName.Setup(c => c.Value).Returns(name);
            properties.Setup(p => p.Item("FullPath")).Returns(fullName.Object);
            project.SetupGet(p => p.Properties).Returns(properties.Object);
            if (projectFiles != null)
            {

                var lookup = new Dictionary<object, ProjectItem>();
                var projectItems = new Mock<ProjectItems>();

                foreach (var file in projectFiles)
                {
                    var item = new Mock<ProjectItem>();
                    item.Setup(i => i.Name).Returns(file);
                    lookup[file] = item.Object;
                    projectItems.Setup(i => i.Item(It.IsAny<object>())).Returns((object index) => lookup[index]);
                }
                projectItems.Setup(c => c.GetEnumerator()).Returns(lookup.Values.GetEnumerator());
                project.Setup(p => p.ProjectItems).Returns(projectItems.Object);
            }
            return project.Object;
        }

        public static ISolutionManager GetSolutionManagerWithProjects(params string[] projects)
        {
            return GetSolutionManager(isSolutionOpen: true, defaultProjectName: null, projects: projects.Select(p => GetProject(p)));
        }

        public static ISolutionManager GetSolutionManager(bool isSolutionOpen = true, string defaultProjectName = null, IEnumerable<Project> projects = null)
        {
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.SetupGet(c => c.DefaultProjectName).Returns(defaultProjectName);
            solutionManager.SetupGet(c => c.DefaultProject).Returns(
                (projects ?? Enumerable.Empty<Project>()).
                Where(p => p.Name == defaultProjectName).SingleOrDefault());
            solutionManager.Setup(c => c.GetProjects()).Returns(projects ?? Enumerable.Empty<Project>());
            solutionManager.Setup(c => c.GetProject(It.IsAny<string>()))
                .Returns((string name) => projects.FirstOrDefault(p => p.Name == name));
            solutionManager.SetupGet(c => c.IsSolutionOpen).Returns(isSolutionOpen);
            return solutionManager.Object;
        }

        public static DTE GetDTE()
        {
            EnsureTypeIdentifierAttribute();
            return new Mock<DTE>().Object;
        }

        public static void EnsureTypeIdentifierAttribute()
        {
            if (!lazyAction.Value)
            {
                throw new InvalidOperationException("Lazy action must have been initialized by now");
            }
        }
    }
}