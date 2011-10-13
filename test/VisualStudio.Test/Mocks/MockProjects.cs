using System.Collections.Generic;
using EnvDTE;

namespace NuGet.VisualStudio.Test
{
    internal class MockProjects : Projects
    {
        private IList<Project> _projects;

        public MockProjects(IList<Project> projects)
        {
            _projects = projects;
        }

        public int Count
        {
            get { return _projects.Count; }
        }

        public DTE DTE
        {
            get { return null; }
        }

        public System.Collections.IEnumerator GetEnumerator()
        {
            return _projects.GetEnumerator();
        }

        public Project Item(object index)
        {
            int intIndex = (int)index;
            return _projects[intIndex];
        }

        public string Kind
        {
            get { return ""; }
        }

        public DTE Parent
        {
            get { return null; }
        }

        public Properties Properties
        {
            get { return null; }
        }
    }
}
