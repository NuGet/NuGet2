using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using NuGet;

namespace PackageExplorerViewModel {
    public abstract class PackagePart : IComparable<PackagePart>, INotifyPropertyChanged {

        protected PackagePart(string name, PackageFolder parent, PackageViewModel viewModel) {
            if (name == null) {
                throw new ArgumentNullException("name");
            }

            if (viewModel == null) {
                throw new ArgumentNullException("viewModel");
            }

            _viewModel = viewModel;
            _parent = parent;
            this.Name = name;
        }

        private readonly PackageFolder _parent;

        public PackageFolder Parent {
            get { return _parent; }
        }

        private readonly PackageViewModel _viewModel;

        public PackageViewModel PackageViewModel {
            get { return _viewModel; }
        }

        private string _name;

        public string Name {
            get {
                return _name;
            }
            set {
                if (_name != value) {
                    _name = value;
                    RaisePropertyChangeEvent("Name");
                    UpdatePath();
                }
            }
        }

        private string _path;

        public string Path {
            get {
                return _path;
            }
            set {
                if (_path != value) {
                    _path = value;
                    RaisePropertyChangeEvent("Path");
                }
            }
        }

        private bool _isSelected;

        public bool IsSelected {
            get {
                return _isSelected;
            }
            set {
                if (_isSelected != value) {
                    _isSelected = value;
                    RaisePropertyChangeEvent("IsSelected");
                }
            }
        }

        private ICommand _deleteCommand;

        public ICommand DeleteCommand {
            get {
                if (_deleteCommand == null) {
                    _deleteCommand = new DeleteContentCommand2(PackageViewModel);
                }
                return _deleteCommand;
            }
        }

        public abstract void Export(string rootPath);

        public void Rename(string newName) {
            if (Name != newName) {
                Name = newName;
                PackageViewModel.NotifyChanges();
            }
        }

        public void Delete() {
            if (Parent != null) {
                Parent.Children.Remove(this);
                PackageViewModel.NotifyChanges();
            }
        }

        public int CompareTo(PackagePart other) {
            if (this == other) {
                return 0;
            }

            if (other == null) {
                return 1;
            }

            // folder goes before file
            if (this is PackageFolder && other is PackageFile) {
                return -1;
            }

            if (this is PackageFile && other is PackageFolder) {
                return 1;
            }

            return String.Compare(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public abstract IEnumerable<IPackageFile> GetFiles();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChangeEvent(string propertyName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void RecalculatePath() {
            Path = (Parent == null || String.IsNullOrEmpty(Parent.Path)) ? Name : (Parent.Path + "\\" + Name);
        }

        internal virtual void UpdatePath() {
            RecalculatePath();
        }
    }
}