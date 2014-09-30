using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client
{
    public abstract class ProjectInstallationTarget : InstallationTarget
    {
        /// <summary>
        /// Gets the name of the target in which packages will be installed (for example, the Project name when targetting a Project)
        /// </summary>
        public override string Name { get { return TargetProject.Name; } }

        public TargetProject TargetProject { get; private set; }

        public override IEnumerable<TargetProject> TargetProjects
        {
            get
            {
                yield return TargetProject;
            }
        }

        protected ProjectInstallationTarget(TargetProject targetProject)
        {
            TargetProject = targetProject;
        }
    }
}
