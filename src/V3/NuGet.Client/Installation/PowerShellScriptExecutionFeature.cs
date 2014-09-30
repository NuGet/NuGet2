using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.Installation
{
    public abstract class PowerShellScriptExecutionFeature
    {
        public abstract void ExecuteScript(string packageInstallPath, string scriptRelativePath, IPackage package, TargetProject project);
    }
}
