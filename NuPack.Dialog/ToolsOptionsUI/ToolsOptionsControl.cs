using System;
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

        public ToolsOptionsControl()
        {
            InitializeComponent();
        }

        internal void InitializeOnActivated() {
            _allPackageSources = new BindingSource(_packageSourceProvider.GetPackageSources().ToList(), null);
            PackageSourcesListBox.DataSource = _allPackageSources;
            NewPackageSource.Text = String.Empty;
        }

        private void removeButton_Click(object sender, EventArgs e) {
            PackageSource selectedPackage = PackageSourcesListBox.SelectedItem as PackageSource;
            if (selectedPackage != null) {
                _allPackageSources.Remove(selectedPackage);
                _packageSourceProvider.RemovePackageSource(selectedPackage);

                // if user deletes the active package source, assign the first item as the active one
                if (_packageSourceProvider.ActivePackageSource == null &&
                    PackageSourcesListBox.Items.Count > 0) {
                    _packageSourceProvider.ActivePackageSource = (PackageSource)PackageSourcesListBox.Items[0];
                }

                PackageSourcesListBox.Invalidate();
            }
        }

        private void addButton_Click(object sender, EventArgs e) {
            string source = NewPackageSource.Text;
            if (!String.IsNullOrWhiteSpace(source)) {
                source = source.Trim();

                // TODO: Provide another textbox for PackageSource name
                PackageSource newPackageSource = new PackageSource("New", source);
                _packageSourceProvider.AddPackageSource(newPackageSource);
                _allPackageSources.Add(newPackageSource);

                PackageSourcesListBox.Invalidate();

                // now clear the text box
                NewPackageSource.Text = String.Empty;
            }
        }

        private void defaultButton_Click(object sender, EventArgs e) {
            PackageSource selectedPackage = PackageSourcesListBox.SelectedItem as PackageSource;
            if (selectedPackage != null) {
                _packageSourceProvider.ActivePackageSource = selectedPackage;

                PackageSourcesListBox.Invalidate();
            }
        }

        private void AllPackageSourcesList_DrawItem(object sender, DrawItemEventArgs e) {
            // Draw the background of the ListBox control for each item.
            e.DrawBackground();

            if (e.Index < 0 || e.Index >= PackageSourcesListBox.Items.Count) {
                return;
            }

            PackageSource currentItem = (PackageSource)PackageSourcesListBox.Items[e.Index];

            // if the item is the active package source, draw it in bold
            Font newFont = (currentItem != null && currentItem.Equals(_packageSourceProvider.ActivePackageSource)) ? 
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