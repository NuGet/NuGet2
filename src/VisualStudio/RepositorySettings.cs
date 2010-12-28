using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio {
    [Export(typeof(IRepositorySettings))]
    public class RepositorySettings : IRepositorySettings {
        private const string DefaultRepositoryDirectory = "packages";
        private const string NuGetConfig = "nuget.config";

        private string _configurationPath;
        private IFileSystem _fileSystem;
        private readonly ISolutionManager _solutionManager;
        private readonly IFileSystemProvider _fileSystemProvider;

        [ImportingConstructor]        
        public RepositorySettings(ISolutionManager solutionManager, IFileSystemProvider fileSystemProvider) {
            if (solutionManager == null) {
                throw new ArgumentNullException("solutionManager");
            }

            if (fileSystemProvider == null) {
                throw new ArgumentNullException("fileSystemProvider");
            }

            _solutionManager = solutionManager;
            _fileSystemProvider = fileSystemProvider;

            _solutionManager.SolutionClosing += (sender, e) => {
                // Kill our configuration cache when someone closes the solution
                _configurationPath = null;
                _fileSystem = null;
            };
        }

        public string RepositoryPath {
            get {
                return GetRepositoryPath();
            }
        }

        private IFileSystem FileSystem {
            get {
                if (_fileSystem == null) {
                    _fileSystem = _fileSystemProvider.GetFileSystem(_solutionManager.SolutionDirectory);
                }
                return _fileSystem;
            }
        }

        private string GetRepositoryPath() {
            // If the solution directory is unavailable then throw an exception
            if (String.IsNullOrEmpty(_solutionManager.SolutionDirectory)) {
                throw new InvalidOperationException(VsResources.SolutionDirectoryNotAvailable);
            }

            // Get the configuration path (if any)
            string configurtionPath = GetConfigurationPath();

            string path = null;
            string directoryPath = _solutionManager.SolutionDirectory;

            // If we found a config file, try to read it
            if (!String.IsNullOrEmpty(configurtionPath)) {
                // Read the path from the file
                path = GetRepositoryPathFromConfig(configurtionPath);
            }

            if (String.IsNullOrEmpty(path)) {
                // If the path is null then default to the directory
                path = DefaultRepositoryDirectory;
            }
            else {
                // Resolve the path relative to the configuration path
                directoryPath = Path.GetDirectoryName(configurtionPath);
            }

            return Path.Combine(directoryPath, path);
        }

        /// <summary>
        /// Returns the configuraton path by walking the directory structure to find a nuget.config file.
        /// </summary>
        private string GetConfigurationPath() {
            if (CheckConfiguration()) {
                // Start from the solution directory and try to find a nuget.config in the list of candidates
                _configurationPath = (from directory in GetConfigurationDirectories(_solutionManager.SolutionDirectory)
                                      let configPath = Path.Combine(directory, NuGetConfig)
                                      where FileSystem.FileExists(configPath)
                                      select configPath).FirstOrDefault();
            }

            return _configurationPath;
        }

        private bool CheckConfiguration() {
            // If there's no saved configuration path then look for a configuration file.
            // This is to accomate the workflow where someone changes the solution repository
            // after installing packages using the default "packages" folder.

            // REVIEW: Do we always look even in the default scenario where the user has no nuget.config file?
            if (String.IsNullOrEmpty(_configurationPath)) {
                return true;
            }

            // If we have a configuration file path cached. We only do the directory walk if the file no longer exists
            return !FileSystem.FileExists(_configurationPath);
        }

        /// <summary>
        /// Extracts the repository path from a nuget.config settings file
        /// </summary>
        /// <param name="path">Full path to the nuget.config file</param>
        private string GetRepositoryPathFromConfig(string path) {
            try {
                XDocument document = null;
                using (Stream stream = FileSystem.OpenFile(path)) {
                    document = XDocument.Load(stream);
                }

                // <settings>
                //    <repositoryPath>..</repositoryPath>
                // </settings>
                return document.Root.GetOptionalElementValue("repositoryPath");
            }
            catch (XmlException e) {
                // Set the configuration path to null if it fails
                _configurationPath = null;

                // If we were unable to parse the configuration file then show an error
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                                  VsResources.ErrorReadingFile, path), e);
            }
        }

        /// <summary>
        /// Returns the list of candidates for nuget config files.
        /// </summary>
        private IEnumerable<string> GetConfigurationDirectories(string path) {
            while (!String.IsNullOrEmpty(path)) {
                yield return path;

                path = Path.GetDirectoryName(path);
            }
        }
    }
}
