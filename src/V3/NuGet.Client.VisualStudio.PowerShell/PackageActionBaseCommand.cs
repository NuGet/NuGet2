using Microsoft.VisualStudio.Shell;
using NuGet.Client;
using NuGet.Client.Installation;
using NuGet.Client.ProjectSystem;
using NuGet.Client.Resolution;
using NuGet.Client.VisualStudio;
using NuGet.Client.VisualStudio.PowerShell;
using NuGet.Versioning;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;



#if VS14
using Microsoft.VisualStudio.ProjectSystem.Interop;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
#endif

namespace NuGet.PowerShell.Commands
{
    /// <summary>
    /// This command process the specified package against the specified project.
    /// </summary>
    public abstract class PackageActionBaseCommand : NuGetPowerShellBaseCommand
    {
        private PackageActionType _actionType;
        private PackageIdentity _identity;
        private string _version;
        private string _readmeFile;
        private readonly IVsCommonOperations _vsCommonOperations;
        private readonly IDeleteOnRestartManager _deleteOnRestartManager;
        private IDisposable _expandedNodesDisposable;
        private SourceRepository _v3SourceRepository;

        public PackageActionBaseCommand(
            IVsPackageSourceProvider packageSourceProvider,
            IPackageRepositoryFactory packageRepositoryFactory,
            SVsServiceProvider svcServiceProvider,
            IVsPackageManagerFactory packageManagerFactory,
            IHttpClientEvents clientEvents,
            PackageActionType actionType)
            : base(packageSourceProvider, packageRepositoryFactory, svcServiceProvider, packageManagerFactory, clientEvents)
        {
            _actionType = actionType;
        }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        public virtual string Id { get; set; }

        [Parameter(Position = 2)]
        public string Version
        {
            get
            {
                if (string.IsNullOrEmpty(_version) && _actionType == PackageActionType.Install)
                {
                    IsVersionSpecified = false;
                    _version = VersionUtil.GetLastestVersionForPackage(RepositoryManager.ActiveRepository, this.Id);
                }
                return _version;
            }
            set
            {
                IsVersionSpecified = true;
                _version = value;
            }
        }

        [Parameter(Position = 3)]
        [ValidateNotNullOrEmpty]
        public string Source { get; set; }

        [Parameter]
        public SwitchParameter WhatIf { get; set; }

        public PackageIdentity Identity
        {
            get
            {
                if (_identity == null)
                {
                    _identity = new PackageIdentity(Id, NuGetVersion.Parse(Version));
                }
                return _identity;
            }
            set
            {
                _identity = value;
            }
        }

        public ActionResolver PackageActionResolver { get; set; }

        public SourceRepository V3SourceRepository
        {
            get
            {
                _v3SourceRepository = RepositoryManager.ActiveRepository;
                return _v3SourceRepository;
            }
            set
            {
                if (!string.IsNullOrEmpty(Source))
                {
                    Client.PackageSource source = new Client.PackageSource(Source, Source);
                    _v3SourceRepository = new V3SourceRepository(source, PSCommandsUserAgentClient);
                }
                else
                {
                    _v3SourceRepository = value;
                }
            }
        }

        public IPackageRepository V2LocalRepository
        {
            get
            {
                var packageManager = PackageManagerFactory.CreatePackageManager();
                return packageManager.LocalRepository;
            }
        }

        public IVsPackageManager PackageManager
        {
            get
            {
                return PackageManagerFactory.CreatePackageManager();
            }
        }

        public IEnumerable<VsProject> TargetedProjects
        {
            get
            {
                EnvDTE.Project project = Solution.DteSolution.GetAllProjects()
                    .FirstOrDefault(p => String.Equals(p.Name, ProjectName, StringComparison.OrdinalIgnoreCase));
                VsProject vsProject = Solution.GetProject(project);
                List<VsProject> targetedProjects = new List<VsProject> { vsProject };
                return targetedProjects;
            }
        }

        public bool IsVersionSpecified { get; set; }

        protected void CheckForSolutionOpen()
        {
            if (!SolutionManager.IsSolutionOpen)
            {
                ErrorHandler.ThrowSolutionNotOpenTerminatingError();
            }
        }

        protected virtual void ResolvePackageFromRepository()
        {
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to display friendly message to the console.")]
        protected override void ProcessRecordCore()
        {
            try
            {
                CheckForSolutionOpen();
                ResolvePackageFromRepository();
                ExecutePackageAction();
            }
            catch (Exception ex)
            {
                // unhandled exceptions should be terminating
                ErrorHandler.HandleException(ex, terminating: true);
            }
            finally
            {
                UnsubscribeEvents();
            }
        }

        private void ExecutePackageAction()
        {
            try
            {
                SubscribeToProgressEvents();

                // Resolve Actions
                List<VsProject> targetProjects = TargetedProjects.ToList();
                Task<IEnumerable<Client.Resolution.PackageAction>> resolverAction = 
                    PackageActionResolver.ResolveActionsAsync(Identity, _actionType, targetProjects, Solution);
                IEnumerable<Client.Resolution.PackageAction> actions = resolverAction.Result;

                if (WhatIf.IsPresent)
                {
                    foreach (var action in actions)
                    {
                        // TODO: Call new CalculatePreviewForProject Api after Fei moves it out to NuGet.Client
                        Log(Client.MessageLevel.Info, Resources.Log_OperationWhatIf, action);
                    }

                    return;
                }
                else
                {
                    // Execute Actions
                    ActionExecutor executor = new ActionExecutor();
                    Task task = executor.ExecuteActionsAsync(actions, this, CancellationToken.None);
                    task.Wait();
                }
            }
            catch (Exception ex)
            {
                this.Log(Client.MessageLevel.Warning, ex.InnerException.Message);
            }
            finally
            {
                UnsubscribeFromProgressEvents();
            }
        }
    }
}