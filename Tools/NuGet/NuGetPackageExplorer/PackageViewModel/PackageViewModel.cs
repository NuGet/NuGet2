using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using NuGet;

namespace PackageExplorerViewModel {
   
    public class PackageViewModel : INotifyPropertyChanged, IShowFileContentHandler {

        private readonly IPackage _package;
        private IList<PackagePart> _packageParts;
        private string _currentFileContent;
        private string _currentFileName;

        public event PropertyChangedEventHandler PropertyChanged;

        public PackageViewModel(IPackage package, string source) {
            if (package == null) {
                throw new ArgumentNullException("package");
            }
            _package = package;

            PackageSource = source;
        }

        public string PackageSource { 
            get; private set; 
        }

        public string WindowTitle {
            get {
                return Resources.Dialog_Title + " - " + _package.ToString();
            }
        }

        public IPackageMetadata PackageMetadata {
            get {
                return _package;
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

        public void ShowFile(string name, string content) {
            CurrentFileName = name;
            CurrentFileContent = content;
        }
    }
}