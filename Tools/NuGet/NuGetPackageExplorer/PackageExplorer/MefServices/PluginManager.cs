using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using NuGetPackageExplorer.Types;

namespace PackageExplorer {

    [Export(typeof(IPluginManager))]
    internal class PluginManager : IPluginManager {
        private const string NuGetDirectoryName = "NuGet";
        private const string PluginsDirectoryName = "PackageExplorerPlugins";

        // %localappdata%/NuGet/PackageExplorerPlugins
        private static readonly string PluginsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            NuGetDirectoryName,
            PluginsDirectoryName
        );

        private DirectoryCatalog _pluginCatalog;
        private AggregateCatalog _mainCatalog;

        [Import]
        public Lazy<IUIServices> UIServices { get; set; }

        public PluginManager(AggregateCatalog catalog) {
            if (catalog == null) {
                throw new ArgumentNullException("catalog");
            }

            _mainCatalog = catalog;
            if (Directory.Exists(PluginsDirectory)) {
                _pluginCatalog = new DirectoryCatalog(PluginsDirectory);
                _mainCatalog.Catalogs.Add(_pluginCatalog);
            }
        }

        public void AddPluginFromAssembly(string assemblyPath) {
            if (File.Exists(assemblyPath)) {
                try {
                    EnsurePluginCatalog();
                    string assemblyName = Path.GetFileName(assemblyPath);
                    string targetPath = Path.Combine(PluginsDirectory, assemblyName);
                    if (File.Exists(targetPath)) {
                        UIServices.Value.Show("Adding plugin assembly failed. There is already an existing assembly with the same name.", MessageLevel.Error);
                    }
                    else {
                        File.Copy(assemblyPath, targetPath);
                        UIServices.Value.Show("Plugin assembly added successfully.", MessageLevel.Information);
                        // trigger MEF recomposition
                        _pluginCatalog.Refresh();
                    }
                }
                catch (Exception exception) {
                    UIServices.Value.Show(exception.Message, MessageLevel.Error);
                }
            }
        }

        private void EnsurePluginCatalog() {
            if (_pluginCatalog != null) {
                return;
            }

            if (!Directory.Exists(PluginsDirectory)) {
                // creates the plugins directory if it doesn't exist
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                DirectoryInfo nugetDirectory = EnsureDirectory(new DirectoryInfo(localAppData), NuGetDirectoryName);
                EnsureDirectory(nugetDirectory, PluginsDirectoryName);
            }
            _pluginCatalog = new DirectoryCatalog(PluginsDirectory);
            _mainCatalog.Catalogs.Add(_pluginCatalog);
        }

        private DirectoryInfo EnsureDirectory(DirectoryInfo parentInfo, string path) {
            DirectoryInfo child = parentInfo.EnumerateDirectories(path, SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (child == null) {
                // if the child directory doesn't exist, create it
                child = parentInfo.CreateSubdirectory(path);
            }
            return child;
        }
    }
}