using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NuGet;
using PackageExplorerViewModel.Types;

namespace PackageExplorerViewModel {
    internal class SavePackageCommand : CommandBase, ICommand {

        private const string SaveAction = "Save";
        private const string SaveAsAction = "SaveAs";
        private const string ForceSaveAction = "ForceSave";

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
            if (action == ForceSaveAction) {
                return true;
            }
            else if (action == SaveAction) {
                return !ViewModel.IsInEditMode && CanSaveTo(ViewModel.PackageSource);
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
            if (!ViewModel.IsValid) {
                ViewModel.MessageBox.Show(Resources.PackageHasNoFile, MessageLevel.Warning);
                return;
            }

            string action = parameter as string;
            if (action == SaveAction || action == ForceSaveAction) {
                if (CanSaveTo(ViewModel.PackageSource)) {
                    Save();
                }
                else {
                    SaveAs();
                }
            }
            else if (action == SaveAsAction) {
                SaveAs();
            }
        }

        private static bool CanSaveTo(string packageSource) {
            return !String.IsNullOrEmpty(packageSource) && 
                    Path.IsPathRooted(packageSource) &&
                    Path.GetExtension(packageSource).Equals(Constants.PackageExtension, StringComparison.OrdinalIgnoreCase);
        }

        private void Save() {
            SavePackage(ViewModel.PackageSource);
            RaiseCanExecuteChangedEvent();
        }

        private void SaveAs() {
            string packageName = ViewModel.PackageMetadata.ToString() + Constants.PackageExtension;
            string title = "Save " + packageName;
            string filter = "NuGet package file (*.nupkg)|*.nupkg|All files (*.*)|*.*";
            string selectedPackagePath;
            if (ViewModel.UIServices.OpenSaveFileDialog(title, packageName, filter, out selectedPackagePath)) {
                SavePackage(selectedPackagePath);
                ViewModel.PackageSource = selectedPackagePath;
            }
            RaiseCanExecuteChangedEvent();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void SavePackage(string fileName) {
            try {
                PackageHelper.SavePackage(ViewModel.PackageMetadata, ViewModel.GetFiles(), fileName, true);
                ViewModel.OnSaved(fileName);
            }
            catch (Exception ex) {
                ViewModel.MessageBox.Show(ex.Message, MessageLevel.Error);
            }
        }

        private void RaiseCanExecuteChangedEvent() {
            if (CanExecuteChanged != null) {
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }
    }
}