using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "update", "UpdateCommandDescription")]
    public class UpdateCommand : Command {
        private const string DefaultFeedUrl = ListCommand.DefaultFeedUrl;
        private const string NuGetCommandLinePackageId = "NuGet.CommandLine";
        private const string NuGetExe = "NuGet.exe";

        public IPackageRepositoryFactory RepositoryFactory { get; private set; }

        [ImportingConstructor]
        public UpdateCommand(IPackageRepositoryFactory packageRepositoryFactory) {

            if (packageRepositoryFactory == null) {
                throw new ArgumentNullException("packageRepositoryFactory");
            }

            RepositoryFactory = packageRepositoryFactory;
        }

        public override void ExecuteCommand() {
            Assembly assembly = typeof(InstallCommand).Assembly;
            SelfUpdate(assembly.Location, assembly.GetName().Version);
        }

        internal void SelfUpdate(string exePath, Version version) {
            Console.WriteLine(NuGetResources.UpdateCommandCheckingForUpdates, DefaultFeedUrl);

            // Get the nuget command line package from the specified repository
            IPackageRepository packageRepository = RepositoryFactory.CreateRepository(new PackageSource(DefaultFeedUrl));

            IPackage package = packageRepository.FindPackage(NuGetCommandLinePackageId);

            // We didn't find it so complain
            if (package == null) {
                throw new CommandLineException(NuGetResources.UpdateCommandUnableToFindPackage, NuGetCommandLinePackageId);
            }

            Console.WriteLine(NuGetResources.UpdateCommandCurrentlyRunningNuGetExe, version);

            // Check to see if an update is needed
            if (version >= package.Version) {
                Console.WriteLine(NuGetResources.UpdateCommandNuGetUpToDate);
            }
            else {
                Console.WriteLine(NuGetResources.UpdateCommandUpdatingNuGet, package.Version);

                // Get NuGet.exe file from the package
                IPackageFile file = package.GetFiles().FirstOrDefault(f => Path.GetFileName(f.Path).Equals(NuGetExe, StringComparison.OrdinalIgnoreCase));

                // If for some reason this package doesn't have NuGet.exe then we don't want to use it
                if (file == null) {
                    throw new CommandLineException(NuGetResources.UpdateCommandUnableToLocateNuGetExe);
                }

                // Get the exe path and move it to a temp file (NuGet.exe.old) so we can replace the running exe with the bits we got 
                // from the package repository
                string renamedPath = exePath + ".old";
                Move(exePath, renamedPath);

                // Update the file
                UpdateFile(exePath, file);

                Console.WriteLine(NuGetResources.UpdateCommandUpdateSuccessful);
            }
        }

        protected virtual void UpdateFile(string exePath, IPackageFile file) {
            using (Stream fromStream = file.GetStream(), toStream = File.Create(exePath)) {
                fromStream.CopyTo(toStream);
            }
        }

        protected virtual void Move(string oldPath, string newPath) {
            try {
                if (File.Exists(newPath)) {
                    File.Delete(newPath);
                }
            }
            catch (FileNotFoundException) {

            }

            File.Move(oldPath, newPath);
        }
    }
}