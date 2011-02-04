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
        private IPackageMetadata _packageMetadata;
        private readonly EditablePackageMetadata _editablePackageMetadata;
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
            _packageMetadata = _package;
            _editablePackageMetadata = new EditablePackageMetadata();
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
                return Resources.Dialog_Title + " - " + _package.ToString();
            }
        }

        public IPackageMetadata PackageMetadata {
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

        public EditablePackageMetadata EditablePackageMetadata {
            get {
                return _editablePackageMetadata;
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
                    _packageParts = PathToTreeConverter.Convert(_package.GetFiles().ToList(), this).Children;
                }

                return _packageParts;
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

        void IPackageViewModel.StartEditMode() {
            _editablePackageMetadata.CopyFrom(PackageMetadata);
            RaisePropertyChangeEvent("EditablePackageMetadata");
            IsInEditMode = true;
        }

        void IPackageViewModel.CancelEditMode() {
            IsInEditMode = false;
        }

        void IPackageViewModel.ApplyPackageMetadataChanges() {
            PackageMetadata = EditablePackageMetadata.Clone();
            IsInEditMode = false;
        }
        
        #endregion
    }
}