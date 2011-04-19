using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using NuGet;

namespace PackageExplorerViewModel {

    public class PackageFolder : PackagePart {

        public ICollection<PackagePart> Children { get; private set; }

        public PackageFolder(string name, PackageFolder parent)
            : base(name, parent, parent.PackageViewModel) {
            this.Children = new SortedCollection<PackagePart>();
        }

        public PackageFolder(string name, PackageViewModel viewModel)
            : base(name, null, viewModel) {
            this.Children = new SortedCollection<PackagePart>();
        }

        internal override void UpdatePath() {
            base.UpdatePath();

            if (Children != null) {
                foreach (var child in Children) {
                    child.UpdatePath();
                }
            }
        }

        public override IEnumerable<IPackageFile> GetFiles() {
            return Children.SelectMany(p => p.GetFiles());
        }

        public void RemoveChild(PackagePart child) {
            if (child == null) {
                throw new ArgumentNullException("child");
            }

            bool removed = Children.Remove(child);
            if (removed) {
                PackageViewModel.NotifyChanges();
            }
        }

        public ICommand AddContentFileCommand {
            get {
                return PackageViewModel.AddContentFileCommand;
            }
        }

        public ICommand AddNewFolderCommand {
            get {
                return PackageViewModel.AddNewFolderCommand;
            }
        }

        private bool _isExpanded;

        public bool IsExpanded {
            get {
                return _isExpanded;
            }
            set {
                if (_isExpanded != value) {
                    _isExpanded = value;
                    OnPropertyChanged("IsExpanded");
                }
            }
        }

        public PackagePart this[string name] {
            get {
                return Children.SingleOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
        }

        public bool ContainsFolder(string folderName) {
            if (Children == null) {
                return false;
            }

            return Children.Any(p => p is PackageFolder && p.Name.Equals(folderName, StringComparison.OrdinalIgnoreCase));
        }

        public bool ContainsFile(string fileName) {
            if (Children == null) {
                return false;
            }

            return Children.Any(p => p is PackageFile && p.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        }

        public PackageFolder AddFolder(string folderName) {
            if (ContainsFolder(folderName) || ContainsFile(folderName)) {
                PackageViewModel.UIServices.Show(Resources.RenameCausesNameCollison, Types.MessageLevel.Error);
                return null;
            }
            var newFolder = new PackageFolder(folderName, this);
            Children.Add(newFolder);
            newFolder.IsSelected = true;
            this.IsExpanded = true;
            PackageViewModel.NotifyChanges();
            return newFolder;
        }

        public PackageFile AddFile(string filePath) {
            if (!File.Exists(filePath)) {
                throw new ArgumentException("File does not exist.", "filePath");
            }

            string newFileName = System.IO.Path.GetFileName(filePath);
            if (ContainsFolder(newFileName)) {
                PackageViewModel.UIServices.Show(Resources.FileNameConflictWithExistingDirectory, Types.MessageLevel.Error);
                return null;
            }

            bool showingRemovedFile = false;
            if (ContainsFile(newFileName)) {
                bool confirmed = PackageViewModel.UIServices.Confirm(Resources.ConfirmToReplaceExsitingFile, true);
                if (confirmed) {
                    // check if we are currently showing the content of the file to be removed.
                    // if we are, we'll need to show the new content after replacing the file.
                    if (PackageViewModel.ShowContentViewer) {
                        PackagePart part = this[newFileName];
                        if (PackageViewModel.CurrentFileInfo.File == part) {
                            showingRemovedFile = true;
                        }
                    }

                    // remove the existing file before adding the new one
                    RemoveChildByName(newFileName);
                }
                else {
                    return null;
                }
            }

            var physicalFile = new PhysicalFile(filePath);
            var newFile = new PackageFile(physicalFile, newFileName, this);
            Children.Add(newFile);
            newFile.IsSelected = true;
            this.IsExpanded = true;
            PackageViewModel.NotifyChanges();

            if (showingRemovedFile) {
                ICommand command = PackageViewModel.ViewContentCommand;
                command.Execute(newFile);
            }

            return newFile;
        }

        private void RemoveChildByName(string name) {
            int count = Children.RemoveAll(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            Debug.Assert(count <= 1);
            if (count == 1) {
                PackageViewModel.NotifyChanges();
            }
        }

        public override void Export(string rootPath) {
            string fullPath = System.IO.Path.Combine(rootPath, Path);
            if (!Directory.Exists(fullPath)) {
                Directory.CreateDirectory(fullPath);
            }

            foreach (var part in Children) {
                part.Export(rootPath);
            }
        }
    }
}