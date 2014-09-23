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
    public abstract class TargetProject
    {
        public abstract string Name { get; }
        public abstract IProjectSystem ProjectSystem { get; }
        public abstract InstalledPackagesList InstalledPackages { get; }

        /// <summary>
        /// Gets the list of frameworks supported by this target.
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<FrameworkName> GetSupportedFrameworks();

        /// <summary>
        /// Executes the specified action against this installation target.
        /// </summary>
        /// <param name="actions"></param>
        /// <returns></returns>
        public abstract Task ExecuteActionsAsync(IEnumerable<NewPackageAction> actions);
    }
}
