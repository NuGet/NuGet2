using System;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using NuGet;

namespace PackageExplorerViewModel {

    public class EditablePackageDependency : INotifyPropertyChanged, IDataErrorInfo {
        private const string LessThanOrEqualTo = "\u2264";
        private const string GreaterThanOrEqualTo = "\u2265";

        public EditablePackageDependency() {
        }

        public EditablePackageDependency(string id, IVersionSpec versionSpec) {
            this.Id = id;
            this.VersionSpec = versionSpec;
        }

        private string _id;

        public string Id {
            get {
                return _id;
            }
            set {
                if (String.IsNullOrEmpty(value)) {
                    throw new ArgumentException("Id is required.");
                }

                PackageIdValidator.ValidatePackageId(value);

                if (_id != value) {
                    _id = value;
                    RaisePropertyChange("Id");
                }
            }
        }

        private IVersionSpec _versionSpec;

        public IVersionSpec VersionSpec {
            get { return _versionSpec; }
            set {
                if (value == null) {
                    throw new ArgumentException("Dependency version is required.");
                }

                if (_versionSpec != value) {
                    _versionSpec = value;
                    RaisePropertyChange("VersionSpec");
                }
            }
        }

        public override string ToString() {
            if (VersionSpec == null) {
                return Id;
            }

            if (VersionSpec.MinVersion != null && VersionSpec.IsMinInclusive && VersionSpec.MaxVersion == null && !VersionSpec.IsMaxInclusive) {
                return String.Format(CultureInfo.InvariantCulture, "{0} ({1} {2})", Id, GreaterThanOrEqualTo, VersionSpec.MinVersion);
            }

            if (VersionSpec.MinVersion != null && VersionSpec.MaxVersion != null && VersionSpec.MinVersion == VersionSpec.MaxVersion && VersionSpec.IsMinInclusive && VersionSpec.IsMaxInclusive) {
                return String.Format(CultureInfo.InvariantCulture, "{0} (= {1})", Id, VersionSpec.MinVersion);
            }

            var versionBuilder = new StringBuilder();
            if (VersionSpec.MinVersion != null) {
                if (VersionSpec.IsMinInclusive) {
                    versionBuilder.AppendFormat(CultureInfo.InvariantCulture, "({0} ", GreaterThanOrEqualTo);
                }
                else {
                    versionBuilder.Append("(> ");
                }
                versionBuilder.Append(VersionSpec.MinVersion);
            }

            if (VersionSpec.MaxVersion != null) {
                if (versionBuilder.Length == 0) {
                    versionBuilder.Append("(");
                }
                else {
                    versionBuilder.Append(" && ");
                }

                if (VersionSpec.IsMaxInclusive) {
                    versionBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0} ", LessThanOrEqualTo);
                }
                else {
                    versionBuilder.Append("< ");
                }
                versionBuilder.Append(VersionSpec.MaxVersion);
            }

            if (versionBuilder.Length > 0) {
                versionBuilder.Append(")");
            }

            return Id + " " + versionBuilder;
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
                if (String.IsNullOrEmpty(Id)) {
                    return "Id is required.";
                }

                if (!PackageIdValidator.IsValidPackageId(Id)) {
                    return "Value '" + Id + "' cannot be converted.";
                }
            }
            else if (columnName == "VersionSpec") {
                if (VersionSpec == null) {
                    return "Dependency version is required.";
                }
            }

            return null;
        }
    }
}