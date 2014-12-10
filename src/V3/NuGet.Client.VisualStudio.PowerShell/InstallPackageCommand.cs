using Microsoft.VisualStudio.Shell;
using NuGet.Client.Resolution;
using NuGet.VisualStudio;
using System.Management.Automation;



#if VS14
using Microsoft.VisualStudio.ProjectSystem.Interop;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
#endif

namespace NuGet.Client.VisualStudio.PowerShell
{
    /// <summary>
    /// This command installs the specified package into the specified project.
    /// </summary>
    /// TODO List
    /// 1. Filter unlisted packages from latest version, if version is not specified by user
    /// 2. Add back fall back to cache featuree
    /// 3. Add new path/package recognition feature
    /// 4. Add back WriteDisClaimer before installing packages. Should be one of the Resolver actions.
    /// 5. Add back popping up Readme.txt feature. Should be one of the Resolver actions. 
    [Cmdlet(VerbsLifecycle.Install, "Package2")]
    public class InstallPackageCommand : PackageInstallBaseCommand
    {
        public InstallPackageCommand() :
            base(ServiceLocator.GetInstance<IVsPackageSourceProvider>(),
                 ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                 ServiceLocator.GetInstance<SVsServiceProvider>(),
                 ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                 ServiceLocator.GetInstance<ISolutionManager>(),
                 ServiceLocator.GetInstance<IHttpClientEvents>())
        {
        }
    }
}