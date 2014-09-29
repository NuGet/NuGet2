using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client
{
    public abstract class ProjectInstallationTarget : CoreInteropInstallationTargetBase
    {
        /// <summary>
        /// Gets the name of the target in which packages will be installed (for example, the Project name when targetting a Project)
        /// </summary>
        public override string Name { get { return ProjectManager.Project.ProjectName; } }

        public CoreInteropTargetProjectBase TargetProject { get; private set; }

        public IProjectManager ProjectManager { get; private set; }

        public override IEnumerable<TargetProject> TargetProjects
        {
            get
            {
                yield return TargetProject;
            }
        }

        protected ProjectInstallationTarget(CoreInteropTargetProjectBase targetProject)
        {
            TargetProject = targetProject;
        }

        protected internal override IProjectManager GetProjectManager(TargetProject project)
        {
            if (!TargetProject.Equals(project))
            {
                throw new KeyNotFoundException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.ProjectInstallationTarget_ProjectIsNotTargetted,
                    project.Name));
            }
            return ProjectManager;
        }

        protected internal override IPackageManager GetPackageManager()
        {
            return ProjectManager.PackageManager;
        }
    }
}
