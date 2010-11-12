using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using NuGet.Dialog.PackageManagerUI;
using NuGet.VisualStudio;

namespace NuGet.Dialog.ToolsOptionsUI {
    /// <summary>
    /// Represents the Tools - Options - Package Manager dialog
    /// </summary>
    /// <remarks>
    /// The code in this class assumes that while the dialog is open, noone is modifying the VSPackageSourceProvider directly.
    /// Otherwise, we have a problem with synchronization with the package source provider.
    /// </remarks>
    public partial class ToolsOptionsControl : UserControl {
        private IPackageSourceProvider _packageSourceProvider;
        private BindingSource _allPackageSources;
        private PackageSource _activePackageSource;
        private bool _initialized;
        private ListViewDataBinder<PackageSource> _listViewDataBinder;

        public ToolsOptionsControl(IPackageSourceProvider packageSourceProvider) {
            _packageSourceProvider = packageSourceProvider;
            InitializeComponent();
            SetupDataBindings();
        }

        public ToolsOptionsControl()
            : this(ServiceLocator.GetInstance<IPackageSourceProvider>()) {
        }

        private void SetupDataBindings() {
            NewPackageName.TextChanged += (o, e) => UpdateUI();
            NewPackageSource.TextChanged += (o, e) => UpdateUI();
            PackageSourcesListView.ItemSelectionChanged += (o, e) => UpdateUI();
            NewPackageName.Focus();
            UpdateUI();
        }

        public void UpdateUI() {
            defaultButton.Enabled = PackageSourcesListView.SelectedItems.Count > 0;
            removeButton.Enabled = PackageSourcesListView.SelectedItems.Count > 0 && !((PackageSource)PackageSourcesListView.SelectedItems[0].Tag).IsAggregate;
        }

        public void BindData() {
            _listViewDataBinder.Bind();
            UpdateUI();
        }

        internal void InitializeOnActivated() {
            if (_initialized) {
                return;
            }

            _initialized = true;
            _allPackageSources = new BindingSource(_packageSourceProvider.GetPackageSources().ToList(), null);
            _activePackageSource = _packageSourceProvider.ActivePackageSource;

            _listViewDataBinder = new ListViewDataBinder<PackageSource>(PackageSourcesListView,
                ps => new string[] { ps.Name, ps.Source },  // new ListViewItem
                (ps, item) => {
                    // set checkmark image on default package
                    if ((_activePackageSource == null && item.Index == 0)   // no default package, so select first
                        || (ps.Equals(_activePackageSource))) { // OR current item is default package so set checkmark
                        item.ImageIndex = 0;
                    }
                }
                );
            _listViewDataBinder.DataSource = _allPackageSources;
            BindData();
        }

        /// <summary>
        /// Persist the package sources, which was add/removed via the Options page, to the VS Settings store.
        /// This gets called when users click OK button.
        /// </summary>
        internal bool ApplyChangedSettings() {
            // if user presses Enter after filling in Name/Source but doesn't click Add
            // the options will be closed without adding the source, try adding before closing
            // Only apply if nothing was added
            TryAddSourceResults result = TryAddSource();
            if (result != TryAddSourceResults.NothingAdded) {
                return false;
            }

            _packageSourceProvider.SetPackageSources((IEnumerable<PackageSource>)_allPackageSources.DataSource);
            _packageSourceProvider.ActivePackageSource = _activePackageSource;
            return true;
        }

        /// <summary>
        /// This gets called when users close the Options dialog
        /// </summary>
        internal void ClearSettings() {
            // clear this flag so that we will set up the bindings again when the option page is activated next time
            _initialized = false;

            _allPackageSources = null;
            _activePackageSource = null;
            ClearNameSource();
            UpdateUI();
        }

        private void OnRemoveButtonClick(object sender, EventArgs e) {
            if (PackageSourcesListView.SelectedItems.Count == 0) {
                return;
            }
            var selectedPackage = PackageSourcesListView.SelectedItems[0].Tag as PackageSource;
            if (selectedPackage != null) {
                _allPackageSources.Remove(selectedPackage);

                if (_activePackageSource != null && _activePackageSource.Equals(selectedPackage)) {
                    _activePackageSource = null;

                    // if user deletes the active package source, assign the first item as the active one
                    if (_allPackageSources.Count > 0) {
                        _activePackageSource = (PackageSource)_allPackageSources[0];
                    }
                }

                BindData();
            }
        }

        private void OnAddButtonClick(object sender, EventArgs e) {
            TryAddSourceResults result = TryAddSource();
            if (result == TryAddSourceResults.NothingAdded) {
                MessageHelper.ShowWarningMessage(Resources.ShowWarning_NameAndSourceRequired, Resources.ShowWarning_Title);
                SelectAndFocus(NewPackageName);
            }        
        }

        private TryAddSourceResults TryAddSource() {
            var name = NewPackageName.Text.Trim();
            var source = NewPackageSource.Text.Trim();
            if (String.IsNullOrWhiteSpace(name) && String.IsNullOrWhiteSpace(source)) {
                return TryAddSourceResults.NothingAdded;
            }
            
            // validate name
            if (String.IsNullOrWhiteSpace(name)) {
                MessageHelper.ShowWarningMessage(Resources.ShowWarning_NameRequired, Resources.ShowWarning_Title);
                SelectAndFocus(NewPackageName);
                return TryAddSourceResults.InvalidSource;                
            }

            // validate source
            if (String.IsNullOrWhiteSpace(source)) {
                MessageHelper.ShowWarningMessage(Resources.ShowWarning_SourceRequried, Resources.ShowWarning_Title);
                SelectAndFocus(NewPackageSource);
                return TryAddSourceResults.InvalidSource;
            }

            if (!(PathValidator.IsValidLocalPath(source) || PathValidator.IsValidUncPath(source) || PathValidator.IsValidUrl(source))) {
                MessageHelper.ShowWarningMessage(Resources.ShowWarning_InvalidSource, Resources.ShowWarning_Title);
                SelectAndFocus(NewPackageSource);
                return TryAddSourceResults.InvalidSource;                
            }

            var sourcesList = (IEnumerable<PackageSource>) _allPackageSources.List;

            // check to see if name has already been added
            bool hasName = sourcesList.Any(ps => String.Equals(name, ps.Name, StringComparison.OrdinalIgnoreCase));
            if (hasName) {
                MessageHelper.ShowWarningMessage(Resources.ShowWarning_UniqueName, Resources.ShowWarning_Title);
                SelectAndFocus(NewPackageName);
                return TryAddSourceResults.SourceAlreadyAdded;
            }

            // check to see if source has already been added
            bool hasSource = sourcesList.Any(ps => String.Equals(source, ps.Source, StringComparison.OrdinalIgnoreCase));
            if (hasSource) {
                MessageHelper.ShowWarningMessage(Resources.ShowWarning_UniqueSource, Resources.ShowWarning_Title);
                SelectAndFocus(NewPackageSource);
                return TryAddSourceResults.SourceAlreadyAdded;
            }

            var newPackageSource = new PackageSource(source, name);
            _allPackageSources.Add(newPackageSource);

            // if the collection contains only the package source that we just added, 
            // make it the default package source
            if (_activePackageSource == null && _allPackageSources.Count == 1) {
                _activePackageSource = newPackageSource;
            }

            BindData();

            // now clear the text boxes
            ClearNameSource();

            return TryAddSourceResults.SourceAdded;
        }

        private static void SelectAndFocus(TextBox textBox) {
            textBox.Focus();
            textBox.SelectAll();
        }

        private void ClearNameSource() {
            NewPackageName.Text = String.Empty;
            NewPackageSource.Text = String.Empty;
            NewPackageName.Focus();
        }

        private void OnDefaultPackageSourceButtonClick(object sender, EventArgs e) {
            if (PackageSourcesListView.SelectedItems.Count == 0) {
                return;
            }

            _activePackageSource = PackageSourcesListView.SelectedItems[0].Tag as PackageSource;
            BindData();
        }


        private void PackageSourcesContextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            if (e.ClickedItem == CopyPackageSourceStripMenuItem && PackageSourcesListView.SelectedItems.Count > 0) {
                var selectedPackageSource = (PackageSource) PackageSourcesListView.SelectedItems[0].Tag;
                Clipboard.Clear();
                Clipboard.SetText(selectedPackageSource.Source);
            }
        }

        private void PackageSourcesListView_MouseClick(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Right) {
                PackageSourcesContextMenu.Show(PackageSourcesListView, e.Location);
            }
        }
    }

    internal enum TryAddSourceResults {
        NothingAdded = 0,
        SourceAdded = 1,
        InvalidSource = 2,
        SourceAlreadyAdded = 3
    }
}
