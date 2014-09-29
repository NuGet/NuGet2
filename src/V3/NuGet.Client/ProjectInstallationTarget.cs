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
        public override string Name { get { return TargetProject.GetProjectManager().Project.ProjectName; } }

        public CoreInteropTargetProjectBase TargetProject { get; private set; }

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
            return TargetProject.GetProjectManager();
        }

        protected internal override IPackageManager GetPackageManager()
        {
            return TargetProject.GetProjectManager().PackageManager;
        }
    }
}
