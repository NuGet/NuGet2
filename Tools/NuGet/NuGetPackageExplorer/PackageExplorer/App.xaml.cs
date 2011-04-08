using System;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Windows;
using NuGet;
using PackageExplorerViewModel;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        private CompositionContainer _container;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        internal CompositionContainer Container {
            get {
                if (_container == null) {
                    var catalog1 = new AssemblyCatalog(typeof(App).Assembly);
                    var catalog2 = new AssemblyCatalog(typeof(PackageViewModel).Assembly);
                    var catalog = new AggregateCatalog(catalog1, catalog2);
                    _container = new CompositionContainer(catalog);
                }

                return _container;
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e) {
            MainWindow window = Container.GetExportedValue<MainWindow>();
            window.Show();

            if (e.Args.Length > 0) {
                string file = e.Args[0];
                bool successful = LoadFile(window, file);
                if (successful) {
                    return;
                }
            }

            if (AppDomain.CurrentDomain.SetupInformation != null && 
                AppDomain.CurrentDomain.SetupInformation.ActivationArguments != null) {
                // click-once deployment
                var activationData = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;
                if (activationData != null && activationData.Length > 0) {
                    string file = activationData[0];
                    LoadFile(window, file);
                }
            }
        }

        private static bool LoadFile(MainWindow window, string file) {
            if (FileUtility.IsSupportedFile(file) && File.Exists(file)) {
                window.OpenLocalPackage(file);
                return true;
            }
            else {
                return false;
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e) {
            PackageExplorer.Properties.Settings.Default.Save();
            if (_container != null) {
                _container.Dispose();
            }
        }
    }
}