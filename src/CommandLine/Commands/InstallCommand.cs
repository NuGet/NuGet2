using System;
using System.ComponentModel.Composition;
using System.IO;
using NuGet.Common;
using System.Linq;
using System.Xml.Linq;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "install", "InstallCommandDescription", 
        MinArgs = 1, MaxArgs = 1,
        UsageSummaryResourceName = "InstallCommandUsageSummary", UsageDescriptionResourceName = "InstallCommandUsageDescription")]
    public class InstallCommand : Command {
        private const string DefaultFeedUrl = ListCommand.DefaultFeedUrl;

        [Option(typeof(NuGetResources), "InstallCommandSourceDescription")]
        public string Source { get; set; }

        [Option(typeof(NuGetResources), "InstallCommandOutputDirDescription")]
        public string OutputDirectory { get; set; }

        [Option(typeof(NuGetResources), "InstallCommandVersionDescription")]
        public string Version { get; set; }

        [Option(typeof(NuGetResources), "InstallCommandExcludeVersionDescription", AltName = "x")]
        public bool ExcludeVersion { get; set; }

        public IPackageRepositoryFactory RepositoryFactory { get; private set; }

        [ImportingConstructor]
        public InstallCommand(IPackageRepositoryFactory packageRepositoryFactory) {
            if (packageRepositoryFactory == null) {
                throw new ArgumentNullException("packageRepositoryFactory");
            }

            RepositoryFactory = packageRepositoryFactory;
        }

        public override void ExecuteCommand() {
            var feedUrl = DefaultFeedUrl;
            if (!String.IsNullOrEmpty(Source)) {
                feedUrl = Source;
            }

            IPackageRepository packageRepository = RepositoryFactory.CreateRepository(new PackageSource(feedUrl));

            // Use the passed in install path if any, and default to the current dir
            string installPath = OutputDirectory ?? Directory.GetCurrentDirectory();

            var packageManager = new PackageManager(packageRepository,
                new DefaultPackagePathResolver(installPath, useSideBySidePaths: !ExcludeVersion), 
                new PhysicalFileSystem(installPath));

            packageManager.Logger = Console;

            // If the first argument is a packages.config file, install everything it lists
            // Otherwise, treat the first argument as a package Id
            if (Path.GetFileName(Arguments[0]).Equals(PackageReferenceRepository.PackageReferenceFile, StringComparison.OrdinalIgnoreCase)) {
                InstallPackagesFromConfigFile(packageManager, installPath);
            }
            else {
                string packageId = Arguments[0];
                Version version = Version != null ? new Version(Version) : null;
                packageManager.InstallPackage(packageId, version);
            }
        }

        private void InstallPackagesFromConfigFile(PackageManager packageManager, string path) {
            // Read all the Id/Version pairs from the packages.config file
            // REVIEW: would be nice to share some reading code with core
            var packages = from packageTag in XElement.Load(Arguments[0]).Elements("package")
                           select new { Id = packageTag.Attribute("id").Value, Version = new Version(packageTag.Attribute("version").Value) };
            bool installedAny = false;
            foreach (var package in packages) {
                if (!IsPackageInstalled(package.Id, package.Version, packageManager, path)) {
                    // Note that we ignore dependencies here because packages.config already contains the full closure
                    packageManager.InstallPackage(package.Id, package.Version, ignoreDependencies: true);
                    installedAny = true;
                }
            }

            if (!installedAny) {
                Console.WriteLine(NuGetResources.InstallCommandNothingToInstall, PackageReferenceRepository.PackageReferenceFile);
            }
        }

        // Do a very quick check of whether a package in installed by checked whether the nupkg file exists
        private bool IsPackageInstalled(string packageId, Version version, PackageManager packageManager, string path) {
            var packageDir = packageManager.PathResolver.GetPackageDirectory(packageId, version);
            var packageFile = packageManager.PathResolver.GetPackageFileName(packageId, version);

            string packagePath = Path.Combine(path, packageDir, packageFile);

            return File.Exists(packagePath);
        }
    }
}
