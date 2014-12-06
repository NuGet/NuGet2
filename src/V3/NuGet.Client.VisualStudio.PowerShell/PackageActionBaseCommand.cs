using Microsoft.VisualStudio.Shell;
using NuGet.Client.Installation;
using NuGet.Client.Resolution;
using NuGet.Resources;
using NuGet.Versioning;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
        private IEnumerable<PackageIdentity> _identities;
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
        public string Version { get; set; }

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

        public bool IncludeAllProjects { get; set; }

        public bool ReadFromPackagesConfig { get; set; }

        public bool ReadFromDirectPackagePath { get; set; }

        public bool IsConsolidating { get; set; }

        public bool IsVersionSpecified { get; set; }

        public IEnumerable<VsProject> Projects
        {
            get
            {
                IEnumerable<VsProject> projects;
                if (IncludeAllProjects)
                {
                    IEnumerable<string> projectNames = GetAllValidProjectNames();
                    projects = GetProjectsByName(projectNames).ToList();
                }
                else
                {
                    VsProject vsProject = GetProject(true);
                    projects = new List<VsProject> { vsProject };
                }
                return projects;
            }
        }
       
        public IEnumerable<PackageIdentity> Identities
        {
            get
            {
                _identities = GetIdentitiesForResolver();
                return _identities;
            }
            set
            {
                _identities = value;
            }
        }

        /// <summary>
        /// Get Identities for Resolver. Can be a single Identity for Install/Uninstall-Package.
        /// or multiple identities for Install/Update-Package.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<PackageIdentity> GetIdentitiesForResolver()
        {
            IEnumerable<PackageIdentity> identityList = null;
            if (!ReadFromPackagesConfig)
            {
                identityList = GetPackageIdentityForResolver();
            }
            else
            {
                identityList = CreatePackageIdentitiesFromPackagesConfig();
            }
            return identityList;
        }

        /// <summary>
        /// Return list of package identities parsed from packages.config
        /// </summary>
        /// <returns></returns>
        private IEnumerable<PackageIdentity> CreatePackageIdentitiesFromPackagesConfig()
        {
            List<PackageIdentity> identities = new List<PackageIdentity>();
            IEnumerable<PackageIdentity> parsedIdentities;
            using (FileStream stream = new FileStream(Id, FileMode.Open))
            {
                PackagesConfigReader reader = new PackagesConfigReader(stream);
                parsedIdentities = reader.GetPackages();
                stream.Close();
            }

            foreach (PackageIdentity identity in parsedIdentities.ToList())
            {
                PackageIdentity resolvedIdentity = Client.PackageRepositoryHelper.ResolvePackage(ActiveSourceRepository, V2LocalRepository, identity, IncludePrerelease.IsPresent);
                identities.Add(resolvedIdentity);
            }
            return identities;
        }

        /// <summary>
        /// Returns single package identity for resolver when Id is specified
        /// </summary>
        /// <returns></returns>
        private List<PackageIdentity> GetPackageIdentityForResolver()
        {
            PackageIdentity identity = null;

            // If Version is specified by commandline parameter
            if (!string.IsNullOrEmpty(Version))
            {
                IsVersionSpecified = true;
                identity = new PackageIdentity(Id, NuGetVersion.Parse(Version));
                if (_actionType == PackageActionType.Uninstall || IsConsolidating)
                {
                    identity = Client.PackageRepositoryHelper.ResolvePackage(V2LocalRepository, identity, IncludePrerelease.IsPresent);
                }
                else
                {
                    if (!ReadFromDirectPackagePath)
                    {
                        identity = Client.PackageRepositoryHelper.ResolvePackage(ActiveSourceRepository, V2LocalRepository, identity, IncludePrerelease.IsPresent);
                    }
                }
            }
            else
            {
                // For Uninstall-Package and Consolidate-Package
                if (_actionType == PackageActionType.Uninstall || IsConsolidating)
                {
                    VsProject target = this.GetProject(true);
                    InstalledPackageReference installedPackage = GetInstalledReference(target, Id);
                    identity = installedPackage.Identity;
                    Version = identity.Version.ToNormalizedString();
                }
                else
                {
                    // For Install-Package and Update-Package
                    Version = PowerShellPackageViewModel.GetLastestVersionForPackage(ActiveSourceRepository, Id, IncludePrerelease.IsPresent);
                    identity = new PackageIdentity(Id, NuGetVersion.Parse(Version));
                }
            }

            return new List<PackageIdentity>() { identity };
        }

        /// <summary>
        /// Get Installed Package References a single project with specified packageId
        /// </summary>
        /// <returns></returns>
        private InstalledPackageReference GetInstalledReference(VsProject proj, string Id)
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
        /// Returns the list of package identities installed to a project
        /// </summary>
        /// <param name="proj"></param>
        /// <returns></returns>
        private List<PackageIdentity> GetInstalledPackageIdentitiesForProject(VsProject proj)
        {
            List<PackageIdentity> identities = new List<PackageIdentity>();
            IEnumerable<InstalledPackageReference> refs = GetInstalledReferences(proj);
            foreach (InstalledPackageReference packageRef in refs)
            {
                identities.Add(packageRef.Identity);
            }
            return identities;
        }

        /// <summary>
        /// Get Installed Package References for all targeted projects.
        /// </summary>
        /// <returns></returns>
        protected Dictionary<VsProject, List<PackageIdentity>> GetInstalledPackagesForAllProjects()
        {
            Dictionary<VsProject, List<PackageIdentity>> dic = new Dictionary<VsProject, List<PackageIdentity>>();
            foreach (VsProject proj in Projects)
            {
                List<PackageIdentity> list = GetInstalledPackageIdentitiesForProject(proj);
                dic.Add(proj, list);
            }
            return dic;
        }

        /// <summary>
        /// Get Installed Package References for a single project
        /// </summary>
        /// <returns></returns>
        private IEnumerable<InstalledPackageReference> GetInstalledReferences(VsProject proj)
        {
            IEnumerable<InstalledPackageReference> refs = null;
            InstalledPackagesList installedList = proj.InstalledPackages;
            if (installedList != null)
            {
                refs = installedList.GetInstalledPackages();
            }
            return refs;
        }

        private void CheckForSolutionOpen()
        {
            if (!SolutionManager.IsSolutionOpen)
            {
                ErrorHandler.ThrowSolutionNotOpenTerminatingError();
            }
        }

        private void ParseUserInputForId()
        {
            if (!String.IsNullOrEmpty(Id))
            {
                if (Id.ToLowerInvariant().EndsWith(NuGet.Constants.PackageReferenceFile))
                {
                    ReadFromPackagesConfig = true;
                }
                else if (Id.ToLowerInvariant().EndsWith(NuGet.Constants.PackageExtension))
                {
                    ReadFromDirectPackagePath = true;

                    if (UriHelper.IsHttpSource(Id))
                    {
                        //InMemoryPackageRepository inMemoRepo = new InMemoryPackageRepository();
                        //PackageDownloader downloader = new PackageDownloader();
                        //ZipPackage package;
                        //using (var targetStream = new MemoryStream())
                        //{
                        //    downloader.DownloadPackage(
                        //        new HttpClient(new Uri(Id)),
                        //        packageName,
                        //        targetStream);

                        //    targetStream.Seek(0, SeekOrigin.Begin);
                        //    package = new ZipPackage(targetStream);
                        //}
                        //inMemoRepo.AddPackage(package);
                        //ActiveSourceRepository = inMemoRepo;
                    }
                    else
                    {
                        try
                        {
                            string fullPath = Path.GetFullPath(Id);
                            Source = Path.GetDirectoryName(fullPath);
                            var package = new OptimizedZipPackage(fullPath);
                            if (package != null)
                            {
                                Id = package.Id;
                                Version = package.Version.ToString();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    }
                }
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to display friendly message to the console.")]
        protected override void ProcessRecordCore()
        {
            try
            {
                CheckForSolutionOpen();
                ParseUserInputForId();
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

        protected virtual void ExecutePackageAction()
        {
            SubscribeToProgressEvents();

            foreach (PackageIdentity identity in Identities)
            {
                ExecuteSinglePackageAction(identity, Projects);
            }
        }

        /// <summary>
        /// Resolve and execute actions for a single package
        /// </summary>
        /// <param name="identity"></param>
        protected void ExecuteSinglePackageAction(PackageIdentity identity, IEnumerable<VsProject> projs)
        {
            try
            {
                // Resolve Actions
                List<VsProject> targetProjects = projs.ToList();
                Task<IEnumerable<Client.Resolution.PackageAction>> resolverAction =
                    PackageActionResolver.ResolveActionsAsync(identity, _actionType, targetProjects, Solution);

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
                else
                {
                    // Execute Actions
                    if (actions.Count() == 0 && _actionType == PackageActionType.Install)
                    {
                        Log(MessageLevel.Info, NuGetResources.Log_PackageAlreadyInstalled, identity.Id);
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
    }
}