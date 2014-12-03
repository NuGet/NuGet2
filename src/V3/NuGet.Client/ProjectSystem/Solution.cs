using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.Installation;

namespace NuGet.Client.ProjectSystem
{
    public abstract class Solution : InstallationTarget, IEquatable<Solution>
    {
        public override bool IsSolution
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a list of all projects targetted by this installation target.
        /// </summary>
        public abstract IEnumerable<Project> Projects { get; }

        /// <summary>
        /// Gets the project with the specified name, if it exists, otherwise returns null.
        /// <returns></returns>
        public virtual Project GetProject(string projectName)
        {
            return Projects.FirstOrDefault(p => String.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase));
        }

        public abstract bool Equals(Solution other);

        public override IEnumerable<FrameworkName> GetSupportedFrameworks()
        {
            return Enumerable.Empty<FrameworkName>();
        }

        public override Solution OwnerSolution
        {
            get
            {
                return this;
            }
        }

        public override IEnumerable<InstallationTarget> GetAllTargetsRecursively()
        {
            yield return this;

            // Recursive descent
            foreach (var project in Projects)
            {
                // Right now, GetAllTargetsRecursively returns just the Project, but in future we may have sub-targets below Project!
                foreach (var target in project.GetAllTargetsRecursively())
                {
                    yield return target;
                }
            }
        }
    }
}
