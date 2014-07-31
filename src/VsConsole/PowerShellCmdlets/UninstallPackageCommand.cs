using System.Management.Automation;
using NuGet.Resolver;
using NuGet.VisualStudio;

namespace NuGet.PowerShell.Commands
{

    /// <summary>
    /// This command uninstalls the specified package from the specified project.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Uninstall, "Package")]
    public class UninstallPackageCommand : ProcessPackageBaseCommand
    {
        public UninstallPackageCommand()
            : this(ServiceLocator.GetInstance<ISolutionManager>(),
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                   ServiceLocator.GetInstance<IHttpClientEvents>(),
                   ServiceLocator.GetInstance<IVsCommonOperations>(),
                   ServiceLocator.GetInstance<IDeleteOnRestartManager>())
        {
        }

        public UninstallPackageCommand(ISolutionManager solutionManager,
                                       IVsPackageManagerFactory packageManagerFactory,
                                       IHttpClientEvents httpClientEvents,
                                       IVsCommonOperations vsCommonOperations,
                                       IDeleteOnRestartManager deleteOnRestartManager)
            : base(solutionManager, packageManagerFactory, httpClientEvents, vsCommonOperations, deleteOnRestartManager)
        {
        }

        [Parameter(Position = 2)]
        [ValidateNotNull]
        public SemanticVersion Version { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        [Parameter]
        public SwitchParameter RemoveDependencies { get; set; }

        [Parameter]
        public SwitchParameter WhatIf { get; set; }

        protected override void ProcessRecordCore()
        {
            if (!SolutionManager.IsSolutionOpen)
            {
                // terminating
                ErrorHandler.ThrowSolutionNotOpenTerminatingError();
            }

            // Locate the package to uninstall
            IPackage package = PackageManager.LocatePackageToUninstall(
                ProjectManager,
                Id,
                Version);

            // resolve actions
            var resolver = new ActionResolver()
            {
                Logger = this,
                ForceRemove = Force.IsPresent,
                RemoveDependencies = RemoveDependencies.IsPresent
            };
            resolver.AddOperation(
                PackageAction.Uninstall,
                package,
                ProjectManager);

            var actions = resolver.ResolveActions();
            if (WhatIf)
            {
                foreach (var operation in actions)
                {
                    Log(MessageLevel.Info, Resources.Log_OperationWhatIf, operation);
                }

                return;
            }

            // execute actions
            var actionExecutor = new ActionExecutor()
            {
                Logger = this
            };
            actionExecutor.Execute(actions);
        }
    }
}