using Microsoft.VisualStudio.Shell;
using NuGet.VisualStudio;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;

namespace NuGet.Client.VisualStudio.PowerShell
{
    /// <summary>
    /// This cmdlet set the Default project of PowerShell tool window
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "Source")]
    [OutputType(typeof(string))]
    public class SetSourceCommand : NuGetPowerShellBaseCommand
    {
        public SetSourceCommand() :
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
            if (!string.IsNullOrEmpty(Name))
            {
                bool success = SetPackageSourceByName(Name);
            }
        }
    }
}
