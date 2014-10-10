using System;
using System.Collections.Generic;
using NuGet.Client.Installation;

namespace NuGet.Client.ProjectSystem
{
    public abstract class Project : InstallationTarget, IEquatable<Project>
    {
        public override bool IsSolution
        {
            get
            {
                return false;
            }
        }

        public override IEnumerable<InstallationTarget> GetAllTargetsRecursively()
        {
            yield return this;
        }

        public abstract bool Equals(Project other);
    }
}
