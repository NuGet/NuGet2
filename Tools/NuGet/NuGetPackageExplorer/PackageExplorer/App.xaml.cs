using System;
using System.IO;
using System.Windows;
using NuGet;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        private void Application_Startup(object sender, StartupEventArgs e) {
            MainWindow window = new MainWindow();
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
            if (File.Exists(file) && file.EndsWith(Constants.PackageExtension, StringComparison.OrdinalIgnoreCase)) {
                window.LoadPackage(file);
                return true;
            }
            else {
                return false;
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e) {
            PackageExplorer.Properties.Settings.Default.Save();
        }
    }
}
