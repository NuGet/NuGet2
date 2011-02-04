using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using NuGet;

namespace PackageExplorerViewModel {
    internal class SavePackageCommand : CommandBase, ICommand {

        private const string SaveAction = "Save";
        private const string SaveAsAction = "SaveAs";

        public SavePackageCommand(IPackageViewModel model) : base(model) {
            model.PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName.Equals("IsInEditMode")) {
                if (CanExecuteChanged != null) {
                    CanExecuteChanged(this, EventArgs.Empty);
                }
            }
        }

        public bool CanExecute(object parameter) {
            return !ViewModel.IsInEditMode;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            string action = parameter as string;
            if (action == SaveAction) {
                SavePackage(ViewModel.PackageSource);
            }
            else if (action == SaveAsAction) {
                string packageName = Path.GetFileName(ViewModel.PackageSource);

                string selectedPackageName;
                if (ViewModel.OpenSaveFileDialog(packageName, out selectedPackageName)) {
                    SavePackage(selectedPackageName);
                }
            }
        }

        private void SavePackage(string packageSource) {
            var builder = new PackageBuilder();
            builder.Files.AddRange(ViewModel.GetFiles());
            // TODO

            using (Stream stream = File.OpenWrite(packageSource)) {
                builder.Save(stream);
            }
        }
    }
}