using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.WebMatrix.Utility;
using NuGet;

namespace NuGet.WebMatrix
{
    internal class PackageViewModel : NotifyPropertyChanged
    {
        private static readonly Lazy<ImageSource> _defaultIconImageSource = new Lazy<ImageSource>(() =>
        {
            return Extensions.ConvertToImageSource(Resources.NugetIcon96);
        });

        private IPackage _package;
        private IPackage _remotePackage;
        private bool _shouldPullRemotePackage;
        private Lazy<string> _name;
        private Lazy<string> _searchText;
        private Lazy<ImageSource> _iconImageSource;
        private Lazy<string> _authors;
        private Lazy<IEnumerable<PackageViewModel>> _dependencies;
        private bool? _isEnabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:PackageViewModel"/> class.
        /// </summary>
        public PackageViewModel(
            NuGetModel model,
            IPackage package,
            PackageViewModelAction packageAction)
            : this(model, package, false, packageAction)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:PackageViewModel"/> class for an installed package
        /// </summary>
        public PackageViewModel(
            NuGetModel model,
            IPackage package,
            bool shouldPullRemotePackage,
            PackageViewModelAction packageAction)
        {
            Debug.Assert(package != null, "package parameter should not be null");

            this.Model = model;
            _package = package;
            _shouldPullRemotePackage = shouldPullRemotePackage;
            this.PackageAction = packageAction;

            this.LaunchUrlCommand = new RelayCommand(url => this.OpenUrl(url as Uri));

            SetName();
            SetSearchtext();
            _authors = new Lazy<string>(GetAuthors);
            SetDependencies();
            SetIconSource();
        }

        private NuGetModel Model
        {
            get;
            set;
        }

        private PackageViewModelAction PackageAction
        {
            get;
            set;
        }

        private void SetDependencies()
        {
            _dependencies = new Lazy<IEnumerable<PackageViewModel>>(() => DetermineDependencies());
        }

        private string GetAuthors()
        {
            if (_package.Owners == null)
            {
                return null;
            }
            else
            {
                return String.Join(", ", _package.Authors.Select(a => a.Trim()));
            }
        }

        private void SetSearchtext()
        {
            _searchText = new Lazy<string>(() =>
            {
                // TODO : Verify that we want to search on the description as well as the Name
                return string.Format("{0} {1}", this.Name, _package.Description ?? string.Empty).Trim();
            });
        }

        private void SetName()
        {
            _name = new Lazy<string>(() =>
                {
                    // The name is the Title, or, if the title is not
                    // set, the package Id
                    string name = _package.Title;
                    if (string.IsNullOrEmpty(name))
                    {
                        name = _package.Id;
                    }

                    return name;
                });
        }

        private void SetIconSource()
        {
            _iconImageSource = new Lazy<ImageSource>(() => (this.IconUrl == null ? DefaultIconImageSource : (ImageSource)new BitmapImage(this.IconUrl)));
        }

        private IEnumerable<PackageViewModel> DetermineDependencies()
        {
            if (this.HasDependencies)
            {
                var packageDependencies = this.Model.FindDependenciesToBeInstalled(_package);

                // Loop over each PackageDependency and determine if it is installed or not
                // Return the list of IPackage instances which are not already installed
                List<PackageViewModel> dependencies = new List<PackageViewModel>();
                foreach (var dependency in packageDependencies)
                {
                    Debug.Assert(dependency != null, "The dependency was not found in the SourceRepository - NuGet bug?");
                    if (dependency != null)
                    {
                        // pass on _isFeatured for dependencies so telemetry represents top level usage
                        dependencies.Add(new PackageViewModel(this.Model, dependency, this.PackageAction));
                    }
                }

                return dependencies;
            }
            else
            {
                return null;
            }
        }

        public static ImageSource DefaultIconImageSource
        {
            get
            {
                return _defaultIconImageSource.Value;
            }
        }

        public string Description
        {
            get
            {
                var description = _package.Description;
                if (String.IsNullOrWhiteSpace(description))
                {
                    return _package.Summary;
                }
                else
                {
                    return description;
                }
            }
        }

        public int? UninstalledDownloadCount
        {
            get
            {
                return IsInstalled ? null : DownloadCount;
            }
        }

        public int? DownloadCount
        {
            get
            {
                DataServicePackage dataServicePackage = _package as DataServicePackage ?? this.RemotePackage as DataServicePackage;
                if (dataServicePackage == null)
                {
                    return null;
                }
                else
                {
                    return dataServicePackage.DownloadCount;
                }
            }
        }

        private void InitializeRemotePackage()
        {
            /// REMOTEPACKAGE SHOULD BE NULL IF THE FILTER IS NOT 'INSTALLED' 
            /// IF THE FILTER IS INSTALLED, WE QUERY FOR THE REMOTE PACKAGE WHEN 'HasUpdates' property IS RETRIEVED
            /// THIS HAPPENS WHEN A PACKAGE IS SELECTED IN THE INSTALLED FILTER
            /// USING THE REMOTE PACKAGE, WE DETERMINE IF A NEW VERSION IS AVAILABLE. ALSO, WE USE THE REMOTE PACKAGE
            /// TO GET PACKAGE GALLERY URL, ICON URL, and REPORT ABUSE URL
            if (_remotePackage == null && _shouldPullRemotePackage)
            {
                var packageManager = this.Model.PackageManager;
                var installedPackage = this.InstalledPackage;
                _remotePackage = packageManager.GetUpdate(InstalledPackage) ??
                    (packageManager.FindPackage(installedPackage.Id, installedPackage.Version) ?? _package);
                this.OnPropertyChanged("GalleryPackageUrl");
                this.OnPropertyChanged("IconUrl");
                this.OnPropertyChanged("ReportAbuseUrl");
                this.OnPropertyChanged("DisplayInstalledVersion");
            }
        }

        public bool HasUpdates
        {
            get
            {
                // checking for updates can be tricky because the view model might have
                // the 'remote' package or the 'local' and 'remote' packages
                // we want to compare the remote package first if we have one
                var installedPackage = this.GetInstalledPackages().SingleOrDefault();
                InitializeRemotePackage();
                if (installedPackage != null)
                {                    
                    if (this.RemotePackage != null)
                    {
                        return installedPackage.Version < this.RemotePackage.Version;
                    }
                    else
                    {
                        return installedPackage.Version < this.Package.Version;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        public ImageSource IconImageSource
        {
            get
            {
                return _iconImageSource.Value;
            }
        }

        public Uri IconUrl
        {
            get
            {
                if (Package.IconUrl != null && Package.IconUrl.IsAbsoluteUri)
                {
                    return Package.IconUrl;
                }
                else if (RemotePackage != null && RemotePackage.IconUrl != null && RemotePackage.IconUrl.IsAbsoluteUri)
                {
                    return RemotePackage.IconUrl;
                }
                else
                {
                    return null;
                }
            }
        }

        public DateTime? LastUpdated
        {
            get
            {
                var utcTime = _package.Published;
                if (utcTime.HasValue)
                {
                    return utcTime.Value.ToLocalTime().DateTime;
                }
                else
                {
                    return null;
                }
            }
        }

        public ICommand LaunchUrlCommand
        {
            get;
            private set;
        }

        public Uri LicenseUrl
        {
            get
            {
                if (Package.LicenseUrl != null && Package.LicenseUrl.IsAbsoluteUri)
                {
                    return Package.LicenseUrl;
                }
                else
                {
                    return null;
                }
            }
        }

        public Uri GalleryPackageUrl
        {
            get
            {
                DataServicePackage dataServicePackage = _package as DataServicePackage ?? this.RemotePackage as DataServicePackage;
                if (dataServicePackage == null)
                {
                    return null;
                }
                else
                {
                    return dataServicePackage.GalleryDetailsUrl;
                }
            }
        }

        public string Id
        {
            get
            {
                return _package.Id;
            }
        }

        public IPackage InstalledPackage
        {
            get
            {
                return this.GetInstalledPackages().FirstOrDefault();
            }
        }

        /// <summary>
        /// This is the installed version of the package
        /// If a package is not installed, this will return null and does not apply
        /// </summary>
        public string InstalledVersion
        {
            get
            {
                var installedPackage = this.InstalledPackage;
                if (installedPackage != null)
                {
                    return installedPackage.Version.ToString();
                }
                else
                {
                    return null;
                }
            }
        }

        public bool IsInstalled
        {
            get
            {
                return this.GetInstalledPackages().Any();
            }
        }

        public bool IsUpdating
        {
            get
            {
                return this.PackageAction == PackageViewModelAction.Update;
            }
        }

        public string Name
        {
            get
            {
                return _name.Value;
            }
        }

        public string Summary
        {
            get
            {
                var summary = _package.Summary;
                if (String.IsNullOrWhiteSpace(summary))
                {
                    return _package.Description;
                }
                else
                {
                    return summary;
                }
            }
        }

        public bool SupportsEnableDisable
        {
            get
            {
                return this.Model.PackageManager.SupportsEnableDisable;
            }
        }

        public string SearchText
        {
            get
            {
                return _searchText.Value;
            }
        }

        public string Authors
        {
            get
            {
                return _authors.Value;
            }
        }

        public bool HasDependencies
        {
            get
            {
                return _package.DependencySets != null && _package.DependencySets.Any(ds => ds.Dependencies.Any());
            }
        }

        /// <summary>
        /// This property is blocking and may take some time to return. It's a property rather than a method because it
        /// can be used by a WPF binding.
        /// </summary>
        public IEnumerable<PackageViewModel> Dependencies
        {
            get
            {
                var dependencies = _dependencies.Value;

                // If there are no dependencies, return null
                if (dependencies == null || !dependencies.Any())
                {
                    return null;
                }
                else
                {
                    return dependencies;
                }
            }
        }

        /// <summary>
        /// REMOTEPACKAGE SHOULD BE NULL IF THE FILTER IS NOT 'INSTALLED'
        /// IF THE FILTER IS INSTALLED, WE QUERY FOR THE REMOTE PACKAGE WHEN 'HasUpdates' property IS RETRIEVED
        /// THIS HAPPENS WHEN A PACKAGE IS SELECTED IN THE INSTALLED FILTER
        /// USING THE REMOTE PACKAGE, WE DETERMINE IF A NEW VERSION IS AVAILABLE. ALSO, WE USE THE REMOTE PACKAGE
        /// TO GET PACKAGE GALLERY URL, ICON URL, and REPORT ABUSE URL
        /// </summary>
        internal IPackage RemotePackage
        {
            get
            {
                return _remotePackage;
            }
        }

        internal IPackage Package
        {
            get
            {
                return _package;
            }
        }

        /// <summary>
        /// This is the version displayed alongside the package ID
        /// In the installed filter, this is the installed version
        /// On other filters, this is the latest uninstalled version
        /// </summary>
        public string DisplayVersion
        {
            get
            {
                return IsInstalled ? ((_package.Version != null) ? _package.Version.ToString() : null) : Resources.String_NotInstalled;
            }
        }

        /// <summary>
        /// THIS DOES NOT APPLY FOR THE INSTALLED FILTER. null is returned for the installed filter
        /// This is the installed version of the package
        /// </summary>
        public string DisplayInstalledVersion
        {
            get
            {
                return _shouldPullRemotePackage ? null : InstalledVersion;
            }
        }

        /// <summary>
        /// THIS IS ONLY USED WHEN THE PACKAGE IS NOT INSTALLED OR NEEDS AN UPDATE
        /// This is the newer version of the package that is not installed
        /// </summary>
        public string LatestUninstalledVersion
        {
            get
            {
                string version;
                if (this.RemotePackage != null)
                {
                    version = this.RemotePackage.Version.ToString();
                }
                else if (_package.Version != null)
                {
                    version = _package.Version.ToString();
                }
                else
                {
                    version = null;
                }

                return version;
            }
        }

        public Uri ProjectUrl
        {
            get
            {
                if (_package.ProjectUrl != null && _package.ProjectUrl.IsAbsoluteUri)
                {
                    return _package.ProjectUrl;
                }
                else
                {
                    return null;
                }
            }
        }

        public Uri ReportAbuseUrl
        {
            get
            {
                DataServicePackage dataServicePackage = _package as DataServicePackage ?? this.RemotePackage as DataServicePackage;
                if (dataServicePackage == null)
                {
                    return null;
                }
                else
                {
                    return dataServicePackage.ReportAbuseUrl;
                }
            }
        }

        public bool IsEnabled
        {
            get
            {
                if (_isEnabled == null)
                {
                    _isEnabled = this.Model.PackageManager.IsPackageEnabled(this.Package);
                }

                return _isEnabled.Value;
            }

            set
            {
                if (IsEnabled != value)
                {
                    Debug.Assert(
                        this.Model.PackageManager.SupportsEnableDisable,
                        "This is only valid for a package manager that supports enabled/disable");

                    this.Model.PackageManager.TogglePackageEnabled(this.Package);

                    _isEnabled = value;
                    OnPropertyChanged("IsEnabled");
                }
            }
        }

        public bool IsMandatory
        {
            get
            {
                return this.Model.PackageManager.IsMandatory(this.Package);
            }
        }

        public void Install()
        {
            this.Model.InstallPackage(this.Package, inDetails: true);
        }

        public void Update()
        {
            // updating is slightly more complex, because the 'package' might be the
            // installed version (if we're on the installed tab)
            // so use the remote package if we have one
            this.Model.UpdatePackage(this.RemotePackage ?? this.Package, inDetails: true);
        }

        public void Uninstall()
        {
            this.Model.UninstallPackage(this.Package, inDetails: true);
        }

        public string Message
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Gets installed packages with the same Id as this one
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IPackage> GetInstalledPackages()
        {
            return this.Model.PackageManager.GetInstalledPackages().Where(p => p.Id == this.Id);
        }

        private void OpenUrl(Uri url)
        {
            if (url != null)
            {
                ProcessHelper.TryOpenWithWindows(url.AbsoluteUri);
            }
        }
    }
}
