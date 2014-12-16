using Microsoft.VisualStudio.Shell;
using NuGet.VisualStudio;
using System.Management.Automation;

namespace NuGet.Client.VisualStudio.PowerShell
{
        [Cmdlet(VerbsCommon.Open, "PackagePage2", DefaultParameterSetName = ParameterAttribute.AllParameterSets, SupportsShouldProcess = true)]
        public class OpenPackagePageCommand : NuGetPowerShellBaseCommand
        {
        public OpenPackagePageCommand() :
            base(ServiceLocator.GetInstance<IVsPackageSourceProvider>(),
                 ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                 ServiceLocator.GetInstance<SVsServiceProvider>(),
                 ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                 ServiceLocator.GetInstance<ISolutionManager>(),
                 ServiceLocator.GetInstance<IHttpClientEvents>())
        {
        }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        public string Id { get; set; }

        [Parameter(Position = 1)]
        [ValidateNotNull]
        public SemanticVersion Version { get; set; }

        [Parameter(Position = 2)]
        [ValidateNotNullOrEmpty]
        public string Source { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "License")]
        public SwitchParameter License { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ReportAbuse")]
        public SwitchParameter ReportAbuse { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecordCore()
        {
            Log(Client.MessageLevel.Info, Resources.Cmdlet_CommandObsolete, "None");
        }
    }
}
