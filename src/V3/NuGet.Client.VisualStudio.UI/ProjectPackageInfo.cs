using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using NuGet.Client.ProjectSystem;
using NuGet.Versioning;

namespace NuGet.Client.VisualStudio.UI
{
    // This class is used to represent one of the following facts about a package:
    // - A version of the package is installed. In this case, property Version is not null. 
    //   Property IsSolution indicates if the package is installed in the solution or in a project.
    // - The package is not installed in a project/solution. In this case, property Version is null.
    public class PackageInstallationInfo : IComparable<PackageInstallationInfo>,
        INotifyPropertyChanged
    {
        private NuGetVersion _version;

        public NuGetVersion Version
        {
            get
            {
                return _version;
            }
            set
            {
                _version = value;
                UpdateDisplayText();
            }
        }

        public event EventHandler SelectedChanged;

        private bool _selected;

        public bool Selected
        {
            get
            {
                return _selected;
            }
            set
            {
                if (_selected != value)
                {
                    _selected = value;
                    if (SelectedChanged != null)
                    {
                        SelectedChanged(this, EventArgs.Empty);
                    }
                    OnPropertyChanged("Selected");
                }
            }
        }

        private bool _enabled;

        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    OnPropertyChanged("Enabled");
                }
            }
        }

        public Project Project
        {
            get;
            private set;
        }

        private string _name;
        
        public bool IsSolution
        {
            get;
            private set;
        }

        public PackageInstallationInfo(Project project, NuGetVersion version, bool enabled)
        {
            Project = project;
            _name = Project.Name;
            _selected = enabled;
            Version = version;
            Enabled = enabled;
            IsSolution = false;

            UpdateDisplayText();
        }

        // Create PackageInstallationInfo for the solution.
        public PackageInstallationInfo(string name, NuGetVersion version, bool enabled, Project project)
        {
            _name = name;
            Version = version;
            Enabled = enabled;
            _selected = enabled;
            IsSolution = true;

            // this is just a placeholder and will not be really used. It's used to avoid
            // lots of null checks in our code.
            Project = project;

            UpdateDisplayText();
        }

        private string _displayText;

        // the text to be displayed in UI
        public string DisplayText
        {
            get
            {
                return _displayText;
            }
            set
            {
                if (_displayText != value)
                {
                    _displayText = value;
                    OnPropertyChanged("DisplayText");
                }
            }
        }

        private void UpdateDisplayText()
        {
            if (Version == null)
            {
                DisplayText = _name;
            }
            else
            {
                DisplayText = string.Format("{0} ({1})", _name,
                    Version.ToNormalizedString());
            }
        }

        public int CompareTo(PackageInstallationInfo other)
        {
            return this._name.CompareTo(other._name);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }    
}
