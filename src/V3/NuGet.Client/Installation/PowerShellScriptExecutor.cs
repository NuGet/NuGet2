using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.Installation
{
    public abstract class PowerShellScriptExecutor
    {
        // The type of parameter package should be IPackage but we use object instead since we
        // don't want to expose IPackage.
        public abstract void ExecuteScript(string packageInstallPath, string scriptRelativePath, object package, InstallationTarget target, IExecutionLogger logger);
    }
}
