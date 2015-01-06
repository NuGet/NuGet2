using System;
using System.Diagnostics;
using System.Globalization;
using NuGet.Client.ProjectSystem;
using NuGet.Versioning;
using NuGet.ProjectManagement;

namespace NuGet.Client.VisualStudio.UI
{
    // This class is used to represent one of the following facts about a package:
    // - A version of the package is installed. In this case, property Version is not null. 
    //   Property IsSolution indicates if the package is installed in the solution or in a project.
    // - The package is not installed in a project/solution. In this case, property Version is null.
    public class PackageInstallationInfo
    {
        public NuGetVersion Version
        {
            get;
            private set;
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
                _selected = value;
                if (SelectedChanged != null)
                {
                    SelectedChanged(this, EventArgs.Empty);
                }
            }
        }

        public bool Enabled
        {
            get;
            set;
        }

        public NuGetProject Project
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

        public PackageInstallationInfo(NuGetProject project, NuGetVersion version, bool enabled)
        {
            Project = project;
            _name = "NOT IMPL"; // TODO: implement this
            _selected = enabled;
            Version = version;
            Enabled = enabled;
            IsSolution = false;
        }

        // Create PackageInstallationInfo for the solution.
        public PackageInstallationInfo(string name, NuGetVersion version, bool enabled, NuGetProject project)
        {
            _name = name;
            Version = version;
            Enabled = enabled;
            _selected = enabled;
            IsSolution = true;

            // this is just a placeholder and will not be really used. It's used to avoid
            // lots of null checks in our code.
            Project = project;
        }

        public override string ToString()
        {
            if (Version == null)
            {
                return _name;
            }
            else
            {
                return string.Format("{0} ({1})", _name,
                    Version.ToNormalizedString());
            }
        }
    }
}
