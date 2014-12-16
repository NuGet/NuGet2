using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Client.Installation;
using NuGet.Client.ProjectSystem;
using NuGet.VisualStudio;
using DteSolution = EnvDTE.Solution;
using DteProject = EnvDTE.Project;
using System.Globalization;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Client.Interop;
using NuGet.Versioning;

namespace NuGet.Client.VisualStudio
{
    public class VsSolution : Solution
    {
        private readonly ISolutionManager _solution;
        private readonly string _name;
        private readonly InstalledPackagesList _installedSolutionLevelPackages;
        private readonly IVsPackageManager _packageManager;
        
        public override bool IsAvailable
        {
            get
            {
                return DteSolution.IsOpen;
            }
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        public override IEnumerable<Project> Projects
        {
            get
            {
                return _solution.GetProjects()
                    .Select(dteProject => new VsProject(this, dteProject, _packageManager.GetProjectManager(dteProject)));
            }
        }

        public DteSolution DteSolution { get; set; }

        public override InstalledPackagesList InstalledPackages
        {
            get
            {
                return _installedSolutionLevelPackages;
            }
        }

        public VsSolution(DteSolution dteSolution, ISolutionManager solutionManager, IVsPackageManager packageManager)
        {
            _name = String.Format(
                CultureInfo.CurrentCulture,
                Strings.Label_Solution,
                dteSolution.GetName());
            _solution = solutionManager;
            _packageManager = packageManager;

            var repo = (SharedPackageRepository)packageManager.LocalRepository;
            _installedSolutionLevelPackages = new CoreInteropInstalledPackagesList(
                new PackageReferenceRepository(repo.PackageReferenceFile.FullPath, repo));

            DteSolution = dteSolution;

            // Add V2-related interop features
            AddFeature(() => packageManager.LocalRepository);
            AddFeature<IPackageManager>(() => packageManager);
            AddFeature<IPackageCacheRepository>(() => MachineCache.Default);

            // the source repository of the local repo of the solution
            AddFeature<SourceRepository>(() =>
            {
                var localRepo = new NuGet.Client.Interop.V2SourceRepository(
                    null,
                    packageManager.LocalRepository,
                    "");
                return localRepo;
            });

            // Add PowerShell feature
            AddFeature<PowerShellScriptExecutor>(() =>
                new VsPowerShellScriptExecutor(ServiceLocator.GetInstance<IScriptExecutor>()));
        }

        public override bool Equals(Solution other)
        {
            VsSolution vsSln = other as VsSolution;
            return vsSln != null &&
                String.Equals(vsSln.DteSolution.FileName, DteSolution.FileName, StringComparison.OrdinalIgnoreCase);
        }

        public VsProject GetProject(DteProject dteProject)
        {
            return Projects.Cast<VsProject>().FirstOrDefault(
                p => String.Equals(p.DteProject.UniqueName, dteProject.UniqueName, StringComparison.OrdinalIgnoreCase));
        }
        
        public override Task<IEnumerable<JObject>> SearchInstalled(SourceRepository source, string searchText, int skip, int take, CancellationToken cancelToken)
        {
            return Task.Run(async () => {
                Dictionary<string, JObject> result = new Dictionary<string, JObject>(
                    StringComparer.OrdinalIgnoreCase);
                foreach (var proj in Projects)
                {
                    var packages = await proj.InstalledPackages.Search(source, searchText, 0, int.MaxValue, cancelToken);
                    foreach (var package in packages)
                    {
                        AddToResult(result, package);
                    }
                }
                var solutionLevelPackages = await InstalledPackages.Search(source, searchText, 0, int.MaxValue, cancelToken);
                foreach (var package in solutionLevelPackages)
                {
                    AddToResult(result, package);
                }

                var list = result.Values.ToList();
                return list.Skip(skip).Take(take);
            });
        }

        private static void AddToResult(Dictionary<string, JObject> result, JObject package)
        {
            var idVersion = GetIdVersion(package);

            JObject existingValue;
            if (result.TryGetValue(idVersion.Item1, out existingValue))
            {
                // if package has higher version, replace the old value with 
                // this package.
                var existingIdVersion = GetIdVersion(existingValue);
                if (idVersion.Item2 > existingIdVersion.Item2)
                {
                    result[idVersion.Item1] = package;
                }
            }
            else
            {
                result[idVersion.Item1] = package;
            }
        }

        private static Tuple<string, NuGetVersion> GetIdVersion(JObject searchResult)
        {
            string id = searchResult.Value<string>(Properties.PackageId);
            var version = NuGetVersion.Parse(searchResult.Value<string>(Properties.LatestVersion));
            return Tuple.Create(id, version);
        }
    }
}