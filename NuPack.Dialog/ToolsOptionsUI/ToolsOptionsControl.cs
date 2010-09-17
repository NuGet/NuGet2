using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NuPack.VisualStudio;

namespace NuPack.Dialog.ToolsOptionsUI
{
    /// <summary>
    /// Represents the Tools - Options - Package Manager dialog
    /// </summary>
    /// <remarks>
    /// The code in this class assumes that while the dialog is open, noone is modifying the VSPackageSourceProvider directly.
    /// Otherwise, we have a problem with synchronization with the package source provider.
    /// </remarks>
    public partial class ToolsOptionsControl : UserControl
    {
        private VSPackageSourceProvider _packageSourceProvider = Settings.PackageSourceProvider;
        private BindingSource _allPackageSources;
        private PackageSource _activePackageSource;
        private bool _initialized;

        public ToolsOptionsControl()
        {
            InitializeComponent();
            SetupDataBindings();
        }

        private void SetupDataBindings() {
            // Bind the Add Package Source button the text box. It's enabled when textbox is non-empty
            Binding addButtonBinding = new Binding("Enabled", NewPackageSource, "Text");
            addButtonBinding.Format += (o, e) => e.Value = !String.IsNullOrEmpty((String)e.Value);
            addButton.DataBindings.Add(addButtonBinding);

            //// Bind both Remove Package Source and Add Package Source buttons to the List Box.
            //// The buttons are enabled when the List Box has some item selected
            //ConvertEventHandler bindingFormat = (o, e) => e.Value = (Convert.ToInt32(e.Value) > -1);

            //Binding removeButtonBinding = new Binding("Enabled", PackageSourcesListBox, "SelectedIndex");
            //removeButtonBinding.Format += bindingFormat;
            //removeButton.DataBindings.Add(removeButtonBinding);

            //Binding defaultButtonBinding = new Binding("Enabled", PackageSourcesListBox, "SelectedIndex");
            //defaultButtonBinding.Format += bindingFormat;
            //defaultButton.DataBindings.Add(defaultButtonBinding);
        }

        internal void InitializeOnActivated() {
            if (_initialized) {
                return;
            }

            _initialized = true;
            _allPackageSources = new BindingSource(_packageSourceProvider.GetPackageSources().ToList(), null);
            _activePackageSource = _packageSourceProvider.ActivePackageSource;
            PackageSourcesListBox.DataSource = _allPackageSources;
        }

        /// <summary>
        /// Persist the package sources, which was add/removed via the Options page, to the VS Settings store.
        /// This gets called when users click OK button.
        /// </summary>
        internal void ApplyChangedSettings() {
            _packageSourceProvider.SetPackageSources((IEnumerable<PackageSource>)_allPackageSources.DataSource);
            _packageSourceProvider.ActivePackageSource = _activePackageSource;
        }

        /// <summary>
        /// This gets called when users close the Options dialog
        /// </summary>
        internal void ClearSettings() {
            // clear this flag so that we will set up the bindings again when the option page is activated next time
            _initialized = false;

            _allPackageSources = null;
            _activePackageSource = null;
            NewPackageSource.Text = String.Empty;
        }

        private void OnRemoveButtonClick(object sender, EventArgs e) {
            PackageSource selectedPackage = PackageSourcesListBox.SelectedItem as PackageSource;
            if (selectedPackage != null) {
                _allPackageSources.Remove(selectedPackage);

                if (_activePackageSource.Equals(selectedPackage)) {
                    _activePackageSource = null;

                    // if user deletes the active package source, assign the first item as the active one
                    if (_allPackageSources.Count > 0) {
                        _activePackageSource = (PackageSource)_allPackageSources[0];
                    }
                }

                PackageSourcesListBox.Invalidate();
            }
        }

        private void OnAddButtonClick(object sender, EventArgs e) {
            string source = NewPackageSource.Text;
            if (!String.IsNullOrWhiteSpace(source)) {
                source = source.Trim();

                // TODO: Provide another textbox for PackageSource name
                PackageSource newPackageSource = new PackageSource("New", source);
                if (_allPackageSources.Contains(newPackageSource)) {
                    return;
                }

                _allPackageSources.Add(newPackageSource);

                // if the collection contains only the package source that we just added, 
                // make it the default package source
                if (_activePackageSource == null && _allPackageSources.Count == 1) {
                    _activePackageSource = newPackageSource;
                }

                // redraw the listbox to show the new default package source (if any)
                PackageSourcesListBox.Invalidate();

                // now clear the text box
                NewPackageSource.Text = String.Empty;
            }
        }

        private void OnDefaultPackageSourceButtonClick(object sender, EventArgs e) {
            PackageSource selectedPackageSource = PackageSourcesListBox.SelectedItem as PackageSource;
            if (selectedPackageSource != null) {
                _activePackageSource = selectedPackageSource;

                PackageSourcesListBox.Invalidate();
            }
        }

        private void AllPackageSourcesList_DrawItem(object sender, DrawItemEventArgs e) {
            // Draw the background of the ListBox control for each item.
            e.DrawBackground();

            if (e.Index < 0 || e.Index >= PackageSourcesListBox.Items.Count) {
                return;
            }

            var currentItem = PackageSourcesListBox.Items[e.Index];

            // if the item is the active package source, draw it in bold
            Font newFont = (_activePackageSource != null && _activePackageSource.Equals(currentItem)) ? 
                new Font(e.Font, FontStyle.Bold) : 
                e.Font;

            StringFormat drawFormat = new StringFormat {
                Alignment = StringAlignment.Near,
                Trimming = StringTrimming.EllipsisCharacter,
                LineAlignment = StringAlignment.Center
            };

            // Draw the current item text based on the current Font 
            // and the custom brush settings.
            e.Graphics.DrawString(PackageSourcesListBox.GetItemText(currentItem),
                newFont, new SolidBrush(e.ForeColor), e.Bounds, drawFormat);
            
            // If the ListBox has focus, draw a focus rectangle around the selected item.
            e.DrawFocusRectangle();
        }
    }
}