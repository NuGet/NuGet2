using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Microsoft.Win32;
using NuGet;
using System.Globalization;

namespace PackageExplorerViewModel {

    public class PackageViewModel : ViewModelBase {

        private readonly IPackage _package;
        private EditablePackageMetadata _packageMetadata;
        private PackageFolder _packageRoot;
        private string _currentFileContent;
        private string _currentFileName;
        private ICommand _saveCommand, _editCommand, _cancelCommand, _applyCommand, _viewContentCommand, _saveContentCommand, _openContentFileCommand;
        private ICommand _addContentFolderCommand, _addContentFileCommand, _addNewFolderCommand, _deleteContentCommand;
        private bool _isInEditMode;
        private string _packageSource;

        public PackageViewModel(IPackage package, string source) {
            if (package == null) {
                throw new ArgumentNullException("package");
            }
            _package = package;
            _packageMetadata = new EditablePackageMetadata(_package);
            PackageSource = source;

            _packageRoot = PathToTreeConverter.Convert(_package.GetFiles().ToList(), this);
        }

        public bool IsInEditMode {
            get {
                return _isInEditMode;
            }
            private set {
                if (_isInEditMode != value) {
                    _isInEditMode = value;
                    OnPropertyChanged("IsInEditMode");
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
        }

        private bool _showContentViewer;
        public bool ShowContentViewer {
            get { return _showContentViewer; }
            set {
                if (_showContentViewer != value) {
                    _showContentViewer = value;
                    OnPropertyChanged("ShowContentViewer");
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
                    OnPropertyChanged("CurrentfileName");
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
                    OnPropertyChanged("CurrentFileContent");
                }
            }
        }

        public ICollection<PackagePart> PackageParts {
            get {
                return _packageRoot.Children;
            }
        }

        public bool IsValid {
            get {
                return GetFiles().Any() || PackageMetadata.Dependencies.Any() || PackageMetadata.FrameworkAssemblies.Any();
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

        public ICommand AddContentFolderCommand {
            get {
                if (_addContentFolderCommand == null) {
                    _addContentFolderCommand = new AddContentFolderCommand(this);
                }

                return _addContentFolderCommand;
            }
        }

        public ICommand AddContentFileCommand {
            get {
                if (_addContentFileCommand == null) {
                    _addContentFileCommand = new AddContentFileCommand(this);
                }

                return _addContentFileCommand;
            }
        }

        public ICommand AddNewFolderCommand {
            get {
                if (_addNewFolderCommand == null) {
                    _addNewFolderCommand = new AddNewFolderCommand(this);
                }

                return _addNewFolderCommand;
            }
        }

        public ICommand DeleteContentCommand {
            get {
                if (_deleteContentCommand == null) {
                    _deleteContentCommand = new DeleteContentCommand(this);
                }

                return _deleteContentCommand;
            }
        }

        public ICommand ViewContentCommand {
            get {
                if (_viewContentCommand == null) {
                    _viewContentCommand = new ViewContentCommand(this);
                }
                return _viewContentCommand;
            }
        }

        public ICommand SaveContentCommand {
            get {
                if (_saveContentCommand == null) {
                    _saveContentCommand = new SaveContentCommand(this);
                }
                return _saveContentCommand;
            }
        }

        public ICommand OpenContentFileCommand {
            get {
                if (_openContentFileCommand == null) {
                    _openContentFileCommand = new OpenContentFileCommand(this);
                }
                return _openContentFileCommand;
            }
        }

        #endregion

        private object _selectedItem;

        public object SelectedItem {
            get {
                return _selectedItem;
            }
            set {
                if (_selectedItem != value) {
                    _selectedItem = value;
                    OnPropertyChanged("SelectedItem");
                }
            }
        }

        public string PackageSource {
            get { return _packageSource; }
            set {
                if (_packageSource != value) {
                    _packageSource = value;
                    OnPropertyChanged("PackageSource");
                }
            }
        }

        public bool HasEdit {
            get;
            private set;
        }

        public void ShowFile(string name, string content) {
            CurrentFileName = name;
            CurrentFileContent = content;
            ShowContentViewer = true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        public bool OpenSaveFileDialog(string defaultName, bool addPackageExtension, out string selectedFileName) {

            var filter = "All files (*.*)|*.*";
            if (addPackageExtension)
            {
                filter = "NuGet package file (*.nupkg)|*.nupkg|" + filter;
            }
            var dialog = new SaveFileDialog() {
                OverwritePrompt = true,
                Title = "Save " + defaultName,
                Filter = filter,
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public IEnumerable<IPackageFile> GetFiles() {
            return _packageRoot.GetFiles();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public Stream GetCurrentPackageStream()
        {
            string tempFile = Path.GetTempFileName();
            PackageHelper.SavePackage(PackageMetadata, GetFiles(), tempFile, false);
            if (File.Exists(tempFile))
            {
                return File.OpenRead(tempFile);
            }
            else
            {
                return null;
            }
        }

        public void BeginEdit() {
            // raise the property change event here to force the edit form to rebind 
            // all controls, which will erase all error states, if any, left over from the previous edit
            OnPropertyChanged("PackageMetadata");
            IsInEditMode = true;
        }

        public void CancelEdit() {
            PackageMetadata.ResetErrors();
            IsInEditMode = false;
        }

        public void CommitEdit() {
            HasEdit = true;
            PackageMetadata.ResetErrors();
            IsInEditMode = false;
            OnPropertyChanged("WindowTitle");
        }

        internal void OnSaved() {
            HasEdit = false;
        }

        internal void NotifyChanges() {
            HasEdit = true;
        }

        public PackageFolder RootFolder {
            get {
                return _packageRoot;
            }
        }

        public void Export(string rootPath) {
            if (rootPath == null) {
                throw new ArgumentNullException("rootPath");
            }

            if (!Directory.Exists(rootPath)) {
                throw new ArgumentException("Specified directory doesn't exist.");
            }

            // export files
            RootFolder.Export(rootPath);

            // export .nuspec file
            ExportManifest(rootPath, PackageMetadata);
        }

        private static void ExportManifest(string rootPath, EditablePackageMetadata metadata) {
            string filename = metadata.Id + ".nuspec";
            string fullpath = Path.Combine(rootPath, filename);
            using (Stream fileStream = File.Create(fullpath)) {
                Manifest manifest = Manifest.Create(metadata);
                manifest.Save(fileStream);
            }
        }
    }
}