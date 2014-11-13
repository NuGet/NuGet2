using Microsoft.VisualStudio.Shell;
using NuGet.Client.Installation;
using NuGet.Client.Resolution;
using NuGet.Resources;
using NuGet.Versioning;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    /// 1. exaction of Project/Solution such as PackageSolutionPowerShellModel
    /// 2. exaction of LatestVersionViewModel for getting the latest version of a package Id.
    public abstract class PackageActionBaseCommand : NuGetPowerShellBaseCommand
    {
        private PackageActionType _actionType;
        private PackageIdentity _identity;
        private SourceRepository _activeSourceRepository;

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
        }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        public virtual string Id { get; set; }

        [Parameter(Position = 2)]
        public virtual string Version { get; set; }

        [Parameter(Position = 3)]
        [ValidateNotNullOrEmpty]
        public string Source { get; set; }

        [Parameter, Alias("Prerelease")]
        public SwitchParameter IncludePrerelease { get; set; }

        [Parameter]
        public SwitchParameter WhatIf { get; set; }

        public ActionResolver PackageActionResolver { get; set; }

        public SourceRepository ActiveSourceRepository
        {
            get
            {
                if (!string.IsNullOrEmpty(Source))
                {
                    _activeSourceRepository = CreateRepositoryFromSource(Source);
                }
                else
                {
                    _activeSourceRepository = RepositoryManager.ActiveRepository;
                }
                return _activeSourceRepository;
            }
            set
            {
                _activeSourceRepository = value;
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

        public bool IterateProjects { get; set; }

        public bool IsConsolidating { get; set; }

        public bool IsVersionSpecified { get; set; }

        public IEnumerable<VsProject> TargetedProjects
        {
            get
            {
                IEnumerable<VsProject> targetedProjects;
                if (IterateProjects)
                {
                    IEnumerable<string> projectNames = GetAllValidProjectNames();
                    targetedProjects = GetProjectsByName(projectNames).ToList();
                }
                else
                {
                    VsProject vsProject = GetProject(true);
                    targetedProjects = new List<VsProject> { vsProject };
                }
                return targetedProjects;
            }
        }
       
        public PackageIdentity Identity
        {
            get
            {
                if (_identity == null && Version != null)
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
                CalculatePackageVersion();
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

        protected virtual void CalculatePackageVersion()
        {
            if (!string.IsNullOrEmpty(Version))
            {
                IsVersionSpecified = true;
            }
            // For Uninstall-Package and Consolidate-Package
            else if (_actionType == PackageActionType.Uninstall || IsConsolidating)
            {
                VsProject target = this.GetProject(true);
                InstalledPackageReference installedPackage = target.InstalledPackages.GetInstalledPackage(Id);
                Version = installedPackage.Identity.Version.ToNormalizedString();
            }
            else
            {
                // For Install-Package and Update-Package
                Version = PowerShellPackageViewModel.GetLastestVersionForPackage(ActiveSourceRepository, Id, IncludePrerelease.IsPresent);
            }
        }

        protected virtual void ExecutePackageAction()
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
                    foreach (VsProject proj in targetProjects)
                    {
                        IEnumerable<PreviewResult> previewResults = PreviewResult.CreatePreview(actions, proj);
                        if (previewResults.Count() == 0)
                        {
                            PowerShellPreviewResult prResult = new PowerShellPreviewResult();
                            prResult.Id = Identity.Id;
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
                else
                {
                    // Execute Actions
                    if (actions.Count() == 0 && _actionType == PackageActionType.Install)
                    {
                        Log(MessageLevel.Info, NuGetResources.Log_PackageAlreadyInstalled, Identity.Id);
                    }
                    else
                    {
                        ActionExecutor executor = new ActionExecutor();
                        executor.ExecuteActions(actions, this);
                    }                   
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    this.Log(Client.MessageLevel.Warning, ex.InnerException.Message);
                }
                else
                {
                    this.Log(Client.MessageLevel.Warning, ex.Message);
                }
            }
            finally
            {
                UnsubscribeFromProgressEvents();
            }
        }

        public void LogPreviewResult(PreviewResult result, VsProject proj)
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
    }
}