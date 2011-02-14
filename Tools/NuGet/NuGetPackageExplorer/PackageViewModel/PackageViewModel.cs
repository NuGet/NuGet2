using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.Win32;
using NuGet;

namespace PackageExplorerViewModel {

    public class PackageViewModel : ViewModelBase, IPackageViewModel {

        private readonly IPackage _package;
        private EditablePackageMetadata _packageMetadata;
        private IList<PackagePart> _packageParts;
        private string _currentFileContent;
        private string _currentFileName;
        private ICommand _saveCommand, _editCommand, _cancelCommand, _applyCommand;
        private bool _isInEditMode;
        private string _packageSource;

        public PackageViewModel(IPackage package, string source) {
            if (package == null) {
                throw new ArgumentNullException("package");
            }
            _package = package;
            _packageMetadata = new EditablePackageMetadata(_package);
            PackageSource = source;
        }

        public bool IsInEditMode {
            get {
                return _isInEditMode;
            }
            private set {
                if (_isInEditMode != value) {
                    _isInEditMode = value;
                    RaisePropertyChangeEvent("IsInEditMode");
                }
            }
        }

        public string WindowTitle {
            get {
                return Resources.Dialog_Title + " - " + _packageMetadata.ToString();
            }
        }

        public EditablePackageMetadata PackageMetadata {
            get {
                return _packageMetadata;
            }
            private set {
                if (_packageMetadata != value) {
                    _packageMetadata = value;
                    RaisePropertyChangeEvent("PackageMetadata");
                }
            }
        }

        public string CurrentFileName {
            get {
                return _currentFileName;
            }
            internal set {
                if (_currentFileName != value) {
                    _currentFileName = value;
                    RaisePropertyChangeEvent("CurrentfileName");
                }
            }
        }

        public string CurrentFileContent {
            get {
                return _currentFileContent;
            }
            internal set {
                if (_currentFileContent != value) {
                    _currentFileContent = value;
                    RaisePropertyChangeEvent("CurrentFileContent");
                }
            }
        }

        public IList<PackagePart> PackageParts {
            get {
                if (_packageParts == null) {
                    PackageFolder root = PathToTreeConverter.Convert(_package.GetFiles().ToList());
                    _packageParts = root.Children;

                    AssignViewModelToFiles(root);
                }

                return _packageParts;
            }
        }

        private void AssignViewModelToFiles(PackageFolder root) {
            foreach (var part in root.Children) {
                var file = part as PackageFile;
                if (file != null) {
                    file.PackageViewModel = this;
                }
                else {
                    var folder = part as PackageFolder;
                    if (folder != null) {
                        AssignViewModelToFiles(folder);
                    }
                }
            }
        }

        #region Commands

        public ICommand SaveCommand {
            get {
                if (_saveCommand == null) {
                    _saveCommand = new SavePackageCommand(this);
                }
                return _saveCommand;
            }
        }

        public ICommand EditCommand {
            get {
                if (_editCommand == null) {
                    _editCommand = new EditPackageCommand(this);
                }
                return _editCommand;
            }
        }

        public ICommand CancelCommand {
            get {
                if (_cancelCommand == null) {
                    _cancelCommand = new CancelEditCommand(this);
                }

                return _cancelCommand;
            }
        }

        public ICommand ApplyCommand {
            get {
                if (_applyCommand == null) {
                    _applyCommand = new ApplyEditCommand(this);
                }

                return _applyCommand;
            }
        }

        #endregion

        #region IPackageViewModel interface implementations

        public string PackageSource {
            get { return _packageSource; }
            set {
                if (_packageSource != value) {
                    _packageSource = value;
                    RaisePropertyChangeEvent("PackageSource");
                }
            }
        }

        public bool HasEdit {
            get;
            private set;
        }

        IPackageMetadata IPackageViewModel.PackageMetadata {
            get { return this.PackageMetadata; }
        }

        void IPackageViewModel.ShowFile(string name, string content) {
            CurrentFileName = name;
            CurrentFileContent = content;
        }

        bool IPackageViewModel.OpenSaveFileDialog(string defaultName, out string selectedFileName) {
            var dialog = new SaveFileDialog() {
                OverwritePrompt = true,
                Title = "Save " + defaultName,
                Filter = "All files (*.*)|*.*",
                FileName = defaultName
            };

            bool? result = dialog.ShowDialog();
            if (result ?? false) {
                selectedFileName = dialog.FileName;
                return true;
            }
            else {
                selectedFileName = null;
                return false;
            }
        }

        IEnumerable<IPackageFile> IPackageViewModel.GetFiles() {
            return _package.GetFiles();
        }

        void IPackageViewModel.BegingEdit() {
            // raise the property change event here to force the edit form to rebind 
            // all controls, which will erase all error states, if any, left over from the previous edit
            RaisePropertyChangeEvent("PackageMetadata");
            IsInEditMode = true;
        }

        void IPackageViewModel.CancelEdit() {
            PackageMetadata.ResetErrors();
            IsInEditMode = false;
        }

        void IPackageViewModel.CommitEdit() {
            HasEdit = true;
            PackageMetadata.ResetErrors();
            IsInEditMode = false;
            RaisePropertyChangeEvent("WindowTitle");
        }

        void IPackageViewModel.OnSaved() {
            HasEdit = false;
        }

        #endregion
    }
}