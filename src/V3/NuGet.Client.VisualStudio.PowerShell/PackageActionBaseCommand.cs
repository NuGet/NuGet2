using Microsoft.VisualStudio.Shell;
using NuGet.Client.Installation;
using NuGet.Client.Resolution;
using NuGet.Resources;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;


#if VS14
using Microsoft.VisualStudio.ProjectSystem.Interop;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
#endif

namespace NuGet.Client.VisualStudio.PowerShell
{
    /// <summary>
    /// This command process the specified package against the specified project.
    /// </summary>
    /// TODO List
    /// 1. exaction of PowerShellPackage for getting the latest version of a package Id.
    public abstract class PackageActionBaseCommand : NuGetPowerShellBaseCommand
    {
        private PackageActionType _actionType;
        private readonly IVsCommonOperations _vsCommonOperations;
        private readonly IDeleteOnRestartManager _deleteOnRestartManager;
        private IDisposable _expandedNodesDisposable;

        public PackageActionBaseCommand(
            IVsPackageSourceProvider packageSourceProvider,
            IPackageRepositoryFactory packageRepositoryFactory,
            SVsServiceProvider svcServiceProvider,
            IVsPackageManagerFactory packageManagerFactory,
            ISolutionManager solutionManager,
            IHttpClientEvents clientEvents,
            PackageActionType actionType)
            : base(packageSourceProvider, packageRepositoryFactory, svcServiceProvider, packageManagerFactory, solutionManager, clientEvents)
        {
            _actionType = actionType;
            _vsCommonOperations = ServiceLocator.GetInstance<IVsCommonOperations>();
            _deleteOnRestartManager = ServiceLocator.GetInstance<IDeleteOnRestartManager>();
        }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        public virtual string Id { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true, Position = 1)]
        [ValidateNotNullOrEmpty]
        public virtual string ProjectName { get; set; }

        [Parameter(Position = 2)]
        [ValidateNotNullOrEmpty]
        public virtual string Version { get; set; }

        [Parameter(Position = 3)]
        [ValidateNotNullOrEmpty]
        public string Source { get; set; }

        [Parameter]
        public SwitchParameter WhatIf { get; set; }

        public ActionResolver PackageActionResolver { get; set; }

        public IPackageRepository V2LocalRepository
        {
            get
            {
                var packageManager = PackageManagerFactory.CreatePackageManager();
                return packageManager.LocalRepository;
            }
        }

        // Creates the source repository used to resolve dependencies
        protected SourceRepository CreateDependencyResolutionSource()
        {
            return new DependencyResolutionRepository(
            _packageSourceProvider.LoadPackageSources()
                .Where(s => s.IsEnabled)
                .Select(s => CreateRepositoryFromSource(s.Name)));
        }

        public IEnumerable<PackageIdentity> Identities { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            // remember currently expanded nodes so that we can leave them expanded 
            // after the operation has finished.
            SaveExpandedNodes();
        }

        protected override void EndProcessing()
        {
            base.EndProcessing();

            IList<string> packageDirectoriesMarkedForDeletion = _deleteOnRestartManager.GetPackageDirectoriesMarkedForDeletion();
            if (packageDirectoriesMarkedForDeletion != null && packageDirectoriesMarkedForDeletion.Count != 0)
            {
                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    VsResources.RequestRestartToCompleteUninstall,
                    string.Join(", ", packageDirectoriesMarkedForDeletion));
                WriteWarning(message);
            }

            CollapseNodes();
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to display friendly message to the console.")]
        protected override void ProcessRecordCore()
        {
            CheckForSolutionOpen();
            Preprocess();
            ExecutePackageActions();
        }

        protected virtual void Preprocess()
        {
            VsProject vsProject = GetProject(true);
            this.Projects = new List<VsProject> { vsProject };
        }

        protected virtual void ExecutePackageActions()
        {
            SubscribeToProgressEvents();

            foreach (PackageIdentity identity in Identities)
            {
                ExecuteSinglePackageAction(identity, Projects);
            }

            UnsubscribeFromProgressEvents();
        }

        /// <summary>
        /// Resolve and execute actions for a single package
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="projects"></param>
        protected void ExecuteSinglePackageAction(PackageIdentity identity, IEnumerable<VsProject> projects)
        {
            ExecuteSinglePackageAction(identity, projects, _actionType);
        }

        /// <summary>
        /// Resolve and execute actions for a single package for specified package action type.
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="projects"></param>
        /// <param name="actionType"></param>
        protected void ExecuteSinglePackageAction(PackageIdentity identity, IEnumerable<VsProject> projects, PackageActionType actionType)
        {
            if (identity == null)
            {
                return;
            }

            try
            {
                // Resolve Actions
                List<VsProject> targetProjects = projects.ToList();
                Task<IEnumerable<Client.Resolution.PackageAction>> resolverAction =
                    PackageActionResolver.ResolveActionsAsync(identity, actionType, targetProjects, Solution);

                IEnumerable<Client.Resolution.PackageAction> actions = resolverAction.Result;

                if (WhatIf.IsPresent)
                {
                    foreach (VsProject proj in targetProjects)
                    {
                        IEnumerable<PreviewResult> previewResults = PreviewResult.CreatePreview(actions, proj);
                        if (previewResults.Count() == 0)
                        {
                            PowerShellPreviewResult prResult = new PowerShellPreviewResult();
                            prResult.Id = identity.Id;
                            prResult.Action = Resources.Log_NoActionsWhatIf;
                            prResult.ProjectName = proj.Name;
                            WriteObject(prResult);
                        }
                        else
                        {
                            foreach (var p in previewResults)
                            {
                                LogPreviewResult(p, proj);
                            }
                        }
                    }

                    return;
                }

                // Execute Actions
                if (actions.Count() == 0 && actionType == PackageActionType.Install)
                {
                    Log(MessageLevel.Info, NuGetResources.Log_PackageAlreadyInstalled, identity.Id);
                }
                else
                {
                    var userAction = new UserAction(_actionType, identity);
                    ActionExecutor executor = new ActionExecutor();
                    executor.ExecuteActions(actions, this, userAction);
                }
            }
            // TODO: Consider adding the rollback behavior if exception is thrown.
            catch (Exception ex)
            {
                if (ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.Message))
                {
                    WriteError(ex.InnerException.Message);
                }
                else
                {
                    WriteError(ex.Message);
                }
            }
        }

        private void LogPreviewResult(PreviewResult result, VsProject proj)
        {
            IEnumerable<PowerShellPreviewResult> psResults = ConvertToPowerShellPreviewResult(result, proj);
            WriteObject(psResults);
        }

        private IEnumerable<PowerShellPreviewResult> ConvertToPowerShellPreviewResult(PreviewResult result, VsProject proj)
        {
            List<PowerShellPreviewResult> psResults = new List<PowerShellPreviewResult>();
            AddToPowerShellPreviewResult(psResults, result, PowerShellPackageAction.Install, proj);
            AddToPowerShellPreviewResult(psResults, result, PowerShellPackageAction.Uninstall, proj);
            AddToPowerShellPreviewResult(psResults, result, PowerShellPackageAction.Update, proj);
            return psResults;
        }

        private IEnumerable<PowerShellPreviewResult> AddToPowerShellPreviewResult(List<PowerShellPreviewResult> list, 
            PreviewResult result, PowerShellPackageAction action, VsProject proj)
        {
            IEnumerable<PackageIdentity> identities = null;
            IEnumerable<UpdatePreviewResult> updates = null;
            switch (action)
            {
                case PowerShellPackageAction.Install:
                    {
                        identities = result.Added;
                        break;
                    }
                case PowerShellPackageAction.Uninstall:
                    {
                        identities = result.Deleted;
                        break;
                    }
                case PowerShellPackageAction.Update:
                    {
                        updates = result.Updated;
                        break;
                    }
            }

            if (identities != null)
            {
                foreach (PackageIdentity identity in identities)
                {
                    PowerShellPreviewResult previewRes = new PowerShellPreviewResult();
                    previewRes.Id = identity.Id;
                    previewRes.Action = string.Format("{0} ({1})", action.ToString(), identity.Version.ToNormalizedString());
                    list.Add(previewRes);
                    previewRes.ProjectName = proj.Name;
                }
            }

            if (updates != null)
            {
                foreach (UpdatePreviewResult updateResult in updates)
                {
                    PowerShellPreviewResult previewRes = new PowerShellPreviewResult();
                    previewRes.Id = updateResult.Old.Id;
                    previewRes.Action = string.Format("{0} ({1} => {2})", 
                        action.ToString(), updateResult.Old.Version.ToNormalizedString(), updateResult.New.Version.ToNormalizedString());
                    list.Add(previewRes);
                    previewRes.ProjectName = proj.Name;
                }
            }

            return list;
        }

        private void SaveExpandedNodes()
        {
            // remember which nodes are currently open so that we can keep them open after the operation
            _expandedNodesDisposable = _vsCommonOperations.SaveSolutionExplorerNodeStates(SolutionManager);
        }

        private void CollapseNodes()
        {
            // collapse all nodes in solution explorer that we expanded during the operation
            if (_expandedNodesDisposable != null)
            {
                _expandedNodesDisposable.Dispose();
                _expandedNodesDisposable = null;
            }
        }

        /// <summary>
        /// Get the VsProject by ProjectName. 
        /// If ProjectName is not specified, return the Default project of Tool window.
        /// </summary>
        /// <param name="throwIfNotExists"></param>
        /// <returns></returns>
        public VsProject GetProject(bool throwIfNotExists)
        {
            VsProject project = GetProject(ProjectName, throwIfNotExists);
            return project;
        }

        /// <summary>
        /// Get Installed Package References for a single project
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<InstalledPackageReference> GetInstalledReferences(VsProject proj)
        {
            IEnumerable<InstalledPackageReference> refs = Enumerable.Empty<InstalledPackageReference>();
            InstalledPackagesList installedList = proj.InstalledPackages;
            if (installedList != null)
            {
                refs = installedList.GetInstalledPackages();
            }
            return refs;
        }

        /// <summary>
        /// Get Installed Package References a single project with specified packageId
        /// </summary>
        /// <returns></returns>
        protected InstalledPackageReference GetInstalledReference(VsProject proj, string Id)
        {
            InstalledPackageReference packageRef = null;
            InstalledPackagesList installedList = proj.InstalledPackages;
            if (installedList != null)
            {
                packageRef = installedList.GetInstalledPackage(Id);
            }
            return packageRef;
        }

        /// <summary>
        /// Get Installed Package References for all projects.
        /// </summary>
        /// <returns></returns>
        protected Dictionary<VsProject, List<PackageIdentity>> GetInstalledPackagesForAllProjects()
        {
            Dictionary<VsProject, List<PackageIdentity>> dic = new Dictionary<VsProject, List<PackageIdentity>>();
            foreach (VsProject proj in Projects)
            {
                List<PackageIdentity> list = GetInstalledReferences(proj).Select(r => r.Identity).ToList();
                dic.Add(proj, list);
            }
            return dic;
        }

        /// <summary>
        /// Get installed package identity for specific package Id in all projects.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        protected Dictionary<VsProject, PackageIdentity> GetInstalledPackageWithId(string packageId)
        {
            Dictionary<VsProject, PackageIdentity> dic = new Dictionary<VsProject, PackageIdentity>();
            foreach (VsProject proj in Projects)
            {
                InstalledPackageReference reference = GetInstalledReference(proj, packageId);
                if (reference != null)
                {
                    PackageIdentity identity = reference.Identity;
                    dic.Add(proj, identity);
                }
            }
            return dic;
        }
    }
}