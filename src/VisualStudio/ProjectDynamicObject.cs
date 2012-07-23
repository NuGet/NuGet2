using System;
using EnvDTE;

namespace NuGet.VisualStudio
{
    public class ProjectDynamicObject : MarshalByRefObject
    {
        private readonly Project _project;

        public ProjectDynamicObject(Project project)
        {
            _project = project;
        }

        public string Name
        {
            get
            {
                return _project.Name;
            }
        }

        public Properties Properties
        {
            get
            {
                return _project.Properties;
            }
        }
    }
}