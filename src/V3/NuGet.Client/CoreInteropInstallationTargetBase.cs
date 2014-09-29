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
        protected internal abstract IPackageManager GetPackageManager();
    }
}
