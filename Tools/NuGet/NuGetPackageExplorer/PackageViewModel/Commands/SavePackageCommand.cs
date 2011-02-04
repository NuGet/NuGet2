using System;
using System.IO;
using System.Windows.Input;
using NuGet;

namespace PackageExplorerViewModel {
    internal class SavePackageCommand : CommandBase, ICommand {

        private const string SaveAction = "Save";
        private const string SaveAsAction = "SaveAs";

        public SavePackageCommand(IPackageViewModel model, IPackage package) : base(model, package) {
        }

        public bool CanExecute(object parameter) {
            string action = parameter as string;
            return action == SaveAction || action == SaveAsAction;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            string action = parameter as string;
            if (action == SaveAction) {
                SavePackage(Package, ViewModel.PackageSource);
            }
            else if (action == SaveAsAction) {
                string packageName = Path.GetFileName(ViewModel.PackageSource);

                string selectedPackageName;
                if (ViewModel.OpenSaveFileDialog(packageName, out selectedPackageName)) {
                    SavePackage(Package, selectedPackageName);
                }
            }
        }

        private void SavePackage(IPackage _package, string packageSource) {
            var builder = new PackageBuilder();
            builder.Files.AddRange(_package.GetFiles());

            using (Stream stream = File.OpenWrite(packageSource)) {
                builder.Save(stream);
            }
        }
    }
}