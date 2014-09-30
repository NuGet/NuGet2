using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using NewPackageAction = NuGet.Client.Resolution.PackageAction;

namespace NuGet.Client
{
    public abstract class TargetProject : IEquatable<TargetProject>
    {
        public abstract string Name { get; }
        public abstract IProjectSystem ProjectSystem { get; }
        public abstract InstalledPackagesList InstalledPackages { get; }

        /// <summary>
        /// Gets the list of frameworks supported by this target.
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification="This method may require computation")]
        public abstract FrameworkName GetSupportedFramework();

        public abstract bool Equals(TargetProject other);
    }
}
