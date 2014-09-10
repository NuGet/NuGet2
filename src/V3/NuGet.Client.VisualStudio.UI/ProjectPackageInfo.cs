using System;
using System.Diagnostics;
using NuGet.Versioning;

namespace NuGet.Client.VisualStudio.UI
{
    // Represents the version of a package that is installed in the project
    public class ProjectPackageInfo
    {
        public EnvDTE.Project Project
        {
            get;
            private set;
        }

        public SemanticVersion Version
        {
            get;
            private set;
        }

        public bool Selected
        {
            get;
            set;
        }

        public bool Enabled
        {
            get;
            set;
        }

        private string _projectName;

        public ProjectPackageInfo(EnvDTE.Project project, SemanticVersion version, bool enabled)
        {
            Debug.Assert(project != null);

            Project = project;
            _projectName = Project.Name;
            Version = version;
            Enabled = enabled;
        }

        public override string ToString()
        {
            if (Version == null)
            {
                return _projectName;
            }
            else
            {
                return string.Format("{0} ({1})", _projectName,
                    Version.ToString());
            }
        }
    }
}
