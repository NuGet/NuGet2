using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using NuGet;

namespace PackageExplorerViewModel {
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1036:OverrideMethodsOnComparableTypes")]
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
            _name = name;
            RecalculatePath();
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
                    OnPropertyChanged("Name");
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
                    OnPropertyChanged("Path");
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
                    OnPropertyChanged("IsSelected");
                }
            }
        }

        public ICommand DeleteCommand {
            get {
                return PackageViewModel.DeleteContentCommand;
            }
        }

        public ICommand RenameCommand {
            get {
                return PackageViewModel.RenameContentCommand;
            }
        }

        public abstract void Export(string rootPath);

        public void Rename(string newName) {
            if (Name != newName) {
                if (Parent != null) {
                    if (Parent.ContainsFile(newName) || Parent.ContainsFolder(newName)) {
                        PackageViewModel.UIServices.Show(Resources.RenameCausesNameCollison, Types.MessageLevel.Error);
                        return;
                    }
                }

                Name = newName;
                PackageViewModel.NotifyChanges();
            }
        }

        public void Delete() {
            bool confirm = PackageViewModel.UIServices.Confirm(
                String.Format(CultureInfo.CurrentCulture, Resources.ConfirmToDeleteContent, Name),
                isWarning: true);
            if (!confirm) {
                return;
            }

            if (Parent != null) {
                Parent.Children.Remove(this);
                PackageViewModel.NotifyContentDeleted(this);
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "This method is potentially expensive.")]
        public abstract IEnumerable<IPackageFile> GetFiles();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName) {
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

        public override bool Equals(object obj) {
            PackagePart other = obj as PackagePart;
            if (other == null) {
                return false;
            }

            return CompareTo(other) == 0;
        }

        public override int GetHashCode() {
            return Name.ToUpper(CultureInfo.InvariantCulture).GetHashCode();
        }
    }
}