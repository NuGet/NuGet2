using Newtonsoft.Json.Linq;
using NuGet.Client;
using NuGet.Client.Installation;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading.Tasks;

// BUGBUG: Review all the namespaces in NuGet.Client.CommandLine
namespace NuGet.Common
{
    /// <summary>
    /// This installation target represents a filesystem and is really just a wrapper around PackageManager
    /// </summary>
    internal class FilesystemInstallationTarget : InstallationTarget
    {
        private static readonly IEnumerable<FrameworkName> EmtpyFrameworks = new List<FrameworkName>() { VersionUtility.EmptyFramework };
        private const string FilesystemInstallationTargetName = "FilesystemInstallationTarget";

        public FilesystemInstallationTarget(IPackageManager packageManager)
        {
            AddFeature<IPackageManager>(() => packageManager);
            AddFeature<IPackageCacheRepository>(() => MachineCache.Default);
        }
        
        public override string Name
        {
            get { return FilesystemInstallationTargetName; }
        }

        public override bool IsAvailable
        {
            // BUGBUG: Review later
            get { return true; }
        }

        public override bool IsSolution
        {
            get { return false; }
        }

        public override InstalledPackagesList InstalledPackages
        {
            get { throw new System.NotImplementedException(); }
        }

        public override NuGet.Client.ProjectSystem.Solution OwnerSolution
        {
            get { throw new System.NotImplementedException(); }
        }
        
        public override IEnumerable<FrameworkName> GetSupportedFrameworks()
        {
            return EmtpyFrameworks;
        }

        public override Task<IEnumerable<JObject>> SearchInstalled(SourceRepository source, string searchText, int skip, int take, System.Threading.CancellationToken cancelToken)
        {
            throw new System.NotImplementedException();
        }

        public override IEnumerable<InstallationTarget> GetAllTargetsRecursively()
        {
            throw new System.NotImplementedException();
        }
    }
}
