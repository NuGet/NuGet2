using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;

using EnvDTE;
using NuGet.VisualStudio;
using Microsoft.VisualStudio.Shell;

namespace NuGet.Client.VisualStudio.PowerShell
{
    /// <summary>
    /// This cmdlet set the Default project of PowerShell tool window
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "Project")]
    [OutputType(typeof(Project))]
    public class SetProjectCommand : NuGetPowerShellBaseCommand
    {
        public SetProjectCommand() :
            base(ServiceLocator.GetInstance<IVsPackageSourceProvider>(),
                 ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                 ServiceLocator.GetInstance<SVsServiceProvider>(),
                 ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                 ServiceLocator.GetInstance<ISolutionManager>(),
                 ServiceLocator.GetInstance<IHttpClientEvents>())
        {
        }

        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "PowerShell API requirement")]
        public string Name { get; set; }

        protected override void ProcessRecordCore()
        {
            if (!SolutionManager.IsSolutionOpen)
            {
                ErrorHandler.ThrowSolutionNotOpenTerminatingError();
            }

            if (!string.IsNullOrEmpty(Name))
            {
                bool success = SetProjectsByName(Name);
            }         
        }
    }
}
