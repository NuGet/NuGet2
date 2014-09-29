using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client
{
    public abstract class CoreInteropInstallationTargetBase : InstallationTarget
    {
        protected internal abstract IProjectManager GetProjectManager(TargetProject project);
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification="This may not be performant, clients should cache the value they receive")]
        protected internal abstract IPackageManager GetPackageManager();
    }
}
