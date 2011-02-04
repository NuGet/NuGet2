using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.Win32;
using NuGet;

namespace PackageExplorerViewModel {
   
    public class PackageViewModel : INotifyPropertyChanged, IPackageViewModel {

        private readonly IPackage _package;
        private EditablePackageMetadata _packageMetadata;
        private IList<PackagePart> _packageParts;
        private string _currentFileContent;
        private string _currentFileName;
        private ICommand _saveCommand, _editCommand;
        private ICommand _cancelCommand, _applyCommand;
        private bool _isInEditMode;

        public event PropertyChangedEventHandler PropertyChanged;

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

        private void RaisePropertyChangeEvent(string propertyName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
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
            get;
            private set;
        }

        public bool HasEdit {
            get;
            private set;
        }

        void IPackageViewModel.ShowFile(string name, string content) {
            CurrentFileName = name;
            CurrentFileContent = content;
        }

        bool IPackageViewModel.OpenSaveFileDialog(string defaultName, out string selectedFileName)
        {
            var dialog = new SaveFileDialog()
            {
                OverwritePrompt = true,
                Title = "Save " + defaultName,
                Filter = "All files (*.*)|*.*",
                FileName = defaultName
            };

            bool? result = dialog.ShowDialog();
            if (result ?? false)
            {
                selectedFileName = dialog.FileName;
                return true;
            }
            else
            {
                selectedFileName = null;
                return false;
            }
        }

        IEnumerable<IPackageFile> IPackageViewModel.GetFiles() {
            return _package.GetFiles();
        }

        void IPackageViewModel.BegingEdit() {
            IsInEditMode = true;
        }

        void IPackageViewModel.CancelEdit() {
            IsInEditMode = false;
        }

        void IPackageViewModel.CommitEdit() {
            HasEdit = true;
            IsInEditMode = false;
            RaisePropertyChangeEvent("WindowTitle");
        }
        
        #endregion
    }
}