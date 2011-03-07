using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NuGet;

namespace PackageExplorerViewModel {
    internal class SavePackageCommand : CommandBase, ICommand {

        private const string SaveAction = "Save";
        private const string SaveAsAction = "SaveAs";

        public SavePackageCommand(PackageViewModel model)
            : base(model) {
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
            string action = parameter as string;
            if (action == SaveAction) {
                return !ViewModel.IsInEditMode && Path.IsPathRooted(ViewModel.PackageSource);
            }
            else if (action == SaveAsAction) {
                return !ViewModel.IsInEditMode;
            }
            else {
                return false;
            }
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            if (!ViewModel.RootFolder.GetFiles().Any()) {
                MessageBox.Show(Resources.PackageHasNoFile, Resources.Dialog_Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string action = parameter as string;
            if (action == SaveAction) {
                SavePackage(ViewModel.PackageSource);
                RaiseCanExecuteChangedEvent();
            }
            else if (action == SaveAsAction) {
                string packageName = ViewModel.PackageMetadata.ToString() + Constants.PackageExtension;
                string selectedPackageName;
                if (ViewModel.OpenSaveFileDialog(packageName, true, out selectedPackageName)) {
                    SavePackage(selectedPackageName);
                    ViewModel.PackageSource = selectedPackageName;
                }
                RaiseCanExecuteChangedEvent();
            }
        }

        private void SavePackage(string fileName) {
            try {
                PackageHelper.SavePackage(ViewModel.PackageMetadata, ViewModel.GetFiles(), fileName, true);
                ViewModel.OnSaved();
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, Resources.Dialog_Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RaiseCanExecuteChangedEvent() {
            if (CanExecuteChanged != null) {
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }
    }
}