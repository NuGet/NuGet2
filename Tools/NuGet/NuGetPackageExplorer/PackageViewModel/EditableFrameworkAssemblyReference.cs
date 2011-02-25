using System;
using System.ComponentModel;
using NuGet;
using System.Runtime.Versioning;
using System.Collections.Generic;

namespace PackageExplorerViewModel {

    public class EditableFrameworkAssemblyReference : INotifyPropertyChanged, IDataErrorInfo {

        public EditableFrameworkAssemblyReference() {
        }

        public EditableFrameworkAssemblyReference(string assemblyName, IEnumerable<FrameworkName> supportedFrameworks) {
            this.AssemblyName = assemblyName;
            this.SupportedFrameworks = supportedFrameworks;
        }

        private string _assemblyName;

        public string AssemblyName {
            get {
                return _assemblyName;
            }
            set {
                if (_assemblyName != value) {
                    _assemblyName = value;
                    RaisePropertyChange("AssemblyName");
                }
            }
        }

        private IEnumerable<FrameworkName> _supportedFrameworks;

        public IEnumerable<FrameworkName> SupportedFrameworks {
            get { return _supportedFrameworks; }
            set {
                if (_supportedFrameworks != value) {
                    _supportedFrameworks = value;
                    RaisePropertyChange("SupportedFrameworks");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChange(string propertyName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string Error {
            get { return null; }
        }

        public string this[string columnName] {
            get { return IsValid(columnName); }
        }

        private string IsValid(string columnName) {
            if (columnName == "Id") {

                if (String.IsNullOrEmpty(AssemblyName)) {
                    return null;
                }

                if (!PackageIdValidator.IsValidPackageId(AssemblyName)) {
                    return "Value '" + AssemblyName + "' cannot be converted.";
                }
            }

            return null;
        }

        public FrameworkAssemblyReference AsReadOnly() {
            return new FrameworkAssemblyReference(AssemblyName, SupportedFrameworks);
        }
    }
}