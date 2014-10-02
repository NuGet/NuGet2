using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Client.Installation;
using NuGet.Versioning;
using NewPackageAction = NuGet.Client.Resolution.PackageAction;

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
