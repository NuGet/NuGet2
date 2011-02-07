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
                    ViewModel.PackageSource = selectedPackageName;
                }
            }
        }

        private void SavePackage(string targetFileName) {
            var builder = new PackageBuilder();
            // set metadata
            CopyMetadata(ViewModel.PackageMetadata, builder);
            // add files
            builder.Files.AddRange(ViewModel.GetFiles());

            // create package in the temprary file first in case the operation fails which would
            // override existing file with a 0-byte file.
            string tempFileName = Path.GetTempFileName();
            try {
                using (Stream stream = File.Create(tempFileName)) {
                    builder.Save(stream);
                }

                File.Copy(tempFileName, targetFileName, true);

                ViewModel.OnSaved();
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, Resources.Dialog_Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally {
                try {
                    if (File.Exists(tempFileName)) {
                        File.Delete(tempFileName);
                    }
                }
                catch {
                    // don't care if this fails
                }
            }
        }

        private void CopyMetadata(IPackageMetadata source, PackageBuilder builder) {
            builder.Id = source.Id;
            builder.Version = source.Version;
            builder.Title = source.Title;
            builder.Authors.AddRange(source.Authors);
            builder.Owners.AddRange(source.Owners);
            builder.IconUrl = source.IconUrl;
            builder.LicenseUrl = source.LicenseUrl;
            builder.ProjectUrl = source.ProjectUrl;
            builder.RequireLicenseAcceptance = source.RequireLicenseAcceptance;
            builder.Description = source.Description;
            builder.Summary = source.Summary;
            builder.Language = source.Language;
            builder.Tags.AddRange(ParseTags(source.Tags));
            builder.Dependencies.AddRange(source.Dependencies);
        }

        /// <summary>
        /// Tags come in this format. tag1 tag2 tag3 etc..
        /// </summary>
        private static IEnumerable<string> ParseTags(string tags) {
            if (tags == null) {
                return Enumerable.Empty<string>();
            }
            return tags.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}