using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using NuGet;
using PackageExplorerViewModel.Types;

namespace PackageExplorerViewModel {

    public class PackageViewModel : ViewModelBase {

        private readonly IPackage _package;
        private EditablePackageMetadata _packageMetadata;
        private PackageFolder _packageRoot;
        private ICommand _saveCommand, _editCommand, _cancelEditCommand, _applyEditCommand, _viewContentCommand, _saveContentCommand, _openContentFileCommand, _openWithContentFileCommand;
        private ICommand _addContentFolderCommand, _addContentFileCommand, _addNewFolderCommand, _deleteContentCommand;
        private readonly IMruManager _mruManager;
        private readonly IUIServices _uiServices;

        internal PackageViewModel(
            IPackage package,
            string source,
            IMruManager mruManager,
            IUIServices uiServices) {

            if (package == null) {
                throw new ArgumentNullException("package");
            }
            if (mruManager == null) {
                throw new ArgumentNullException("mruManager");
            }
            if (uiServices == null) {
                throw new ArgumentNullException("uiServices");
            }

            _uiServices = uiServices;
            _mruManager = mruManager;
            _package = package;
            _packageMetadata = new EditablePackageMetadata(_package);
            PackageSource = source;

            _packageRoot = PathToTreeConverter.Convert(_package.GetFiles().ToList(), this);
        }

        public IUIServices UIServices {
            get {
                return _uiServices;
            }
        }

        private bool _isInEditMode;
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

        private FileContentInfo _currentFileInfo;
        public FileContentInfo CurrentFileInfo {
            get { return _currentFileInfo; }
            set {
                if (_currentFileInfo != value) {
                    _currentFileInfo = value;
                    OnPropertyChanged("CurrentFileInfo");
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

        private string _packageSource;
        public string PackageSource {
            get { return _packageSource; }
            set {
                if (_packageSource != value) {
                    _packageSource = value;
                    OnPropertyChanged("PackageSource");
                }
            }
        }

        private bool _hasEdit;
        public bool HasEdit {
            get {
                return _hasEdit;
            }
            set {
                if (_hasEdit != value) {
                    _hasEdit = value;
                    OnPropertyChanged("HasEdit");
                }
            }
        }

        public void ShowFile(FileContentInfo fileInfo) {
            ShowContentViewer = true;
            CurrentFileInfo = fileInfo;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public IEnumerable<IPackageFile> GetFiles() {
            return _packageRoot.GetFiles();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public Stream GetCurrentPackageStream() {
            string tempFile = Path.GetTempFileName();
            PackageHelper.SavePackage(PackageMetadata, GetFiles(), tempFile, false);
            if (File.Exists(tempFile)) {
                return File.OpenRead(tempFile);
            }
            else {
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

        private void CommitEdit() {
            HasEdit = true;
            PackageMetadata.ResetErrors();
            IsInEditMode = false;
            OnPropertyChanged("WindowTitle");
        }

        internal void OnSaved(string fileName) {
            HasEdit = false;
            _mruManager.NotifyFileAdded(PackageMetadata, fileName, PackageType.LocalPackage);
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
            ExportManifest(Path.Combine(rootPath, PackageMetadata.Id + ".nuspec"));
        }

        internal void ExportManifest(string fullpath) {
            if (File.Exists(fullpath)) {
                bool confirmed = UIServices.Confirm(
                    String.Format(CultureInfo.CurrentCulture, Resources.ConfirmToReplaceFile, fullpath)
                );
                if (!confirmed) {
                    return;
                }
            }

            using (Stream fileStream = File.Create(fullpath)) {
                Manifest manifest = Manifest.Create(PackageMetadata);
                manifest.Save(fileStream);
            }
        }

        internal void NotifyContentDeleted(PackagePart packagePart) {
            // if the deleted file is being shown in the content pane, close the content pane
            if (CurrentFileInfo != null && CurrentFileInfo.File == packagePart) {
                CloseContentViewer();
            }

            NotifyChanges();
        }

        internal void CloseContentViewer() {
            ShowContentViewer = false;
            CurrentFileInfo = null;
        }

        #region AddContentFileCommand

        public ICommand AddContentFileCommand {
            get {
                if (_addContentFileCommand == null) {
                    _addContentFileCommand = new RelayCommand<object>(AddContentFileExecute, AddContentFileCanExecute);
                }

                return _addContentFileCommand;
            }
        }

        private bool AddContentFileCanExecute(object parameter) {
            return parameter == null || parameter is PackageFolder;
        }

        private void AddContentFileExecute(object parameter) {
            PackageFolder folder = parameter as PackageFolder;
            if (folder != null) {
                AddExistingFileToFolder(folder);
            }
            else {
                AddExistingFileToFolder(RootFolder);
            }
        }

        private void AddExistingFileToFolder(PackageFolder folder) {
            string[] selectedFiles;
            bool result = UIServices.OpenMultipleFilesDialog(
                "Select Files",
                "All files (*.*)|*.*",
                out selectedFiles);

            if (result) {
                foreach (string file in selectedFiles) {
                    folder.AddFile(file);
                }
            }
        }

        #endregion

        #region AddContentFolderCommand

        public ICommand AddContentFolderCommand {
            get {
                if (_addContentFolderCommand == null) {
                    _addContentFolderCommand = new RelayCommand<string>(AddContentFolderExecute, AddContentFolderCanExecute);
                }

                return _addContentFolderCommand;
            }
        }

        private bool AddContentFolderCanExecute(string folderName) {
            if (folderName == null) {
                return false;
            }

            return !RootFolder.ContainsFolder(folderName);
        }

        private void AddContentFolderExecute(string folderName) {
            RootFolder.AddFolder(folderName);
        }

        #endregion

        #region AddNewFolderCommand

        public ICommand AddNewFolderCommand {
            get {
                if (_addNewFolderCommand == null) {
                    _addNewFolderCommand = new RelayCommand<object>(AddNewFolderExecute, AddNewFolderCanExecute);
                }

                return _addNewFolderCommand;
            }
        }

        private bool AddNewFolderCanExecute(object parameter) {
            return parameter == null || parameter is PackageFolder;
        }

        private void AddNewFolderExecute(object parameter) {
            // this command do not apply to content file
            if (parameter != null && parameter is PackageFile) {
                return;
            }

            var folder = (parameter as PackageFolder) ?? RootFolder;
            folder.AddFolder("NewFolder");
        }

        #endregion

        #region SavePackageCommand

        public ICommand SaveCommand {
            get {
                if (_saveCommand == null) {
                    _saveCommand = new SavePackageCommand(this);
                }
                return _saveCommand;
            }
        }

        #endregion

        #region EditPackageCommand

        public ICommand EditCommand {
            get {
                if (_editCommand == null) {
                    _editCommand = new RelayCommand(EditPackageExecute, EditPackageCanExecute);
                }
                return _editCommand;
            }
        }

        private bool EditPackageCanExecute() {
            return !IsInEditMode;
        }

        private void EditPackageExecute() {
            BeginEdit();
        }

        #endregion

        #region ApplyEditCommand

        public ICommand ApplyEditCommand {
            get {
                if (_applyEditCommand == null) {
                    _applyEditCommand = new RelayCommand<IBindingGroup>(ApplyEditExecute);
                }

                return _applyEditCommand;
            }
        }

        private void ApplyEditExecute(IBindingGroup bindingGroup) {
            if (bindingGroup != null) {
                bool valid = bindingGroup.CommitEdit();
                if (valid) {
                    CommitEdit();
                }
            }
        }

        #endregion

        #region CancelEditCommand

        public ICommand CancelEditCommand {
            get {
                if (_cancelEditCommand == null) {
                    _cancelEditCommand = new RelayCommand<IBindingGroup>(CancelEditExecute);
                }

                return _cancelEditCommand;
            }
        }

        private void CancelEditExecute(IBindingGroup bindingGroup) {
            if (bindingGroup != null) {
                bindingGroup.CancelEdit();
            }

            CancelEdit();
        }

        #endregion

        #region DeleteContentCommand

        public ICommand DeleteContentCommand {
            get {
                if (_deleteContentCommand == null) {
                    _deleteContentCommand = new RelayCommand<object>(DeleteContentExecute, DeleteContentCanExecute);
                }

                return _deleteContentCommand;
            }
        }

        private bool DeleteContentCanExecute(object parameter) {
            return parameter is PackagePart;
        }

        private void DeleteContentExecute(object parameter) {
            var file = parameter as PackagePart;
            if (file != null) {
                file.Delete();
            }
        }

        #endregion

        #region OpenContentFileCommand

        public ICommand OpenContentFileCommand {
            get {
                if (_openContentFileCommand == null) {
                    _openContentFileCommand = new RelayCommand<PackageFile>(OpenContentFileExecute);
                }
                return _openContentFileCommand;
            }
        }

        private void OpenContentFileExecute(PackageFile file) {
            FileHelper.OpenFileInShell(file, UIServices);
        }

        #endregion

        #region OpenWithContentFileCommand

        public ICommand OpenWithContentFileCommand {
            get {
                if (_openWithContentFileCommand == null) {
                    _openWithContentFileCommand = new RelayCommand<PackageFile>(FileHelper.OpenFileInShellWith);
                }
                return _openWithContentFileCommand;
            }
        }

        #endregion

        #region SaveContentCommand

        public ICommand SaveContentCommand {
            get {
                if (_saveContentCommand == null) {
                    _saveContentCommand = new RelayCommand<PackageFile>(SaveContentExecute);
                }
                return _saveContentCommand;
            }
        }

        private void SaveContentExecute(PackageFile file) {
            string selectedFileName;
            string title = "Save " + file.Name;
            string filter = "All files (*.*)|*.*";
            if (UIServices.OpenSaveFileDialog(title, file.Name, filter, out selectedFileName)) {
                using (FileStream fileStream = File.OpenWrite(selectedFileName)) {
                    file.GetStream().CopyTo(fileStream);
                }
            }
        }

        #endregion
        
        #region ViewContentCommand

        public ICommand ViewContentCommand {
            get {
                if (_viewContentCommand == null) {
                    _viewContentCommand = new ViewContentCommand(this);
                }
                return _viewContentCommand;
            }
        }

        #endregion
    }
}