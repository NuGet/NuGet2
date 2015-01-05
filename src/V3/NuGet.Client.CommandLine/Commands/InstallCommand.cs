using Newtonsoft.Json.Linq;
using NuGet.Client;
using NuGet.Client.Installation;
using NuGet.Client.ProjectSystem;
using NuGet.Client.Resolution;
using NuGet.Common;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Commands
{
    [Command(typeof(NuGetCommandResourceType), "install", "InstallCommandDescription",
        MinArgs = 0, MaxArgs = 1, UsageSummaryResourceName = "InstallCommandUsageSummary",
        UsageDescriptionResourceName = "InstallCommandUsageDescription",
        UsageExampleResourceName = "InstallCommandUsageExamples")]
    public class InstallCommand : DownloadCommandBase
    {
        [Option(typeof(NuGetCommandResourceType), "InstallCommandVersionDescription")]
        public string Version { get; set; }

        [Option(typeof(NuGetCommandResourceType), "InstallCommandPrerelease")]
        public bool Prerelease { get; set; }

        [Option(typeof(NuGetCommandResourceType), "InstallCommandOutputDirDescription")]
        public string OutputDirectory { get; set; }

        [Option(typeof(NuGetCommandResourceType), "InstallCommandDependencyBehavior")]
        public DependencyBehavior DependencyBehavior { get; set; }

        [ImportingConstructor]
        public InstallCommand()
            : base(MachineCache.Default)
        {
            // On mono, parallel builds are broken for some reason. See https://gist.github.com/4201936 for the errors
            // That are thrown.
            DisableParallelProcessing = EnvironmentUtility.IsMonoRuntime;
            DependencyBehavior = Client.DependencyBehavior.Lowest;
        }

        public override void ExecuteCommand()
        {
            CalculateEffectivePackageSaveMode();
            string installPath = ResolveInstallPath();
            IFileSystem installPathFilesystem = CreateFileSystem(installPath);
            // BUGBUG: Check that the argument is always passed
            string packageId = Arguments[0];
            NuGetVersion version = Version != null ? new NuGetVersion(Version) : null;
            InitializeSourceRepository();
            InstallPackage(installPathFilesystem, packageId, version).Wait();
        }

        internal string ResolveInstallPath()
        {
            if (!String.IsNullOrEmpty(OutputDirectory))
            {
                // Use the OutputDirectory if specified.
                return OutputDirectory;
            }

            ISettings currentSettings = Settings;
            string installPath = currentSettings.GetRepositoryPath();
            if (!String.IsNullOrEmpty(installPath))
            {
                // If a value is specified in config, use that. 
                return installPath;
            }

            // Use the current directory as output.
            return Directory.GetCurrentDirectory();
        }

        protected internal virtual IFileSystem CreateFileSystem(string path)
        {
            path = Path.GetFullPath(path);
            return new PhysicalFileSystem(path);
        }

        private async Task InstallPackage(
            IFileSystem installPathFileSystem,
            string packageId,
            NuGetVersion version)
        {
            if (version == null)
            {
                NoCache = true;
            }
            var packageManager = CreatePackageManager(installPathFileSystem, useSideBySidePaths: true);
            
            // BUGBUG: When adding support for 'AllowMultipleVersions', remember to add PackageInstallNeeded method

            JObject packageMetadata;
            if (version == null)
            {
                packageMetadata = await SourceRepositoryHelper.GetLatestVersionMetadata(SourceRepository, packageId, prerelease: Prerelease);
                version = NuGetVersion.Parse(packageMetadata["version"].ToString());
            }
            else
            {
                packageMetadata = await SourceRepository.GetPackageMetadata(packageId, version);
            }

            if (packageMetadata == null)
            {
                throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture,
                        NuGet.Resources.NuGetResources.UnknownPackageSpecificVersion, packageId, version));
            }

            var actionResolver = new ActionResolver(
                SourceRepository,
                SourceRepository,
                new ResolutionContext()
                {
                    AllowPrerelease = Prerelease,
                    DependencyBehavior = DependencyBehavior,
                });

            var packageActions = await actionResolver.ResolveActionsAsync(new PackageIdentity(packageId, version),
                PackageActionType.Install,
                new FilesystemInstallationTarget(packageManager));

            if (Verbosity == NuGet.Verbosity.Detailed)
            {
                Console.WriteLine("Actions returned by resolver");
                foreach (var action in packageActions)
                {
                    Console.WriteLine(action.ActionType.ToString() + "-" + action.PackageIdentity.ToString());
                }
            }

            packageActions = packageActions.Where(a => a.ActionType == PackageActionType.Download || a.ActionType == PackageActionType.Purge);

            if (Verbosity == NuGet.Verbosity.Detailed)
            {
                Console.WriteLine("After reducing actions to just Download and Purge");
                foreach (var action in packageActions)
                {
                    Console.WriteLine(action.ActionType.ToString() + "-" + action.PackageIdentity.ToString());
                }
            }

            var actionExecutor = new ActionExecutor();
            actionExecutor.ExecuteActions(packageActions, Console);
        }
    }
}
