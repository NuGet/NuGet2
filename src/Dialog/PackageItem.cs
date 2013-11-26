using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Windows.Media.Imaging;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuGet.Dialog.Providers
{
    internal class PackageItem : IVsExtension, INotifyPropertyChanged
    {
        private readonly PackagesProviderBase _provider;
        private readonly IPackage _packageIdentity;
        private readonly bool _isUpdateItem, _isPrerelease;
        private bool _isSelected;
        private bool? _isEnabled;
        private readonly ObservableCollection<Project> _referenceProjectNames;
        private readonly SemanticVersion _oldPackageVersion;
        private IEnumerable<object> _displayDependencies;

        public PackageItem(PackagesProviderBase provider, IPackage package, SemanticVersion oldPackageVersion = null) :
            this(provider, package, new Project[0], oldPackageVersion)
        {
        }

        public PackageItem(PackagesProviderBase provider, IPackage package, IEnumerable<Project> referenceProjectNames, SemanticVersion oldPackageVersion = null)
        {
            _provider = provider;
            _packageIdentity = package;
            _isUpdateItem = oldPackageVersion != null;
            _oldPackageVersion = oldPackageVersion;
            _isPrerelease = !package.IsReleaseVersion();
            _referenceProjectNames = new ObservableCollection<Project>(referenceProjectNames);
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public IPackage PackageIdentity
        {
            get { return _packageIdentity; }
        }

        public string Id
        {
            get { return _packageIdentity.Id; }
        }

        public string Name
        {
            get
            {
                return String.IsNullOrEmpty(_packageIdentity.Title) ? _packageIdentity.Id : _packageIdentity.Title;
            }
        }

        public string Version
        {
            get
            {
                return _packageIdentity.Version.ToString();
            }
        }

        public string OldVersion
        {
            get
            {
                return _oldPackageVersion != null ? _oldPackageVersion.ToString() : null;
            }
        }

        public bool IsUpdateItem
        {
            get
            {
                return _isUpdateItem;
            }
        }

        public FrameworkName TargetFramework
        {
            get;
            set;
        }

        public string Description
        {
            get
            {
                if (_isUpdateItem && !String.IsNullOrEmpty(_packageIdentity.ReleaseNotes))
                {
                    return _packageIdentity.ReleaseNotes;
                }

                return _packageIdentity.Description;
            }
        }

        public string Summary
        {
            get
            {
                return String.IsNullOrEmpty(_packageIdentity.Summary) ? _packageIdentity.Description : _packageIdentity.Summary;
            }
        }

        public IEnumerable<PackageDependency> Dependencies
        {
            get
            {
                return _packageIdentity.GetCompatiblePackageDependencies(TargetFramework);
            }
        }

        /// <summary>
        /// This property is for XAML data binding.
        /// </summary>
        public IEnumerable<object> DisplayDependencies
        {
            get
            {
                if (_displayDependencies == null)
                {
                    if (TargetFramework == null)
                    {
                        var dependencySets = _packageIdentity.DependencySets;
                        if (dependencySets.Any(d => d.TargetFramework != null))
                        {
                            // if there is at least one dependeny set with non-null target framework,
                            // we show the dependencies grouped by target framework.
                            _displayDependencies = _packageIdentity.DependencySets;
                        }
                    }
                    
                    if (_displayDependencies == null)
                    {
                        // otherwise, just show the dependencies as pre 2.0
                        _displayDependencies = Dependencies;
                    }
                }

                return _displayDependencies;
            }
        }

        public ICollection<Project> ReferenceProjects
        {
            get
            {
                return _referenceProjectNames;
            }
        }

        public bool IsPrerelease
        {
            get
            {
                return _isPrerelease;
            }
        }

        public string CommandName
        {
            get;
            set;
        }

        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "This property is data-bound in XAML.")]
        public IEnumerable<string> Authors
        {
            get
            {
                return _packageIdentity.Authors;
            }
        }

        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "This property is data-bound in XAML.")]
        public bool RequireLicenseAcceptance
        {
            get
            {
                return _packageIdentity.RequireLicenseAcceptance;
            }
        }

        public Uri LicenseUrl
        {
            get
            {
                return _packageIdentity.LicenseUrl;
            }
        }

        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
                OnNotifyPropertyChanged("IsSelected");
            }
        }

        public bool IsEnabled
        {
            get
            {
                if (!_isEnabled.HasValue)
                {
                    _isEnabled = _provider.CanExecute(this);
                }

                return _isEnabled.Value;
            }
        }

        internal void UpdateEnabledStatus()
        {
            // set to null to force re-evaluation of the property value
            _isEnabled = null;
            OnNotifyPropertyChanged("IsEnabled");
        }

        private void OnNotifyPropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        // Not used but required by the interface IVsExtension.
        public float Priority
        {
            get { return 0; }
        }

        // Not used but required by the interface IVsExtension.
        public BitmapSource MediumThumbnailImage
        {
            get { return null; }
        }

        // Not used but required by the interface IVsExtension.
        public BitmapSource SmallThumbnailImage
        {
            get { return null; }
        }

        // Not used but required by the interface IVsExtension.
        public BitmapSource PreviewImage
        {
            get { return null; }
        }
    }
}