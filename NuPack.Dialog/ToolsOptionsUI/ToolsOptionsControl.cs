using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NuPack.VisualStudio;

namespace NuPack.Dialog.ToolsOptionsUI {
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

        public ToolsOptionsControl() : this(Settings.PackageSourceProvider) {
        }

        private void SetupDataBindings() {
            NewPackageName.TextChanged += (o, e) => UpdateUI();
            NewPackageSource.TextChanged += (o, e) => UpdateUI();
            PackageSourcesListView.ItemSelectionChanged += (o, e) => UpdateUI();

            UpdateUI();

        }

        public void UpdateUI() {
            addButton.Enabled = !String.IsNullOrEmpty(NewPackageName.Text.Trim()) &&
                                !String.IsNullOrEmpty(NewPackageSource.Text.Trim());

            defaultButton.Enabled = PackageSourcesListView.SelectedItems.Count > 0;
            removeButton.Enabled = PackageSourcesListView.SelectedItems.Count > 0;
        }

        public void BindData() {
            _listViewDataBinder.Bind();
            UpdateUI();
        }

        public void InitializeOnActivated() {
            if (_initialized) {
                return;
            }

            _initialized = true;
            _allPackageSources = new BindingSource(_packageSourceProvider.GetPackageSources().ToList(), null);
            _activePackageSource = _packageSourceProvider.ActivePackageSource;

            _listViewDataBinder = new ListViewDataBinder<PackageSource>(PackageSourcesListView,
                ps => new string[] { "", ps.Name, ps.Source },
                (ps, item) => {
                    if ((_activePackageSource == null && item.Index == 0)
                    || (_activePackageSource != null && _activePackageSource.Equals(ps))) {
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
        public void ApplyChangedSettings() {
            _packageSourceProvider.SetPackageSources((IEnumerable<PackageSource>)_allPackageSources.DataSource);
            _packageSourceProvider.ActivePackageSource = _activePackageSource;
        }

        /// <summary>
        /// This gets called when users close the Options dialog
        /// </summary>
        public void ClearSettings() {
            // clear this flag so that we will set up the bindings again when the option page is activated next time
            _initialized = false;

            _allPackageSources = null;
            _activePackageSource = null;
            NewPackageName.Text = String.Empty;
            NewPackageSource.Text = String.Empty;
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
            var name = NewPackageName.Text;
            var source = NewPackageSource.Text;
            if (!String.IsNullOrWhiteSpace(source)) {
                source = source.Trim();

                var newPackageSource = new PackageSource(name, source);
                if (_allPackageSources.Contains(newPackageSource)) {
                    return;
                }

                _allPackageSources.Add(newPackageSource);

                // if the collection contains only the package source that we just added, 
                // make it the default package source
                if (_activePackageSource == null && _allPackageSources.Count == 1) {
                    _activePackageSource = newPackageSource;
                }

                BindData();

                // now clear the text boxes
                NewPackageName.Text = String.Empty;
                NewPackageSource.Text = String.Empty;
            }
        }

        private void OnDefaultPackageSourceButtonClick(object sender, EventArgs e) {
            if (PackageSourcesListView.SelectedItems.Count == 0) {
                return;
            }

            _activePackageSource = PackageSourcesListView.SelectedItems[0].Tag as PackageSource;
            BindData();
        }


        private void PackageSourcesContextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            //if (e.ClickedItem == CopyPackageSourceStripMenuItem && _currentSelectedSource != null) {
            //    Clipboard.SetText(_currentSelectedSource);
            //}
        }

    }
}