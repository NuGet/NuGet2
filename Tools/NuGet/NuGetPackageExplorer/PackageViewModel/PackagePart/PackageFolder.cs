using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using NuGet;

namespace PackageExplorerViewModel {

    public class PackageFolder : PackagePart {

        public ICollection<PackagePart> Children { get; private set; }

        public PackageFolder(string name, PackageFolder parent) : base(name, parent, parent.PackageViewModel) {
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
                    RaisePropertyChangeEvent("IsExpanded");
                }
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

        public void AddFolder(string folderName) {
            if (ContainsFolder(folderName)) {
                return;
            }
            var newFolder = new PackageFolder(folderName, this);
            Children.Add(newFolder);
            newFolder.IsSelected = true;
            this.IsExpanded = true;
            PackageViewModel.NotifyChanges();
        }

        public void AddFile(string filepath) {
            if (!File.Exists(filepath)) {
                throw new ArgumentException("File does not exist.", "filepath");
            }

            string name = System.IO.Path.GetFileName(filepath);
            if (ContainsFile(name)) {
                return;
            }
            
            var physicalFile = new PhysicalFile(filepath);
            var newFile = new PackageFile(physicalFile, name, this);
            Children.Add(newFile);
            newFile.IsSelected = true;
            this.IsExpanded = true;
            PackageViewModel.NotifyChanges();
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