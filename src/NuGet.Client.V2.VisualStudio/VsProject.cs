//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using Newtonsoft.Json.Linq;
//using NuGet.Client.Installation;
//using NuGet.VisualStudio;
//using NuGet.Client.ProjectSystem;
//using DteProject = EnvDTE.Project;
//using System;
//using System.Runtime.Versioning;

//#if VS14

//using Microsoft.VisualStudio.ProjectSystem.Interop;

//#endif

//namespace NuGet.Client.VisualStudio
//{
//    public class VsProject : Project
//    {
//        private readonly InstalledPackagesList _installed;
//        private readonly VsSolution _solution;

//        public DteProject DteProject { get; private set; }

//        public override string Name
//        {
//            get { return DteProject.GetName(); }
//        }

//        public override bool IsAvailable
//        {
//            get { return !DteProject.IsUnloaded(); }
//        }

//        public override InstalledPackagesList InstalledPackages
//        {
//            get
//            {
//                return _installed;
//            }
//        }

//#if VS14
//        private INuGetPackageManager _nugetAwareProject;
//#endif

//        public VsProject(VsSolution solution, DteProject dteProject, IProjectManager projectManager)
//            : base()
//        {
//            _solution = solution;
//            _installed = new CoreInteropInstalledPackagesList((IPackageReferenceRepository2)projectManager.LocalRepository);
//            DteProject = dteProject;

//            // Add V2-related interop features
//            AddFeature(() => projectManager);
//            AddFeature(() => projectManager.PackageManager);
//            AddFeature(() => projectManager.Project);
//            AddFeature(() => projectManager.PackageManager.LocalRepository);
//            AddFeature<IPackageCacheRepository>(() => MachineCache.Default);

//            // the source repository of the local repo of the project
//            AddFeature<SourceRepository>(() =>
//            {
//                var repo = new NuGet.Client.Interop.V2SourceRepository(
//                    null,
//                    projectManager.LocalRepository,
//                    "");
//                return repo;
//            });

//            // Add PowerShell feature
//            AddFeature<PowerShellScriptExecutor>(() =>
//                new VsPowerShellScriptExecutor(ServiceLocator.GetInstance<IScriptExecutor>()));

//#if VS14
//            // Add NuGetAwareProject if the project system is nuget-aware.
//            _nugetAwareProject = projectManager.Project as INuGetPackageManager;
//            if (_nugetAwareProject != null)
//            {
//                AddFeature<NuGetAwareProject>(() => new VsNuGetAwareProject(_nugetAwareProject));
//            }
//#endif
//        }

//        public static VsProject Create(VsSolution solution, DteProject dteProject)
//        {
//            VsNuGetTraceSources.VsProjectInstallationTarget.Verbose("create", "Created install target for project: {0}", dteProject.Name);
//            var projectManager = ServiceLocator.GetInstance<IVsPackageManagerFactory>()
//                .CreatePackageManagerToManageInstalledPackages()
//                .GetProjectManager(dteProject);
//            return new VsProject(solution, dteProject, projectManager);
//        }

//        public override bool Equals(Project other)
//        {
//            VsProject vsProj = other as VsProject;
//            return vsProj != null && String.Equals(vsProj.DteProject.FileName, DteProject.FileName, StringComparison.OrdinalIgnoreCase);
//        }

//        public override bool Equals(object obj)
//        {
//            return Equals(obj as Project);
//        }

//        public override int GetHashCode()
//        {
//            return DteProject.FileName.ToUpperInvariant().GetHashCode();
//        }

//        public override Solution OwnerSolution
//        {
//            get
//            {
//                return _solution;
//            }
//        }

//        public override IEnumerable<FrameworkName> GetSupportedFrameworks()
//        {
//#if VS14
//            if (_nugetAwareProject != null)
//            {
//                using (var cts = new CancellationTokenSource())
//                {
//                    var task = _nugetAwareProject.GetSupportedFrameworksAsync(cts.Token);
//                    return task.Result;
//                }
//            }
//#endif

//            return new FrameworkName[] { DteProject.GetTargetFrameworkName() };
//        }

//        public override Task<IEnumerable<JObject>> SearchInstalled(SourceRepository source, string searchText, int skip, int take, CancellationToken cancelToken)
//        {
//            return InstalledPackages.Search(source, searchText, skip, take, cancelToken);
//        }

//        public override void AddMetricsMetadata(JObject metricsRecord)
//        {
//            metricsRecord.Add("projectGuids", DteProject.GetAllProjectTypeGuid());
//        }
//    }
//}