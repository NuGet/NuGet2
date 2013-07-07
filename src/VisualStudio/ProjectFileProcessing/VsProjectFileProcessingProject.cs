using System;
using EnvDTE;

namespace NuGet.VisualStudio
{
    public class VsProjectFileProcessingProject:
        IProjectFileProcessingProject
    {
        readonly Project _project;

        public VsProjectFileProcessingProject(Project project)
        {
            if (project == null) throw new ArgumentNullException("project");

            _project = project;
        }

        public IProjectFileProcessingProjectItem GetItem(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentOutOfRangeException("path");

            return new VsProjectFileProcessingProjectItem(_project.GetProjectItem(path));
        }
    }
}